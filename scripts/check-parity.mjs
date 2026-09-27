#!/usr/bin/env node
/**
 * check-parity.mjs — Paridad de endpoints y colecciones E2E entre ambos repos.
 *
 * Valida tres bloques:
 *   1. ENDPOINTS  — rutas (controllers + minimal APIs) idénticas entre los dos repos.
 *   2. COLECCIONES — peticiones por carpeta: Newman vs Bruno-Cli (mismo nº por carpeta).
 *      La carpeta WEBSOCKETS solo existe en Bruno (Newman no soporta WS) → se permite.
 *   3. FICHEROS   — hashes (md5) de las colecciones E2E idénticos entre los dos repos.
 *
 * Uso:  node scripts/check-parity.mjs        (desde cualquiera de los dos repos)
 * Exit: 0 = todo OK · 1 = hay discrepancias.
 */
import fs from "fs";
import path from "path";
import crypto from "crypto";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoActual = path.resolve(__dirname, "..");
const baseDir = path.dirname(repoActual);

const NOMBRES = ["TiendaDawApi-NetCore", "TiendaDawApi-Cqrs-MediatR-NetCore"];
const rutas = NOMBRES.map((n) => path.join(baseDir, n)).filter((p) => fs.existsSync(p));
const [repoA, repoB] = rutas;

let errores = 0;
const ok = (msg) => console.log(`  [OK]    ${msg}`);
const fail = (msg) => { errores++; console.log(`  [ERROR] ${msg}`); };
const aviso = (msg) => console.log(`  [AVISO] ${msg}`);
const bloque = (t) => console.log(`\n=== ${t} ===`);

function md5(file) {
  return crypto.createHash("md5").update(fs.readFileSync(file)).digest("hex");
}

function walk(dir, out = []) {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, e.name);
    if (e.isDirectory()) walk(p, out);
    else out.push(p);
  }
  return out;
}

// ---------- 1. ENDPOINTS: controllers + minimal APIs ----------
function collectRoutes(root) {
  const apiDir = path.join(root, "TiendaApi.Api");
  const routes = new Set();
  for (const f of walk(apiDir)) {
    if (!f.endsWith(".cs")) continue;
    const txt = fs.readFileSync(f, "utf8");

    // Minimal APIs: app.MapGet("/path", ...) / endpoints.MapPost("/graphql", ...)
    for (const m of txt.matchAll(/\.Map(Get|Post|Put|Patch|Delete)(?:<[^>]+>)?\(\s*"([^"]+)"/g)) {
      routes.add(`${m[1].toUpperCase()} ${norm(m[2])}`);
    }

    // Controllers: [Route("api/x")] de clase + [HttpVerb("ruta")] de método
    if (!f.includes(`${path.sep}Controllers${path.sep}`)) continue;
    const classRoute = txt.match(/\[Route\("([^"]*)"\)\]/);
    const base = classRoute ? classRoute[1] : "";
    for (const m of txt.matchAll(/\[Http(Get|Post|Put|Patch|Delete)(?:\("([^"]*)"\))?\]/g)) {
      const sub = m[2] ?? "";
      routes.add(`${m[1].toUpperCase()} ${norm(path.posix.join(base, sub))}`);
    }
  }
  return routes;
}

function norm(p) {
  let r = p.replace(/\\/g, "/").replace(/\/+/g, "/");
  if (!r.startsWith("/")) r = "/" + r;
  if (r.length > 1 && r.endsWith("/")) r = r.slice(0, -1);
  return r;
}

function diffSets(a, b) {
  const soloA = [...a].filter((x) => !b.has(x)).sort();
  const soloB = [...b].filter((x) => !a.has(x)).sort();
  return { soloA, soloB };
}

// ---------- 2. COLECCIONES: peticiones por carpeta ----------
function newmanFolders(root) {
  const col = JSON.parse(
    fs.readFileSync(path.join(root, "TiendaApi.Tests.E2E", "Postman-Cli", "TiendaApi.NetCore.postman_collection.json"), "utf8"),
  );
  const count = (items) => items.reduce((n, i) => n + (i.item ? count(i.item) : 1), 0);
  return col.item.map((f) => ({ name: normFolder(f.name), count: count(f.item || [f]) }));
}

function brunoFolders(root) {
  const dir = path.join(root, "TiendaApi.Tests.E2E", "Bruno-Cli");
  const out = [];
  for (const d of fs.readdirSync(dir, { withFileTypes: true })) {
    if (!d.isDirectory()) continue;
    const bru = walk(path.join(dir, d.name)).filter((f) => f.endsWith(".bru"));
    const reqs = bru.filter((f) => {
      const head = fs.readFileSync(f, "utf8").slice(0, 400);
      return /type:\s*(http|graphql|ws)\b/.test(head);
    });
    if (reqs.length > 0) out.push({ name: normFolder(d.name), count: reqs.length });
  }
  return out;
}

function normFolder(n) {
  return n.replace(/^\d+\s*-\s*/, "").trim().toUpperCase();
}

// ---------- 3. HASHES de colecciones entre repos ----------
function collectionHashes(root) {
  const base = path.join(root, "TiendaApi.Tests.E2E");
  const map = new Map();
  for (const f of walk(base)) {
    const rel = path.relative(base, f).replace(/\\/g, "/");
    if (/results\.json$/.test(rel)) continue; // salidas de corridas (solo CQRS)
    map.set(rel, md5(f));
  }
  return map;
}

// ================================ main ================================
console.log("check-parity.mjs — paridad endpoints + colecciones E2E entre repos");
if (!repoA || !repoB) {
  console.error("No se encontraron ambos repos hermanos.");
  process.exit(1);
}
console.log(`  A: ${repoA}`);
console.log(`  B: ${repoB}`);

bloque("1. ENDPOINTS (controllers + minimal APIs)");
const rA = collectRoutes(repoA);
const rB = collectRoutes(repoB);
const { soloA, soloB } = diffSets(rA, rB);
if (soloA.length === 0 && soloB.length === 0) {
  ok(`rutas idénticas entre ambos repos (${rA.size} rutas)`);
} else {
  for (const r of soloA) fail(`solo en ${path.basename(repoA)}: ${r}`);
  for (const r of soloB) fail(`solo en ${path.basename(repoB)}: ${r}`);
}

bloque("2. COLECCIONES (Newman vs Bruno-Cli, por carpeta)");
const nf = newmanFolders(repoA);
const bf = brunoFolders(repoB);
const bfMap = new Map(bf.map((f) => [f.name, f.count]));
const nfMap = new Map(nf.map((f) => [f.name, f.count]));
for (const f of nf) {
  if (f.name === "WEBSOCKETS") continue; // solo Newman: no aplica (ver abajo)
  if (!bfMap.has(f.name)) fail(`carpeta Newman sin equivalente en Bruno-Cli: ${f.name}`);
  else if (bfMap.get(f.name) !== f.count) fail(`${f.name}: Newman=${f.count} Bruno-Cli=${bfMap.get(f.name)}`);
  else ok(`${f.name}: ${f.count} peticiones`);
}
for (const f of bf) {
  if (!nfMap.has(f.name)) {
    if (f.name === "WEBSOCKETS") aviso(`WEBSOCKETS (solo Bruno-Cli): ${f.count} peticiones — Newman no soporta WS`);
    else fail(`carpeta Bruno-Cli sin equivalente en Newman: ${f.name}`);
  }
}
const totalN = nf.reduce((n, f) => n + f.count, 0);
const totalB = bf.reduce((n, f) => n + f.count, 0);
const wsB = (bf.find((f) => f.name === "WEBSOCKETS")?.count) ?? 0;
if (totalN === totalB - wsB) ok(`totales: Newman=${totalN}, Bruno-Cli=${totalB} (=${totalN} + ${wsB} WS)`);
else fail(`totales no cuadran: Newman=${totalN}, Bruno-Cli=${totalB} (WS=${wsB})`);

bloque("3. HASHES de colecciones E2E entre repos");
const hA = collectionHashes(repoA);
const hB = collectionHashes(repoB);
let distintos = 0;
for (const [rel, h] of hA) {
  if (!hB.has(rel)) { fail(`falta en ${path.basename(repoB)}: ${rel}`); distintos++; }
  else if (hB.get(rel) !== h) { fail(`hash distinto: ${rel}`); distintos++; }
}
for (const rel of hB.keys()) {
  if (!hA.has(rel) && !/results\.json$/.test(rel)) { fail(`falta en ${path.basename(repoA)}: ${rel}`); distintos++; }
}
if (distintos === 0) ok(`${hA.size} ficheros de colección idénticos entre ambos repos`);

console.log(`\n${errores === 0 ? "TODO OK" : `ERRORES: ${errores}`}`);
process.exit(errores === 0 ? 0 : 1);
