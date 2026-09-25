#!/usr/bin/env node
// check-docs.mjs — validador de consistencia entre FASES-MEJORAS.md, BITACORA.md y README.md
// Uso: node scripts/check-docs.mjs   (sale con 1 si algo falla, 0 si todo OK)
// Sin dependencias externas (Node 18+).

import { execSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const read = (f) => readFileSync(join(root, f), "utf8");

let errors = 0;
let warnings = 0;
const ok = (m) => console.log(`  OK ${m}`);
const fail = (m) => { errors++; console.error(`  FALLO ${m}`); };
const warn = (m) => { warnings++; console.log(`  aviso ${m}`); };

const fases = read("FASES-MEJORAS.md");
const bitacora = read("BITACORA.md");
const readme = read("README.md");

// ---------------------------------------------------------------------------
console.log("\n[1] BITACORA: indice vs secciones (mismo conjunto, orden y hashes)");

// Secciones: ## Fase N - Titulo (`hash`) OK  |  ## PostgreSQL 17 (`hash`) OK
const secciones = [];
for (const line of bitacora.split(/\r?\n/)) {
  const m = line.match(/^## (?:Fase (\d+)|PostgreSQL 17)\b.*\(`([0-9a-f]{7})`\)/);
  if (m) secciones.push({ id: m[1] ?? "PG17", hash: m[2] });
}

// Filas del indice: | 0 - Baseline | `hash` ... | OK |  /  | PG 17 | ...
const filas = [];
for (const line of bitacora.split(/\r?\n/)) {
  if (!/^\|/.test(line)) continue;
  const celdas = line.split("|").slice(1, -1).map((s) => s.trim());
  if (celdas.length < 3) continue;
  const c1 = celdas[0];
  let id = null;
  const mm = c1.match(/^(\d+)\s*[·]/);
  if (mm) id = mm[1];
  else if (/^PG 17/.test(c1)) id = "PG17";
  if (!id) continue;
  const h = line.match(/`([0-9a-f]{7})`/);
  filas.push({ id, hash: h ? h[1] : null, estado: celdas[celdas.length - 1], c1 });
}

if (filas.length !== secciones.length) {
  fail(`indice: ${filas.length} filas | secciones: ${secciones.length}`);
} else {
  ok(`indice y secciones: ${filas.length} entradas`);
}
let ordenOk = filas.length === secciones.length;
for (let i = 0; i < Math.min(filas.length, secciones.length); i++) {
  const f = filas[i], s = secciones[i];
  if (f.id !== s.id) { ordenOk = false; fail(`orden [${i}]: indice -> "${f.c1}" pero seccion -> id ${s.id}`); }
  else if (f.hash !== s.hash) { ordenOk = false; fail(`hash [${f.id}]: indice \`${f.hash}\` != seccion \`${s.hash}\``); }
}
if (ordenOk) ok("orden y hashes del indice coinciden con las secciones");
for (const f of filas) if (!/✅|OK/.test(f.estado)) fail(`fila indice "${f.c1}" sin marca OK`);
if (filas.every((f) => /✅|OK/.test(f.estado))) ok("todas las filas del indice marcadas OK");

// ---------------------------------------------------------------------------
console.log("\n[2] Hashes citados existen en git log");

let commits = new Set();
try {
  commits = new Set(execSync("git log --format=%h", { cwd: root, encoding: "utf8" }).split(/\s+/).filter(Boolean));
} catch {
  warn("git no disponible -> comprobacion de hashes omitida");
}
if (commits.size) {
  let total = 0, bad = 0;
  for (const [name, txt] of [["FASES-MEJORAS.md", fases], ["BITACORA.md", bitacora]]) {
    const cited = new Set([...txt.matchAll(/`([0-9a-f]{7})`/g)].map((m) => m[1]));
    for (const h of cited) {
      total++;
      if (!commits.has(h)) { bad++; fail(`${name}: hash \`${h}\` no existe en la rama`); }
    }
  }
  if (!bad) ok(`${total} hashes citados existen en git log`);
}

// ---------------------------------------------------------------------------
console.log("\n[3] FASES-MEJORAS.md: fases 0-12 presentes y COMPLETADAS");

const fasesSecs = [...fases.matchAll(/^## Fase (\d+)\b[^\n]*/gm)].map((m) => ({ num: +m[1], head: m[0] }));
const nums = fasesSecs.map((s) => s.num);
const esperado = Array.from({ length: 13 }, (_, i) => i);
for (const n of esperado) if (!nums.includes(n)) fail(`falta -> ## Fase ${n}`);
for (const s of fasesSecs) {
  const start = fases.indexOf(s.head);
  const fin = fases.indexOf("\n## ", start + 1);
  const bloque = fases.slice(start, fin === -1 ? undefined : fin);
  if (!/COMPLETADA/.test(bloque)) fail(`Fase ${s.num} sin "COMPLETADA"`);
}
if (esperado.every((n) => nums.includes(n)) && new Set(nums).size === nums.length) {
  ok("fases 0-12 presentes, unicas y COMPLETADAS");
}

// ---------------------------------------------------------------------------
console.log("\n[4] Conjunto de fases: FASES vs BITACORA");

const numsBit = secciones.filter((s) => s.id !== "PG17").map((s) => +s.id);
const soloF = nums.filter((n) => !numsBit.includes(n));
const soloB = numsBit.filter((n) => !nums.includes(n));
if (soloF.length) fail(`en FASES pero no en BITACORA: ${soloF.join(", ")}`);
if (soloB.length) fail(`en BITACORA pero no en FASES: ${soloB.join(", ")}`);
if (!soloF.length && !soloB.length) ok("mismo conjunto de fases (BITACORA anade PostgreSQL 17 aparte)");

// ---------------------------------------------------------------------------
console.log("\n[5] Texto de 'Fases pendientes' coherente con el conteo");

const p = bitacora.match(/las (\d+) fases del plan \(0-(\d+)\)/);
if (!p) fail("no se encontro el enunciado 'las N fases del plan (0-M)'");
else {
  const n = +p[1], max = +p[2];
  const maxReal = Math.max(...nums);
  if (n !== fasesSecs.length) fail(`dice ${n} fases pero FASES tiene ${fasesSecs.length}`);
  else if (max !== maxReal) fail(`dice 0-${max} pero el maximo es ${maxReal}`);
  else ok(`"${n} fases del plan (0-${max})" coherente`);
}

// ---------------------------------------------------------------------------
console.log("\n[6] README.md: TOC vs headings (anclas de GitHub)");

// Regla de GitHub: minusculas, quitar puntuacion/emoji (conservar letras,
// numeros, marcas combinantes como FE0F, guiones y bajos), espacios -> guion.
const anchor = (h) => h
  .replace(/^#+\s+/, "")
  .toLowerCase()
  .replace(/[^\p{L}\p{N}\p{M}\s\-_]/gu, "")
  .replace(/\s/g, "-");

const lineas = readme.split(/\r?\n/);
const setHeads = new Set(lineas.filter((l) => /^#{1,4} /.test(l)).map(anchor));
const iToc = lineas.findIndex((l) => /^## .*Tabla de Contenidos/.test(l));
if (iToc === -1) fail("no se encontro la seccion Tabla de Contenidos");
else {
  const rotas = [];
  for (let i = iToc + 1; i < lineas.length && !/^## /.test(lineas[i]); i++) {
    for (const m of lineas[i].matchAll(/\]\(#([^)]+)\)/g)) {
      if (!setHeads.has(m[1])) rotas.push(`linea ${i + 1}: #${m[1]}`);
    }
  }
  if (rotas.length) rotas.forEach((r) => fail(`ancla rota en TOC -> ${r}`));
  else ok("todas las anclas del TOC resuelven a un heading");
}

// ---------------------------------------------------------------------------
console.log(`\n${errors ? `FALLOS: ${errors}` : "TODO OK"}${warnings ? ` | avisos: ${warnings}` : ""}\n`);
process.exit(errors ? 1 : 0);
