#!/usr/bin/env node
/**
 * Automation E2E — TiendaApi .NET
 *
 * Node nativo, SIN npm install.
 *
 * Uso (desde la raíz del repo):
 *   node TiendaApi.Tests.E2E/Automation/test-runner.mjs
 *
 * Modos:
 *   - Auto (default): levanta infra (postgres+mongodb en Docker si hace falta),
 *     hace restore/build de la API, la arranca en Development y ejecuta la suite.
 *   - Externo: BASE_URL=http://localhost:5031 node ... → solo ejecuta la suite
 *     contra una API ya levantada (también se usa automáticamente si la API
 *     ya responde en el baseUrl configurado).
 *
 * Diseño:
 *   - NO ejecuta `docker compose down -v` al final: solo para los servicios de BD
 *     que ÉL haya levantado (si ya estaban corriendo, no se tocan) → no rompe
 *     el entorno de desarrollo.
 *   - Fallback: si `dotnet run` no responde, intenta `docker compose up -d --build`.
 *   - Espera /health primero; si falla acepta /swagger o /api/productos.
 *   - Rate limit awareness: helper st() → falla CLARO si la API devuelve 429
 *     (100/15s general · 10/min auth · 20/min POST · 200/min graphql).
 *   - Credenciales seed: admin/Admin123!, userdaw/Userdaw123! (iguales en
 *     modo Custom e Identity — ver SeedCredentials.cs).
 *   - Sale con código 0 si todo OK, 1 si algo falla (listo para CI).
 */

import { spawn, spawnSync } from "node:child_process";
import { existsSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(__dirname, "..", "..");

const CONFIG = {
  nombre: "TiendaApi .NET · Automation E2E",
  csproj: "TiendaApi.Api/TiendaApi.csproj",
  baseUrl: process.env.BASE_URL || "http://localhost:5031",
  healthPaths: ["/health", "/swagger", "/api/productos"],
  env: {
    ASPNETCORE_ENVIRONMENT: "Development",
    ASPNETCORE_URLS: "http://localhost:5031",
    // Auth:Mode parametrizable: AUTH_MODE=Identity node test-runner.mjs
    // arranca la API con UserManager/PBKDF2 en vez de BCrypt.
    Auth__Mode: process.env.AUTH_MODE || "Custom",
  },
  dbServices: ["postgres", "mongodb"],
  composeFile: "docker-compose.local.yml",
  // Auth:Mode ACTIVO de esta corrida (misma variable que env.Auth__Mode): los
  // tests condicionales (ramas Identity/Custom de signup) lo consultan aqui.
  // Sin esta declaracion CONFIG.authMode era undefined y la rama Identity
  // nunca se ejecutaba (hay que validar los DOS modos).
  authMode: process.env.AUTH_MODE || "Custom",
  // Credenciales seed unificadas (las MISMAS en Custom e Identity — SeedCredentials)
  admin: { username: process.env.ADMIN_USER || "admin", password: process.env.ADMIN_PASS || "Admin123!" },
  user: { username: process.env.USER_USER || "userdaw", password: process.env.USER_PASS || "Userdaw123!" },
};

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
let exitCode = 0;
const log = (m) => console.log(m);
const logOk = (m) => console.log(`  ✅ ${m}`);
const logErr = (m) => console.error(`  ❌ ${m}`);

function run(cmd) {
  const res = spawnSync(cmd, { cwd: ROOT, encoding: "utf8", shell: true });
  if (res.status !== 0) throw new Error(`Fallo: ${cmd}\n${(res.stdout || "") + (res.stderr || "")}`.slice(0, 2000));
  return res;
}
function tryRun(cmd) {
  try { run(cmd); } catch (e) { log(`  ⚠️ (tolerado) ${e.message.split("\n")[0]}`); }
}
function dockerAvailable() {
  try { return spawnSync("docker", ["compose", "version"], { encoding: "utf8", shell: true }).status === 0; }
  catch { return false; }
}
function serviceRunning(name) {
  try {
    const res = spawnSync(
      `docker compose -f ${CONFIG.composeFile} ps --status running --format "{{.Service}}"`,
      { cwd: ROOT, encoding: "utf8", shell: true }
    );
    if ((res.stdout || "").split(/\r?\n/).includes(name)) return true;
    // Los contenedores locales pueden existir SIN labels de compose (creados a
    // mano o por otra via): compose ps no los asocia al proyecto y el `up`
    // posterior chocaria por container_name. Detectarlos por nombre generado
    // (tienda-local-<servicio>) para no tocar infra que ya esta sana.
    const byName = spawnSync(
      `docker ps --filter "name=tienda-local-${name}" --filter status=running --format "{{.Names}}"`,
      { encoding: "utf8", shell: true }
    );
    return (byName.stdout || "").includes(`tienda-local-${name}`);
  } catch { return false; }
}
function killTree(pid) {
  if (!pid) return;
  try { spawnSync("taskkill", ["/pid", String(pid), "/T", "/F"], { shell: true }); }
  catch { try { process.kill(pid); } catch { } }
}
function spawnApi() {
  // --no-launch-profile: launchSettings.json fuerza Development y su propio puerto;
  // aquí controlamos entorno y URL explícitamente.
  const child = spawn(
    "dotnet",
    ["run", "--project", CONFIG.csproj, "-c", "Debug", "--no-launch-profile"],
    {
      cwd: ROOT,
      env: { ...process.env, ...CONFIG.env },
      stdio: ["ignore", "pipe", "pipe"],
      shell: true,
    }
  );
  child.stdout.on("data", (d) => process.stdout.write(`   [api] ${d}`));
  child.stderr.on("data", (d) => process.stderr.write(`   [api] ${d}`));
  return child;
}
async function fetchAny(url) {
  try {
    const ctrl = new AbortController();
    const t = setTimeout(() => ctrl.abort(), 3000);
    const res = await fetch(url, { signal: ctrl.signal });
    clearTimeout(t);
    return res;
  } catch { return null; }
}
async function waitForApi(timeoutMs = 30000) {
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    for (const p of CONFIG.healthPaths) {
      if (await fetchAny(CONFIG.baseUrl + p)) return true;
    }
    await sleep(500);
  }
  return false;
}
async function req(method, urlPath, { body, headers = {}, token, ifNoneMatch } = {}) {
  const h = { ...headers };
  const isForm = typeof FormData !== "undefined" && body instanceof FormData;
  if (body !== undefined && !isForm) h["Content-Type"] = h["Content-Type"] || "application/json";
  if (token) h.Authorization = `Bearer ${token}`;
  if (ifNoneMatch) h["If-None-Match"] = ifNoneMatch;
  const res = await fetch(CONFIG.baseUrl + urlPath, {
    method,
    headers: h,
    body: body === undefined ? undefined : isForm ? body : typeof body === "string" ? body : JSON.stringify(body),
  });
  let text = "";
  try { text = await res.text(); } catch { }
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { }
  return { status: res.status, text, json, headers: res.headers };
}

const resultados = [];
async function test(nombre, fn) {
  try {
    await fn();
    resultados.push({ nombre, ok: true });
    logOk(nombre);
  } catch (e) {
    resultados.push({ nombre, ok: false, error: e.message });
    logErr(`${nombre} → ${e.message}`);
    exitCode = 1;
  }
}
function assert(c, m) { if (!c) throw new Error(m); }
function assertEq(a, e, m) { if (a !== e) throw new Error(`${m}: obtenido ${JSON.stringify(a)}, esperado ${JSON.stringify(e)}`); }

// Rate limit awareness: el 429 NUNCA se acepta como OK.
function st(r) {
  assert(
    r.status !== 429,
    "Rate limit 429 alcanzado: la suite superó un límite de peticiones " +
    "(100/15s general · 10/min auth · 20/min POST). Espera ~1 min y relanza."
  );
  return r.status;
}

// Helpers de tiempo real: WebSocket global de Node 22+ (sin npm install).
// SignalR se habla a mano: handshake JSON + separador de registro (char 30).
const SEP = String.fromCharCode(30);
const wsUrl = (p) => CONFIG.baseUrl.replace(/^http/, "ws") + p;
const wait = (ms) => new Promise((r) => setTimeout(r, ms));

function wsOpen(url, { timeoutMs = 5000 } = {}) {
  return new Promise((resolve, reject) => {
    const sock = new WebSocket(url);
    const t = setTimeout(() => { try { sock.close(); } catch { } reject(new Error(`timeout abriendo ${url}`)); }, timeoutMs);
    sock.onopen = () => { clearTimeout(t); resolve(sock); };
    sock.onerror = () => { clearTimeout(t); reject(new Error(`error WS abriendo ${url}`)); };
  });
}
function wsNextMessage(sock, timeoutMs = 5000) {
  return new Promise((resolve, reject) => {
    const t = setTimeout(() => reject(new Error("timeout esperando mensaje WS")), timeoutMs);
    sock.onmessage = (ev) => { clearTimeout(t); resolve(String(ev.data)); };
  });
}
function wsWaitClose(sock, timeoutMs = 5000) {
  return new Promise((resolve, reject) => {
    const t = setTimeout(() => reject(new Error("timeout esperando cierre WS")), timeoutMs);
    sock.onclose = (ev) => { clearTimeout(t); resolve({ code: ev.code, reason: ev.reason }); };
  });
}

const dest = (pref) => ({
  nombreCompleto: `Automation ${pref}`,
  email: `auto_${pref}_${Date.now()}@test.com`,
  telefono: "+34123456789",
  direccion: {
    calle: "Calle Automation", numero: "1", ciudad: "Madrid",
    provincia: "Madrid", pais: "España", codigoPostal: "28001",
  },
});

async function runSuite() {
  const ts = Date.now();
  let adminToken = null;
  let userToken = null;
  let categoriaId = null;
  let productoId = null;
  let pedidoId = null;
  let userIdCreado = null;
  let userIdSignup = null;
  let autoToken = null;
  let productoIdCreadoGQL = null;
let imagenProductoUrl = null;

  // ============================ HEALTH ============================
  await test("Health: GET /health → 200 con JSON status", async () => {
    const r = await req("GET", "/health");
    assertEq(st(r), 200, "status");
    assert(r.json && typeof r.json === "object", "la respuesta no es JSON");
    assert(r.json.status !== undefined, "falta la propiedad status");
  });

  await test("Health: GET /health/live → 200 (liveness, sin dependencias)", async () => {
    const r = await req("GET", "/health/live");
    assertEq(st(r), 200, "status");
  });

  await test("Health: GET /health/ready → 200 (dependencias ready)", async () => {
    const r = await req("GET", "/health/ready");
    assertEq(st(r), 200, "status");
  });

  await test("Info: GET /version → 200 con versión del assembly", async () => {
    const r = await req("GET", "/version");
    assertEq(st(r), 200, "status");
    const v = r.json?.version ?? r.json?.Version;
    assert(v, `falta version en la respuesta: ${r.text.slice(0, 120)}`);
  });

  await test("RateLimit: respuestas llevan headers RateLimit-*", async () => {
    const r = await req("GET", "/version");
    const limit = r.headers.get("ratelimit-limit");
    const remaining = r.headers.get("ratelimit-remaining");
    assert(limit, "falta RateLimit-Limit");
    assert(remaining !== null && remaining !== undefined, "falta RateLimit-Remaining");
  });

  // ============================ AUTH ============================
  await test("Auth: POST signup usuario único → 201 con token", async () => {
    const r = await req("POST", "/api/v1/auth/signup", {
      body: { username: `auto_${ts}`, email: `auto_${ts}@test.com`, password: "Test1234!" },
    });
    assertEq(st(r), 201, "status");
    assert(r.json?.token, "falta token");
    assertEq(r.json?.user?.role, "USER", "role");
    userIdSignup = r.json?.user?.id;
    autoToken = r.json.token;
  });

  await test("Auth: POST signup con username inválido → 400 en ambos modos", async () => {
    const r = await req("POST", "/api/v1/auth/signup", {
      body: { username: "x", email: `bad_${ts}@test.com`, password: "Test1234!" },
    });
    // FluentValidationAutoValidation (ControllersConfig) evalua ANTES del
    // controller en TODOS los Auth:Mode: ni Custom ni Identity llegan a
    // UserManager. La rama Identity solo se ejecuta cuando CONFIG.authMode
    // está definido; sin él, este test siempre tomaría la rama de Custom.
    assertEq(st(r), 400, "status");
  });

  await test("Auth: POST signup con email inválido → 400 en ambos modos", async () => {
    const r = await req("POST", "/api/v1/auth/signup", {
      body: { username: `badmail_${ts}`, email: "no-es-email", password: "Test1234!" },
    });
    // Mismo motivo: la validacion previa (400) pilla el email mal formado
    // antes de que Identity llegue a su BusinessRuleError (422), que sigue
    // existiendo para los errores de UserManager (p. ej. politica de password).
    assertEq(st(r), 400, "status");
  });

  await test("Auth: POST signup username duplicado → 409 Conflict", async () => {
    // Reintenta el signup del usuario único: username y email ya registrados
    const r = await req("POST", "/api/v1/auth/signup", {
      body: { username: `auto_${ts}`, email: `auto_${ts}@test.com`, password: "Test1234!" },
    });
    assertEq(st(r), 409, "status");
  });

  await test("Auth: POST signin admin → 200 con token ADMIN", async () => {
    const r = await req("POST", "/api/v1/auth/signin", { body: CONFIG.admin });
    assertEq(st(r), 200, "status");
    assert(r.json?.token, "falta token");
    assertEq(r.json?.user?.role, "ADMIN", "role");
    adminToken = r.json.token;
  });

  await test("Auth: POST signin user → 200 con token USER", async () => {
    const r = await req("POST", "/api/v1/auth/signin", { body: CONFIG.user });
    assertEq(st(r), 200, "status");
    assert(r.json?.token, "falta token");
    assertEq(r.json?.user?.role, "USER", "role");
    userToken = r.json.token;
  });

  await test("Auth: POST signin password incorrecta → 401", async () => {
    const r = await req("POST", "/api/v1/auth/signin", {
      body: { username: CONFIG.admin.username, password: "WrongPass123" },
    });

  await test("Auth: POST signin usuario no existe → 401", async () => {
    const r = await req("POST", "/api/v1/auth/signin", {
      body: { username: `nobody_${ts}`, password: "Password123!" },
    });
    assertEq(st(r), 401, "status");
  });
    assertEq(st(r), 401, "status");
  });

  // OAuth externo: en este entorno las puertas están habilitadas en
  // appsettings.Development.json pero SIN credenciales → 404 (puerta cerrada).
  // Si algún día se configuran credenciales reales, el 404 pasa a ser 302.
  await test("Auth ext: GET /auth/external/Google sin credenciales → 404 (puerta cerrada)", async () => {
    const r = await req("GET", "/api/v1/auth/external/Google");
    assertEq(st(r), 404, "status");
    assert(String(r.json ?? r.body ?? "").includes("no está configurado"), "mensaje de proveedor no configurado");
  });

  await test("Auth ext: GET /auth/external/ProveedorInventado → 404", async () => {
    const r = await req("GET", "/api/v1/auth/external/ProveedorInventado");
    assertEq(st(r), 404, "status");
  });

  await test("Auth ext: GET /auth/external-callback sin autenticar → 400", async () => {
    const r = await req("GET", "/api/v1/auth/external-callback");
    assertEq(st(r), 400, "status");
  });

  // ============================ CATEGORÍAS ============================
  await test("Categorías: GET paged → 200 con items y paginación", async () => {
    const r = await req("GET", "/api/categorias?page=0&size=2");
    assertEq(st(r), 200, "status");
    assert(Array.isArray(r.json?.items), "items no es array");
    assert(typeof r.json?.totalCount === "number", "falta totalCount");
    assert(r.json.totalCount >= 3, `semilla esperaba >=3 categorías, hay ${r.json.totalCount}`);
  });

  await test("Categorías: GET All (sin paginación) → 200 array", async () => {
    const r = await req("GET", "/api/categorias");
    assertEq(st(r), 200, "status");
    assert(Array.isArray(r.json?.items ?? r.json), "respuesta sin items ni array");
  });

  await test("Categorías: GET con filtros (nombre) → 200", async () => {
    const r = await req("GET", "/api/categorias?nombre=Elec&page=0&size=10&sortBy=id&direction=asc");
    assertEq(st(r), 200, "status");
    assert(Array.isArray(r.json?.items ?? r.json), "respuesta sin items");
  });

  await test("Categorías: GET /1 → 200 (semilla)", async () => {
    const r = await req("GET", "/api/categorias/1");
    assertEq(st(r), 200, "status");
    assert(r.json?.id !== undefined, "falta id");
  });

  await test("Categorías: GET repetido → HIT de OutputCache (header Age)", async () => {
    // [OutputCache(Duration=60)] cachea el GET anónimo; la 2ª petición sirve
    // desde caché y lo delata el header Age. (ETag/304 NO está implementado:
    // el middleware no emite ETag; ver doc/README sección caché HTTP.)
    const url = "/api/categorias?page=0&size=2";
    const first = await req("GET", url);
    assertEq(st(first), 200, "status inicial");
    const second = await req("GET", url);
    assertEq(st(second), 200, "status repetido");
    assert(second.headers.get("age") !== null && second.headers.get("age") !== undefined,
      "la 2ª petición no trae header Age: no sirvió desde OutputCache");
  });

  await test("Categorías: GET /999999 → 404 con message", async () => {
    const r = await req("GET", "/api/categorias/999999");
    assertEq(st(r), 404, "status");
    assert(r.json?.message, "falta message");
  });

  await test("Categorías: POST sin token → 401", async () => {
    const r = await req("POST", "/api/categorias", {
      body: { nombre: `SinAuth ${ts}`, descripcion: "x" },
    });
    assertEq(st(r), 401, "status");
  });

  await test("Categorías: POST rol USER → 403", async () => {
    const r = await req("POST", "/api/categorias", {
      token: userToken,
      body: { nombre: `UserRol ${ts}`, descripcion: "x" },
    });
    assertEq(st(r), 403, "status");
  });

  await test("Categorías: POST admin → 201 y guarda id", async () => {
    const r = await req("POST", "/api/categorias", {
      token: adminToken,
      body: { nombre: `Auto Cat ${ts}`, descripcion: "Creada por automation" },
    });

  await test("Categorías: POST nombre duplicado → 409 Conflict", async () => {
    const nombre = `Dup Cat ${ts}`;
    const body = { nombre, descripcion: "Duplicado E2E" };
    const primero = await req("POST", "/api/categorias", { token: adminToken, body });
    assertEq(st(primero), 201, "status primer create");
    const segundo = await req("POST", "/api/categorias", { token: adminToken, body });
    assertEq(st(segundo), 409, "status duplicado");
    await req("DELETE", `/api/categorias/${primero.json.id}`, { token: adminToken });
  });

  await test("Categorías: PUT /999999 → 404", async () => {
    const r = await req("PUT", "/api/categorias/999999", {
      token: adminToken,
      body: { nombre: "No existe", descripcion: "x" },
    });
    assertEq(st(r), 404, "status");
  });
    assertEq(st(r), 201, "status");
    categoriaId = r.json?.id;
    assert(categoriaId !== undefined, "falta id");
  });

  await test("Categorías: PUT /{id} admin → 200", async () => {
    const r = await req("PUT", `/api/categorias/${categoriaId}`, {
      token: adminToken,
      body: { nombre: `Auto Cat ${ts} v2`, descripcion: "Actualizada" },
    });
    assertEq(st(r), 200, "status");
    assert(String(r.json?.nombre).includes("v2"), "nombre no actualizado");
  });

  await test("Categorías: DELETE /{id} admin → 204", async () => {
    const r = await req("DELETE", `/api/categorias/${categoriaId}`, { token: adminToken });
    assertEq(st(r), 204, "status");
  });

  await test("Categorías: DELETE /999999 → 404", async () => {
    const r = await req("DELETE", "/api/categorias/999999", { token: adminToken });
    assertEq(st(r), 404, "status");
  });

  // ============================ PRODUCTOS ============================
  await test("Productos: GET paged → 200 con semilla", async () => {
    const r = await req("GET", "/api/productos?page=0&size=2");
    assertEq(st(r), 200, "status");
    assert(Array.isArray(r.json?.items), "items no es array");
    assert(r.json.totalCount >= 3, `semilla esperaba >=3 productos, hay ${r.json.totalCount}`);
  });

  await test("Productos: GET All (sin paginación) → 200 array", async () => {
    const r = await req("GET", "/api/productos");
    assertEq(st(r), 200, "status");
    assert(Array.isArray(r.json?.items ?? r.json), "respuesta sin items ni array");
  });

  await test("Productos: GET paged con filtros (precioMax) → 200", async () => {
    const r = await req("GET", "/api/productos?precioMax=1000&page=0&size=2");
    assertEq(st(r), 200, "status");
    assert(Array.isArray(r.json?.items), "items no es array");
    assert(r.json.items.every((p) => p.precio <= 1000), "filtro precioMax no aplicado");
  });

  await test("Productos: GET /1 → 200 (semilla)", async () => {
    const r = await req("GET", "/api/productos/1");
    assertEq(st(r), 200, "status");
    assert(r.json?.id !== undefined, "falta id");
  });

  await test("Productos: GET /999999 → 404 con message", async () => {
    const r = await req("GET", "/api/productos/999999");
    assertEq(st(r), 404, "status");
    assert(r.json?.message, "falta message");
  });

  await test("Productos: GET /categoria/1 → 200 array", async () => {
    const r = await req("GET", "/api/productos/categoria/1");
    assertEq(st(r), 200, "status");
    assert(Array.isArray(r.json), "no es array");
  });

  await test("Productos: POST sin token → 401", async () => {
    const r = await req("POST", "/api/productos", {
      body: { nombre: "SinAuth Prod", precio: 10, stock: 1, categoriaId: 1 },
    });
    assertEq(st(r), 401, "status");
  });

  await test("Productos: POST precio 0 → 400 (validación)", async () => {
    const r = await req("POST", "/api/productos", {
      token: adminToken,
      body: { nombre: `Precio cero ${ts}`, descripcion: "x", precio: 0, stock: 1, categoriaId: 1 },
    });
    assertEq(st(r), 400, "status");
  });

  await test("Productos: POST admin → 201 y guarda id", async () => {
    const r = await req("POST", "/api/productos", {
      token: adminToken,
      body: {
        nombre: `Auto Prod ${ts}`, descripcion: "Creado por automation",
        precio: 19.99, stock: 5, categoriaId: 1,
      },
    });

  await test("Productos: POST con categoriaId inexistente → 400", async () => {
    const r = await req("POST", "/api/productos", {
      token: adminToken,
      body: { nombre: `OrfCat ${ts}`, descripcion: "categoria inexistente", precio: 5, stock: 1, categoriaId: 999999 },
    });
    assertEq(st(r), 400, "status");
  });
    assertEq(st(r), 201, "status");
    productoId = r.json?.id;
    assert(productoId !== undefined, "falta id");
  });

  await test("Productos: PUT /{id} → 200", async () => {
    const r = await req("PUT", `/api/productos/${productoId}`, {
      token: adminToken,
      body: {
        nombre: `Auto Prod ${ts} v2`, descripcion: "Actualizado",
        precio: 29.99, stock: 4, categoriaId: 1,
      },
    });
    assertEq(st(r), 200, "status");
    assertEq(r.json?.precio, 29.99, "precio");
  });

  await test("Productos: PATCH /{id} (parcial) → 200", async () => {
    const r = await req("PATCH", `/api/productos/${productoId}`, {
      token: adminToken,
      body: { precio: 24.99, stock: 3 },
    });
    assertEq(st(r), 200, "status");
    assertEq(r.json?.stock, 3, "stock");
  });

  await test("Productos: PATCH /{id}/imagen sin archivo → 400", async () => {
    const r = await req("PATCH", `/api/productos/${productoId}/imagen`, { token: adminToken });
    assertEq(st(r), 400, "status");
    assert(r.json?.errors?.image || r.json?.title, "falta errors.image/title");
  });

  await test("Productos: PATCH /{id}/imagen con archivo → 200", async () => {
    const png = Buffer.from(
      "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==",
      "base64",
    );
    const fd = new FormData();
    fd.append("image", new Blob([png], { type: "image/png" }), `auto_${ts}.png`);
    const r = await req("PATCH", `/api/productos/${productoId}/imagen`, { body: fd, token: adminToken });
    assertEq(st(r), 200, "status");
    assert(r.json?.imagen, "falta campo imagen");
    imagenProductoUrl = r.json.imagen;
  });

  await test("Productos: DELETE /{id} → 204", async () => {
    const r = await req("DELETE", `/api/productos/${productoId}`, { token: adminToken });
    assertEq(st(r), 204, "status");
  });

  // ============================ PEDIDOS (USUARIO) ============================
  await test("Pedidos user: POST /me sin auth → 401", async () => {
    const r = await req("POST", "/api/pedidos/me", {
      body: { destinatario: dest("noauth"), items: [{ productoId: 1, cantidad: 1, precioUnitario: 99.99 }] },
    });
    assertEq(st(r), 401, "status");
  });

  await test("Pedidos user: POST /me → 201 y guarda id", async () => {
    const r = await req("POST", "/api/pedidos/me", {
      token: userToken,
      body: { destinatario: dest("user"), items: [{ productoId: 1, cantidad: 2, precioUnitario: 99.99 }] },
    });
    assertEq(st(r), 201, "status");
    pedidoId = r.json?.id;
    assert(pedidoId, "falta id");
    assertEq(r.json?.estado, "PENDIENTE", "estado");
    assert(typeof r.json?.total === "number", "falta total");
  });

  await test("Pedidos user: GET /me → 200 array", async () => {
    const r = await req("GET", "/api/pedidos/me", { token: userToken });
    assertEq(st(r), 200, "status");
    assert(Array.isArray(r.json), "no es array");
  });

  await test("Pedidos user: GET /me/paged → 200 paginado", async () => {
    const r = await req("GET", "/api/pedidos/me/paged?page=1&size=2", { token: userToken });
    assertEq(st(r), 200, "status");
    assert(Array.isArray(r.json?.items), "items no es array");
    assert(typeof r.json?.totalCount === "number", "falta totalCount");
  });

  await test("Pedidos user: GET /me/{id} → 200", async () => {
    const r = await req("GET", `/api/pedidos/me/${pedidoId}`, { token: userToken });
    assertEq(st(r), 200, "status");
    assertEq(r.json?.id, pedidoId, "id");
  });

  await test("Pedidos user: PUT /me/{id} → 200 (PENDIENTE)", async () => {
    const r = await req("PUT", `/api/pedidos/me/${pedidoId}`, {
      token: userToken,
      body: { direccionEnvio: "Actualizado desde automation" },
    });
    assertEq(st(r), 200, "status");
    assertEq(r.json?.estado, "PENDIENTE", "estado");
  });

  await test("Pedidos user: GET /me/{id} de otro usuario → 403 Forbidden", async () => {
    assert(autoToken, "falta el token del signup automático");
    const r = await req("GET", `/api/pedidos/me/${pedidoId}`, { token: autoToken });
    assertEq(st(r), 403, "status (pedido de otro usuario)");
  });

  await test("Pedidos user: PUT /me/{id} de otro usuario → 403 Forbidden", async () => {
    const r = await req("PUT", `/api/pedidos/me/${pedidoId}`, {
      token: autoToken,
      body: { direccionEnvio: "Intento de edicion ajena" },
    });
    assertEq(st(r), 403, "status (pedido de otro usuario)");
  });

  await test("Pedidos user: GET /me/{id} inexistente → 404", async () => {
    const r = await req("GET", "/api/pedidos/me/999999999999999999999999", { token: userToken });
    assertEq(st(r), 404, "status");
  });

  await test("Pedidos user: PUT /me/{id} inexistente → 404", async () => {
    const r = await req("PUT", "/api/pedidos/me/999999999999999999999999", {
      token: userToken,
      body: { direccionEnvio: "No existe" },
    });
    assertEq(st(r), 404, "status");
  });

  await test("Pedidos user: POST /me con producto inexistente → 404", async () => {
    const r = await req("POST", "/api/pedidos/me", {
      token: userToken,
      body: { destinatario: dest("invalido"), items: [{ productoId: 999999, cantidad: 1, precioUnitario: 9.99 }] },
    });
    assertEq(st(r), 404, "status (error de negocio: producto no existe)");
  });

  await test("Pedidos user: DELETE /me/{id} (pedido extra) → 204", async () => {
    const post = await req("POST", "/api/pedidos/me", {
      token: userToken,
      body: { destinatario: dest("cancel"), items: [{ productoId: 1, cantidad: 1, precioUnitario: 99.99 }] },
    });
    assertEq(st(post), 201, "POST extra");
    assert(post.json?.id, "falta id del pedido extra");
    const r = await req("DELETE", `/api/pedidos/me/${post.json.id}`, { token: userToken });
    assertEq(st(r), 204, "status");
  });

  // ============================ PEDIDOS (ADMIN) ============================
  await test("Pedidos admin: GET / sin token → 401", async () => {
    const r = await req("GET", "/api/pedidos");
    assertEq(st(r), 401, "status");
  });

  await test("Pedidos admin: GET / rol USER → 403", async () => {
    const r = await req("GET", "/api/pedidos", { token: userToken });
    assertEq(st(r), 403, "status");
  });

  await test("Pedidos admin: GET / rol ADMIN → 200 array", async () => {
    const r = await req("GET", "/api/pedidos", { token: adminToken });
    assertEq(st(r), 200, "status");
    assert(Array.isArray(r.json), "no es array");
  });

  await test("Pedidos admin: GET /paged → 200 con header Link", async () => {
    const r = await req("GET", "/api/pedidos/paged?page=1&size=2", { token: adminToken });
    assertEq(st(r), 200, "status");
    assert(Array.isArray(r.json?.items), "items no es array");
    assert(r.headers.get("link"), "falta header Link");
  });

  await test("Pedidos admin: GET /{id} → 200", async () => {
    const r = await req("GET", `/api/pedidos/${pedidoId}`, { token: adminToken });
    assertEq(st(r), 200, "status");
    assertEq(r.json?.id, pedidoId, "id");
  });

  await test("Pedidos admin: PUT /{id}/estado → 200 ENVIADO", async () => {
    const r = await req("PUT", `/api/pedidos/${pedidoId}/estado`, {
      token: adminToken,
      body: { estado: "ENVIADO" },
    });
    assertEq(st(r), 200, "status");
    assertEq(r.json?.estado, "ENVIADO", "estado");
  });

  await test("Pedidos admin: PUT /{id} → 200 (actualizar completo)", async () => {
    const r = await req("PUT", `/api/pedidos/${pedidoId}`, {
      token: adminToken,
      body: { estado: "ENTREGADO", direccionEnvio: "Actualizado completo desde automation" },
    });
    assertEq(st(r), 200, "status");
    assertEq(r.json?.estado, "ENTREGADO", "estado");
  });

  await test("Pedidos admin: DELETE /{id} → 204", async () => {
    const r = await req("DELETE", `/api/pedidos/${pedidoId}`, { token: adminToken });
    assertEq(st(r), 204, "status");
  });

  await test("Pedidos admin: POST /me crea pedido propio → 201 PENDIENTE", async () => {
    const r = await req("POST", "/api/pedidos/me", {
      token: adminToken,
      body: {
        destinatario: {
          nombreCompleto: "Admin Automation",
          email: "admin-auto@test.com",
          telefono: "+34123456789",
          direccion: { calle: "Calle Admin", numero: "1", ciudad: "Madrid", provincia: "Madrid", pais: "España", codigoPostal: "28001" },
        },
        items: [{ productoId: 1, cantidad: 1, precioUnitario: 1299.99 }],
      },
    });
    assertEq(st(r), 201, "status");
    assertEq(r.json?.estado, "PENDIENTE", "estado");
    if (r.json?.id) { await req("DELETE", `/api/pedidos/me/${r.json.id}`, { token: adminToken }); }
  });

  // ============================ USERS (ADMIN) ============================
  await test("Users admin: GET / sin token → 401", async () => {
    const r = await req("GET", "/api/users");
    assertEq(st(r), 401, "status");
  });

  await test("Users admin: GET / rol USER → 403", async () => {
    const r = await req("GET", "/api/users", { token: userToken });
    assertEq(st(r), 403, "status");
  });

  await test("Users admin: GET / rol ADMIN → 200 paginado", async () => {
    const r = await req("GET", "/api/users?page=0&size=5", { token: adminToken });
    assertEq(st(r), 200, "status");
    assert(Array.isArray(r.json?.items), "items no es array");
    assert(r.json.totalCount >= 2, "semilla esperaba >=2 usuarios");
  });

  await test("Users admin: GET /999999 → 404", async () => {
    const r = await req("GET", "/api/users/999999", { token: adminToken });
    assertEq(st(r), 404, "status");
  });

  await test("Users admin: GET /{id} → 200 con datos", async () => {
    assert(userIdSignup, "falta el id del signup automático");
    const r = await req("GET", `/api/users/${userIdSignup}`, { token: adminToken });
    assertEq(st(r), 200, "status");
    assertEq(r.json?.id, userIdSignup, "id");
  });

  await test("Users admin: POST → 201 y guarda id", async () => {
    const r = await req("POST", "/api/users", {
      token: adminToken,
      body: { username: `autoemp_${ts}`, email: `autoemp_${ts}@test.com`, password: "User1234", role: "USER" },
    });
    assertEq(st(r), 201, "status");
    userIdCreado = r.json?.id;
    assert(userIdCreado !== undefined, "falta id");
    assertEq(r.json?.role, "USER", "role");
  });

  await test("Users admin: PUT /{id} → 200", async () => {
    const r = await req("PUT", `/api/users/${userIdCreado}`, {
      token: adminToken,
      body: { email: `autoemp_upd_${ts}@test.com` },
    });
    assertEq(st(r), 200, "status");
    assertEq(r.json?.email, `autoemp_upd_${ts}@test.com`, "email");
  });

  await test("Users admin: PATCH /{id}/avatar → 200", async () => {
    const r = await req("PATCH", `/api/users/${userIdCreado}/avatar`, {
      token: adminToken,
      body: { avatarUrl: "https://ejemplo.com/avatar.jpg" },
    });
    assertEq(st(r), 200, "status");
    assertEq(r.json?.avatar, "https://ejemplo.com/avatar.jpg", "avatar");
  });

  await test("Users admin: DELETE /{id} → 204", async () => {
    const r = await req("DELETE", `/api/users/${userIdCreado}`, { token: adminToken });
    assertEq(st(r), 204, "status");
  });

  // ============================ USERS (PERFIL) ============================
  await test("Users perfil: GET /me/profile → 200", async () => {
    const r = await req("GET", "/api/users/me/profile", { token: userToken });
    assertEq(st(r), 200, "status");
    assertEq(r.json?.username, CONFIG.user.username, "username");
  });

  await test("Users perfil: PUT /me/profile → 200 (email actual)", async () => {
    const get = await req("GET", "/api/users/me/profile", { token: userToken });
    assertEq(st(get), 200, "GET previo");
    const r = await req("PUT", "/api/users/me/profile", {
      token: userToken,
      body: { email: get.json.email }, // sin cambiar datos: idempotente
    });
    assertEq(st(r), 200, "status");
    assertEq(r.json?.email, get.json.email, "email");
  });

  await test("Users perfil: GET /me/profile sin token → 401", async () => {
    const r = await req("GET", "/api/users/me/profile");
    assertEq(st(r), 401, "status");
  });

  await test("Users perfil: PATCH /me/profile/avatar → 200", async () => {
    const r = await req("PATCH", "/api/users/me/profile/avatar", {
      token: userToken,
      body: { avatarUrl: "https://ejemplo.com/mi-avatar.jpg" },
    });
    assertEq(st(r), 200, "status");
    assertEq(r.json?.avatar, "https://ejemplo.com/mi-avatar.jpg", "avatar");
  });

  await test("Users perfil: DELETE /me/profile (cuenta dedicada) → 204", async () => {
    const su = await req("POST", "/api/v1/auth/signup", {
      body: { username: `del_${ts}`, email: `del_${ts}@test.com`, password: "Test1234!" },
    });
    assertEq(st(su), 201, "signup dedicada");
    assert(su.json?.token, "falta token de la cuenta dedicada");
    const r = await req("DELETE", "/api/users/me/profile", { token: su.json.token });
    assertEq(st(r), 204, "status");
  });

  // ============================ STORAGE ============================
  await test("Storage: GET /storage/productos/no-existe.png → 404", async () => {
    const r = await req("GET", "/storage/productos/no-existe.png");
    assertEq(st(r), 404, "status");
  });

  await test("Storage: GET imagen subida → 200", async () => {
    assert(imagenProductoUrl, "sin imagen subida (ejecuta el upload antes)");
    const url = imagenProductoUrl.startsWith("http") ? imagenProductoUrl.replace(CONFIG.baseUrl, "") : imagenProductoUrl;
    const r = await req("GET", url);
    assertEq(st(r), 200, "status");
  });

  // ============================ GRAPHQL ============================
  await test("GraphQL: query productos → 200 con datos", async () => {
    const r = await req("POST", "/graphql", {
      body: { query: "query { productos { id nombre precio stock } }" },
    });
    assertEq(st(r), 200, "status");
    assert(Array.isArray(r.json?.data?.productos), "data.productos no es array");
    assert(!r.json?.errors, `errors: ${JSON.stringify(r.json?.errors)}`);
  });

  await test("GraphQL: query categorias → 200 con datos", async () => {
    const r = await req("POST", "/graphql", {
      body: { query: "query { categorias { id nombre } }" },
    });
    assertEq(st(r), 200, "status");
    assert(Array.isArray(r.json?.data?.categorias), "data.categorias no es array");
    assert(!r.json?.errors, `errors: ${JSON.stringify(r.json?.errors)}`);
  });

  await test("GraphQL: query categoria(id:1) → 200 con nombre", async () => {
    const r = await req("POST", "/graphql", {
      body: { query: "query($id: Long!) { categoria(id: $id) { id nombre } }", variables: { id: 1 } },
    });
    assertEq(st(r), 200, "status");
    assert(r.json?.data?.categoria?.nombre, "falta data.categoria.nombre");
  });

  await test("GraphQL: query producto(id:1) → 200 con objeto", async () => {
    const r = await req("POST", "/graphql", {
      body: { query: "query($id: Long!) { producto(id: $id) { id nombre precio stock } }", variables: { id: 1 } },
    });
    assertEq(st(r), 200, "status");
    assert(r.json?.data?.producto && typeof r.json.data.producto === "object", "data.producto no es objeto");
    const pid = r.json.data.producto.id;
    assert(typeof pid === "number" || (typeof pid === "string" && pid.length > 0), `id inesperado: ${JSON.stringify(pid)}`);
  });

  await test("GraphQL: mutation createProducto sin auth → errors Unauthorized", async () => {
    const r = await req("POST", "/graphql", {
      body: {
        query: "mutation($input: CreateProductoInput!) { createProducto(input: $input) { id nombre } }",
        variables: { input: { nombre: `GQL ${ts}`, precio: 9.99, stock: 1, categoriaId: 1 } },
      },
    });
    assert(Array.isArray(r.json?.errors), `esperaba errors, llegó ${JSON.stringify(r.json).slice(0, 200)}`);
    assert(
      String(r.json.errors[0]?.message).toLowerCase().includes("authoriz"),
      `mensaje inesperado: ${r.json.errors[0]?.message}`
    );
  });

  await test("GraphQL: mutation createProducto con admin → 200 y guarda id", async () => {
    const nombre = `GQLOK ${ts}`;
    const r = await req("POST", "/graphql", {
      token: adminToken,
      body: {
        query: "mutation($input: CreateProductoInput!) { createProducto(input: $input) { id nombre } }",
        variables: { input: { nombre, precio: 9.99, stock: 1, categoriaId: 1 } },
      },
    });
    assertEq(st(r), 200, "status");
    assert(!r.json?.errors, `errors: ${JSON.stringify(r.json?.errors)}`);
    const creado = r.json?.data?.createProducto;
    assert(creado?.id, `falta id en ${JSON.stringify(r.json).slice(0, 200)}`);
    assertEq(creado.nombre, nombre, "nombre");
    productoIdCreadoGQL = creado.id; // lo consumen update/delete más abajo
  });

  await test("GraphQL: mutation createProducto con rol USER → errors authoriz", async () => {
    const r = await req("POST", "/graphql", {
      token: userToken,
      body: {
        query: "mutation($input: CreateProductoInput!) { createProducto(input: $input) { id } }",
        variables: { input: { nombre: `GQLUSER ${ts}`, precio: 1, stock: 1, categoriaId: 1 } },
      },
    });
    assert(Array.isArray(r.json?.errors), `esperaba errors: ${JSON.stringify(r.json).slice(0, 150)}`);
    assert(String(r.json.errors[0]?.message).toLowerCase().includes("authoriz"),
      `mensaje inesperado: ${r.json.errors[0]?.message}`);
  });

  await test("GraphQL: mutation createProducto con categoria inexistente → errors validación", async () => {
    // Por diseño el mutation lanza excepción en validación de negocio (ver XML-doc
    // de ProductoMutation): el cliente lo ve en errors[], no en data null.
    const r = await req("POST", "/graphql", {
      token: adminToken,
      body: {
        query: "mutation($input: CreateProductoInput!) { createProducto(input: $input) { id } }",
        variables: { input: { nombre: `GQLCAT ${ts}`, precio: 9.99, stock: 1, categoriaId: 999999 } },
      },
    });
    assert(Array.isArray(r.json?.errors), `esperaba errors: ${JSON.stringify(r.json).slice(0, 150)}`);
    assert(JSON.stringify(r.json.errors).includes("validaci"),
      `esperaba error de validación: ${JSON.stringify(r.json.errors).slice(0, 150)}`);
  });

  await test("GraphQL: mutation updateProducto con admin → 200 (regresión contexto)", async () => {
    // Regresión del bug "Cannot access a disposed context instance": el mutation
    // inyectaba el repositorio por constructor (singleton) y el DbContext quedaba
    // dispuesto tras la primera petición. updateProducto era el camino afectado.
    const pid = productoIdCreadoGQL ?? null;
    assert(pid, "falta producto creado por la mutation anterior");
    const r = await req("POST", "/graphql", {
      token: adminToken,
      body: {
        query: "mutation($id: Long!, $input: UpdateProductoInput!) { updateProducto(id: $id, input: $input) { id nombre precio } }",
        variables: { id: Number(pid), input: { precio: 11.11 } },
      },
    });
    assertEq(st(r), 200, "status");
    assert(!r.json?.errors, `errors: ${JSON.stringify(r.json?.errors)?.slice(0, 200)}`);
    assertEq(Number(r.json?.data?.updateProducto?.id), Number(pid), "id actualizado");
  });

  await test("GraphQL: mutation deleteProducto con admin → true + limpia estado", async () => {
    const pid = productoIdCreadoGQL ?? null;
    const r = await req("POST", "/graphql", {
      token: adminToken,
      body: {
        query: "mutation($id: Long!) { deleteProducto(id: $id) }",
        variables: { id: Number(pid) },
      },
    });
    assertEq(st(r), 200, "status");
    assert(!r.json?.errors, `errors: ${JSON.stringify(r.json?.errors)?.slice(0, 200)}`);
    assertEq(r.json?.data?.deleteProducto, true, "resultado delete");
  });
  // ============================ REALTIME (WebSocket + SignalR) ============================
  // Node 22+ trae cliente WebSocket global (sin npm install, mantiene la filosofía
  // del runner). SignalR se habla a mano: handshake JSON + separador de registro.
  await test("Realtime WS: /ws/productos (público) recibe broadcast al crear producto", async () => {
    const sock = await wsOpen(wsUrl("/ws/productos"));
    const waiting = wsNextMessage(sock, 6000);
    const r = await req("POST", "/api/productos", {
      token: adminToken,
      body: { nombre: `WS ${ts}`, descripcion: "Broadcast E2E", precio: 7.77, stock: 1, categoriaId: 1 },
    });
    assertEq(st(r), 201, "status producto");
    const msg = await waiting;
    assert(msg.includes('"entity":"productos"'), `mensaje inesperado: ${msg.slice(0, 150)}`);
    assert(msg.includes(String(r.json.id)) || msg.includes(`WS ${ts}`),
      `el broadcast no contiene el producto creado: ${msg.slice(0, 200)}`);
    const del = await req("DELETE", `/api/productos/${r.json.id}`, { token: adminToken });
    assertEq(st(del), 204, "cleanup");
    sock.close();
  });

  await test("Realtime WS: /ws/pedidos sin token → cierre 1008 (Token requerido)", async () => {
    const sock = new WebSocket(wsUrl("/ws/pedidos"));
    const close = await wsWaitClose(sock, 5000);
    assertEq(close.code, 1008, "close code (PolicyViolation)");
    assert(String(close.reason).toLowerCase().includes("token"), `razón de cierre: ${close.reason}`);
  });

  await test("Realtime WS: /ws/pedidos con token admin → conexión estable", async () => {
    const sock = await wsOpen(`${wsUrl("/ws/pedidos")}?token=${encodeURIComponent(adminToken)}`);
    await wait(1200);
    assertEq(sock.readyState, WebSocket.OPEN, "el servidor cerró una conexión autenticada");
    sock.close();
  });

  await test("Realtime SignalR: /hubs/productos handshake + invocación ProductoCreado", async () => {
    const sock = await wsOpen(wsUrl("/hubs/productos"));
    sock.send(`{"protocol":"json","version":1}${SEP}`);
    const hs = await wsNextMessage(sock, 5000);
    assert(hs.includes("{}"), `handshake inesperado: ${hs.slice(0, 120)}`);
    // Suscripción implícita: el hub emite a Clients.All; esperamos la invocación
    const waiting = wsNextMessage(sock, 6000);
    const r = await req("POST", "/api/productos", {
      token: adminToken,
      body: { nombre: `SIGNALR ${ts}`, descripcion: "Invocación E2E", precio: 8.88, stock: 1, categoriaId: 1 },
    });
    assertEq(st(r), 201, "status producto");
    const msg = await waiting;
    assert(msg.includes("ProductoCreado"), `esperaba invocación ProductoCreado: ${msg.slice(0, 200)}`);
    assert(msg.includes(String(r.json.id)) || msg.includes(`SIGNALR ${ts}`),
      `la invocación no contiene el producto: ${msg.slice(0, 200)}`);
    const del = await req("DELETE", `/api/productos/${r.json.id}`, { token: adminToken });
    assertEq(st(del), 204, "cleanup");
    sock.close();
  });

  await test("Realtime SignalR: /hubs/pedidos [Authorize] sin token → rechazo en el upgrade (401)", async () => {
    // [Authorize]: el servidor rechaza el upgrade HTTP con 401; el WebSocket
    // ni siquiera llega a abrirse (onerror, no un handshake con error)
    let rejected = false;
    try {
      const sock = await wsOpen(wsUrl("/hubs/pedidos"), { timeoutMs: 4000 });
      sock.close();
    } catch {
      rejected = true;
    }
    assert(rejected, "el hub autorizado aceptó una conexión sin token");
  });

  // ============================ HISTORIAL Y FEED (Cassandra) ============================
  // Los flujos nuevos de las series (Q-A) y el feed (Q-B). NO se hace ningun
  // signin nuevo (usan adminToken/userToken ya guardados) para respetar auth 10/min.
  let serieAntes = 0;
  let precioNuevo = null;

  await test("Historial: GET serie seed (anon) 200 con >=3 puntos ordenados", async () => {
    const r = await req("GET", "/api/productos/1/historial-precios?limit=50");
    assertEq(st(r), 200, "status");
    assert(Array.isArray(r.json), "no es array");
    assert(r.json.length >= 3, `la semilla esperaba >=3 puntos, hay ${r.json.length}`);
    // Orden DESC garantizado por la CLUSTERING KEY, sin ORDER BY en el servidor.
    for (let i = 1; i < r.json.length; i++) {
      assert(new Date(r.json[i - 1].fecha) >= new Date(r.json[i].fecha),
        `la serie no viene ordenada DESC en la posicion ${i}`);
    }
    serieAntes = r.json.length;
  });

  await test("Historial: GET rango invertido 400 con errors.desde", async () => {
    const r = await req("GET", "/api/productos/1/historial-precios?desde=2026-10-10&hasta=2026-09-01");
    assertEq(st(r), 400, "status");
    assert(r.json?.errors?.desde, `falta errors.desde: ${r.text.slice(0, 150)}`);
  });

  await test("Historial: GET id inexistente 404 (lo decide PostgreSQL)", async () => {
    const r = await req("GET", "/api/productos/999999/historial-precios");
    assertEq(st(r), 404, "status");
    assert(r.json?.message, "falta message");
  });

  await test("Historial: PUT precio 200 y la serie crece con el punto nuevo PRIMERO", async () => {
    // Precio garantizado DISTINTO al actual: el historial ahora solo registra
    // cambios reales de precio (no eventos de edicion), asi que leemos el
    // precio vigente y variamos - random puro podria coincidir y no habria punto.
    const act = await req("GET", "/api/productos/1");
    assertEq(st(act), 200, "GET precio actual");
    precioNuevo = Math.round((act.json.precio + 1 + Math.random() * 500) * 100) / 100;
    const put = await req("PUT", "/api/productos/1", {
      token: adminToken,
      body: {
        nombre: "Laptop Dell XPS 15", descripcion: "Laptop de alto rendimiento",
        precio: precioNuevo, stock: 10, categoriaId: 1,
      },
    });
    assertEq(st(put), 200, "status PUT");
    assertEq(put.json?.precio, precioNuevo, "precio del PUT");

    const r = await req("GET", "/api/productos/1/historial-precios?limit=50");
    assertEq(st(r), 200, "status GET serie");
    assert(r.json.length >= serieAntes + 1,
      `la serie no crecio tras el update (${serieAntes} -> ${r.json.length})`);
    assertEq(r.json[0].precio, precioNuevo,
      "el punto mas reciente es el precio recien escrito (fail-open)");
  });

  await test("Feed: POST vista de 3 productos 204 cada una", async () => {
    for (const pid of [1, 3, 2]) {
      const r = await req("POST", `/api/productos/${pid}/vista`, { token: userToken });
      assertEq(st(r), 204, `status POST vista ${pid}`);
    }
  });

  await test("Feed: GET /me/feed 200 con <=20 entradas ordenadas y nombre copiado", async () => {
    const r = await req("GET", "/api/users/me/feed", { token: userToken });
    assertEq(st(r), 200, "status");
    assert(Array.isArray(r.json), "no es array");
    assert(r.json.length >= 3, `esperaba al menos las 3 vistas del flujo, hay ${r.json.length}`);
    assert(r.json.length <= 20, `limit por defecto (20) excedido: ${r.json.length}`);
    for (let i = 1; i < r.json.length; i++) {
      assert(new Date(r.json[i - 1].fecha) >= new Date(r.json[i].fecha),
        `feed no ordenado DESC en la posicion ${i}`);
    }
    const conNombre = r.json.find((e) => e.productoId === 1);
    assert(conNombre?.nombre, "la entrada del feed debe traer el nombre denormalizado");
  });

  await test("Feed: GET /me/feed?limit=5 acota la particion a 5", async () => {
    const r = await req("GET", "/api/users/me/feed?limit=5", { token: userToken });
    assertEq(st(r), 200, "status");
    assert(r.json.length <= 5, `limit=5 excedido: ${r.json.length}`);
  });

  await test("Feed: GET /me/feed sin token 401", async () => {
    const r = await req("GET", "/api/users/me/feed");
    assertEq(st(r), 401, "status");
  });

  await test("Feed: POST vista de producto inexistente 404", async () => {
    const r = await req("POST", "/api/productos/999999/vista", { token: userToken });
    assertEq(st(r), 404, "status");
  });

  // ============================ LIMPIEZA ============================
  await test("Limpieza: DELETE signup automático → 204/404", async () => {
    if (!userIdSignup) return; // signup falló arriba (ya reportado)
    const r = await req("DELETE", `/api/users/${userIdSignup}`, { token: adminToken });
    assert(st(r) === 204 || r.status === 404, `status ${r.status}`);
  });
}

async function main() {
  log(`\n=== ${CONFIG.nombre} · test-runner (Node nativo) ===\n`);
  log(`▶ baseUrl: ${CONFIG.baseUrl}`);
  let procs = [];
  let startedServices = [];
  const external = !!process.env.BASE_URL;
  let yaResponde = false;

  try {
    if (!external) {
      yaResponde = !!(await fetchAny(CONFIG.baseUrl + "/health")) || !!(await fetchAny(CONFIG.baseUrl + "/swagger"));

      if (!yaResponde) {
        // Infraestructura: solo levantar servicios de BD que no estén corriendo
        if (existsSync(path.join(ROOT, CONFIG.composeFile)) && dockerAvailable()) {
          for (const s of CONFIG.dbServices) {
            if (!serviceRunning(s)) startedServices.push(s);
          }
          if (startedServices.length) {
            log(`▶ Levantando infraestructura: ${startedServices.join(", ")}`);
            run(`docker compose -f ${CONFIG.composeFile} up -d ${startedServices.join(" ")}`);
            await sleep(2000);
          } else {
            log("▶ Infraestructura ya corriendo (no se toca)");
          }
        }

        log("▶ dotnet restore");
        run(`dotnet restore "${CONFIG.csproj}"`);
        log("▶ dotnet build");
        run(`dotnet build "${CONFIG.csproj}" -c Debug --no-restore`);
        log("▶ dotnet run (segundo plano, --no-launch-profile)");
        procs.push(spawnApi());

        let listo = await waitForApi(30000);
        if (!listo && existsSync(path.join(ROOT, CONFIG.composeFile)) && dockerAvailable()) {
          console.warn("  ⚠️ dotnet run no responde → fallback docker compose up -d --build");
          procs.forEach((p) => killTree(p.pid));
          procs = [];
          log(`▶ Fallback: docker compose -f ${CONFIG.composeFile} up -d --build`);
          run(`docker compose -f ${CONFIG.composeFile} up -d --build`);
          listo = await waitForApi(30000);
        }
        if (!listo) throw new Error("La API no respondió en 30s");
      } else {
        log("▶ API ya responde en el baseUrl configurado → modo adjuntar (sin build)");
      }
    } else {
      log("▶ BASE_URL definido → modo externo (solo suite)");
      const listo = await waitForApi(10000);
      if (!listo) throw new Error(`No responde en ${CONFIG.baseUrl}`);
    }

    log("\n▶ Ejecutando suite de tests HTTP...\n");
    await runSuite();
  } catch (e) {
    logErr(`Error fatal: ${e.message}`);
    exitCode = 1;
  } finally {
    log("\n▶ Limpieza...");
    procs.forEach((p) => killTree(p.pid));
    // NO `docker compose down -v`: solo paramos los servicios de BD que
    // levantamos nosotros; los que ya estaban corriendo se quedan.
    if (startedServices.length) {
      try { run(`docker compose -f ${CONFIG.composeFile} stop ${startedServices.join(" ")}`); }
      catch (e) { logErr(`stop: ${e.message}`); }
    }
  }

  const pass = resultados.filter((r) => r.ok).length;
  const fail = resultados.length - pass;
  // Reporte estructurado: los orquestadores leen SIEMPRE este JSON, jamás la consola.
  try {
    const { writeFileSync } = await import("node:fs");
    const outDir = path.join(__dirname, "reports");
    if (!existsSync(outDir)) (await import("node:fs")).mkdirSync(outDir, { recursive: true });
    writeFileSync(
      path.join(outDir, "report.json"),
      JSON.stringify({ total: resultados.length, passed: pass, failed: fail, resultados }, null, 2),
      "utf8"
    );
  } catch { /* el reporte no debe enmascarar fallos de la suite */ }
  log("\n========== RESUMEN ==========");
  log(`Total: ${resultados.length} · OK: ${pass} · KO: ${fail}`);
  for (const r of resultados.filter((r) => !r.ok)) log(`  ✗ ${r.nombre}: ${r.error}`);
  log("==============================\n");
  process.exit(exitCode);
}
main();
