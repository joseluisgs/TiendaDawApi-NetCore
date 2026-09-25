#!/usr/bin/env node
/**
 * Automation E2E — TiendaApi .NET (Fase 7 del plan FASES-MEJORAS.md)
 *
 * Estilo UD02 ejemplos/<nn>-ProductosX/automation/test-runner.mjs: Node nativo, SIN npm install.
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
 * Diseño (FASES-MEJORAS.md §Fase 7):
 *   - NO ejecuta `docker compose down -v` al final: solo para los servicios de BD
 *     que ÉL haya levantado (si ya estaban corriendo, no se tocan) → no rompe
 *     el entorno de desarrollo.
 *   - Fallback: si `dotnet run` no responde, intenta `docker compose up -d --build`.
 *   - Compatible con Fase 1: espera /health primero; si falla acepta /swagger
 *     o /api/productos.
 *   - Rate limit awareness: helper st() → falla CLARO si la API devuelve 429
 *     (100/15s general · 10/min auth · 20/min POST · 200/min graphql).
 *   - Credenciales seed: admin/admin, userdaw/userdaw.
 *   - Sale con código 0 si todo OK, 1 si algo falla (listo para CI).
 */

import { spawn, spawnSync } from "node:child_process";
import { existsSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(__dirname, "..", "..");

const CONFIG = {
  nombre: "TiendaApi .NET · Automation E2E (Fase 7)",
  csproj: "TiendaApi.Api/TiendaApi.csproj",
  baseUrl: process.env.BASE_URL || "http://localhost:5031",
  healthPaths: ["/health", "/swagger", "/api/productos"],
  env: {
    ASPNETCORE_ENVIRONMENT: "Development",
    ASPNETCORE_URLS: "http://localhost:5031",
  },
  dbServices: ["postgres", "mongodb"],
  composeFile: "docker-compose.local.yml",
  // Credenciales seed (§7.4 del plan)
  admin: { username: process.env.ADMIN_USER || "admin", password: process.env.ADMIN_PASS || "admin" },
  user: { username: process.env.USER_USER || "userdaw", password: process.env.USER_PASS || "userdaw" },
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
    return (res.stdout || "").split(/\r?\n/).includes(name);
  } catch { return false; }
}
function killTree(pid) {
  if (!pid) return;
  try { spawnSync("taskkill", ["/pid", String(pid), "/T", "/F"], { shell: true }); }
  catch { try { process.kill(pid); } catch { } }
}
function spawnApi() {
  // --no-launch-profile: launchSettings.json fuerza Development y su propio puerto;
  // aquí controlamos entorno y URL explícitamente (requisito de §Fase 8/7).
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
  if (body !== undefined) h["Content-Type"] = h["Content-Type"] || "application/json";
  if (token) h.Authorization = `Bearer ${token}`;
  if (ifNoneMatch) h["If-None-Match"] = ifNoneMatch;
  const res = await fetch(CONFIG.baseUrl + urlPath, {
    method,
    headers: h,
    body: body !== undefined ? (typeof body === "string" ? body : JSON.stringify(body)) : undefined,
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

// Rate limit awareness (§7.3): el 429 NUNCA se acepta como OK.
function st(r) {
  assert(
    r.status !== 429,
    "Rate limit 429 alcanzado: la suite superó un límite de peticiones " +
    "(100/15s general · 10/min auth · 20/min POST). Espera ~1 min y relanza."
  );
  return r.status;
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

  // ============================ HEALTH ============================
  await test("Health: GET /health → 200 con JSON status", async () => {
    const r = await req("GET", "/health");
    assertEq(st(r), 200, "status");
    assert(r.json && typeof r.json === "object", "la respuesta no es JSON");
    assert(r.json.status !== undefined, "falta la propiedad status");
  });

  // ============================ AUTH ============================
  await test("Auth: POST signup usuario único → 201 con token", async () => {
    const r = await req("POST", "/api/v1/auth/signup", {
      body: { username: `auto_${ts}`, email: `auto_${ts}@test.com`, password: "Test1234" },
    });
    assertEq(st(r), 201, "status");
    assert(r.json?.token, "falta token");
    assertEq(r.json?.user?.role, "USER", "role");
    userIdSignup = r.json?.user?.id;
  });

  await test("Auth: POST signup con username inválido → 400", async () => {
    const r = await req("POST", "/api/v1/auth/signup", {
      body: { username: "x", email: `bad_${ts}@test.com`, password: "Test1234" },
    });
    assertEq(st(r), 400, "status");
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
    assertEq(st(r), 401, "status");
  });

  // ============================ CATEGORÍAS ============================
  await test("Categorías: GET paged → 200 con items y paginación", async () => {
    const r = await req("GET", "/api/categorias?page=0&size=2");
    assertEq(st(r), 200, "status");
    assert(Array.isArray(r.json?.items), "items no es array");
    assert(typeof r.json?.totalCount === "number", "falta totalCount");
    assert(r.json.totalCount >= 3, `semilla esperaba >=3 categorías, hay ${r.json.totalCount}`);
  });

  await test("Categorías: GET /1 → 200 (semilla)", async () => {
    const r = await req("GET", "/api/categorias/1");
    assertEq(st(r), 200, "status");
    assert(r.json?.id !== undefined, "falta id");
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
    assert(r.headers.get("link"), "falta header Link (Fase 3)");
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

  await test("Pedidos admin: DELETE /{id} → 204", async () => {
    const r = await req("DELETE", `/api/pedidos/${pedidoId}`, { token: adminToken });
    assertEq(st(r), 204, "status");
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

  // ============================ STORAGE ============================
  await test("Storage: GET /storage/productos/no-existe.png → 404", async () => {
    const r = await req("GET", "/storage/productos/no-existe.png");
    assertEq(st(r), 404, "status");
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
    // levantamos nosotros; los que ya estaban corriendo se quedan (§Fase 7).
    if (startedServices.length) {
      try { run(`docker compose -f ${CONFIG.composeFile} stop ${startedServices.join(" ")}`); }
      catch (e) { logErr(`stop: ${e.message}`); }
    }
  }

  const pass = resultados.filter((r) => r.ok).length;
  const fail = resultados.length - pass;
  log("\n========== RESUMEN ==========");
  log(`Total: ${resultados.length} · OK: ${pass} · KO: ${fail}`);
  for (const r of resultados.filter((r) => !r.ok)) log(`  ✗ ${r.nombre}: ${r.error}`);
  log("==============================\n");
  process.exit(exitCode);
}
main();
