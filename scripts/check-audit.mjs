#!/usr/bin/env node
/**
 * check-audit.mjs — Auditoría de vulnerabilidades NuGet (PLAN-MEJORAS-24 · P2).
 *
 * Ejecuta `dotnet list package --vulnerable --include-transitive` sobre la solución
 * de la API y la del cliente del repositorio actual y parsea la tabla de hallazgos:
 *   - Critical / High  → ERROR (exit 1)
 *   - Moderate / Low   → AVISO (exit 0)
 *
 * Uso:  node scripts/check-audit.mjs        (desde la raíz de cualquier repo)
 * Exit: 0 = sin vulnerabilidades Critical/High · 1 = hay Critical/High o fallo del comando.
 */
import { spawnSync } from "node:child_process";
import path from "node:path";
import fs from "node:fs";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repo = path.resolve(__dirname, "..");

const SOLUCIONES = [
  path.join(repo, "TiendaApi.slnx"),
  path.join(repo, "TiendaApi.Clients", "ClientBlazor", "ClientBlazor.slnx"),
];

let errores = 0;
const ok = (msg) => console.log(`  [OK]    ${msg}`);
const fail = (msg) => {
  errores++;
  console.log(`  [ERROR] ${msg}`);
};
const aviso = (msg) => console.log(`  [AVISO] ${msg}`);
const bloque = (t) => console.log(`\n=== ${t} ===`);

// Tabla de salida: "> Paquete  Resuelto  Gravedad  URL"
const RE_HALLAZGO = /^\s*>\s+(\S+)\s+(\S+)\s+(Critical|High|Moderate|Low)\s+(\S+)/gim;
const GRAVEDAD_GRITAR = new Set(["critical", "high"]);

function auditar(sln) {
  const nombre = path.relative(repo, sln).replaceAll("\\", "/");
  if (!fs.existsSync(sln)) {
    fail(`${nombre}: solución no encontrada`);
    return;
  }

  const r = spawnSync(
    "dotnet",
    ["list", sln, "package", "--vulnerable", "--include-transitive"],
    { cwd: repo, encoding: "utf8", maxBuffer: 32 * 1024 * 1024 },
  );

  if (r.error) {
    fail(`${nombre}: no se pudo ejecutar dotnet (${r.error.message})`);
    return;
  }
  if (r.status !== 0 && r.status !== 1) {
    fail(`${nombre}: dotnet list terminó con código ${r.status}`);
    return;
  }

  const salida = `${r.stdout ?? ""}${r.stderr ?? ""}`;
  const hallazgos = [];
  let m;
  RE_HALLAZGO.lastIndex = 0;
  while ((m = RE_HALLAZGO.exec(salida)) !== null) {
    hallazgos.push({ paquete: m[1], version: m[2], gravedad: m[3], advisory: m[4] });
  }

  if (hallazgos.length === 0) {
    ok(`${nombre}: sin vulnerabilidades`);
    return;
  }

  for (const h of hallazgos) {
    const linea = `${h.paquete} ${h.version} [${h.gravedad}] ${h.advisory}`;
    if (GRAVEDAD_GRITAR.has(h.gravedad.toLowerCase())) fail(`${nombre}: ${linea}`);
    else aviso(`${nombre}: ${linea}`);
  }
}

console.log("check-audit.mjs — vulnerabilidades NuGet (API + cliente)");
console.log(`  repo: ${repo}`);

for (const sln of SOLUCIONES) {
  bloque(path.relative(repo, sln).replaceAll("\\", "/"));
  auditar(sln);
}

console.log("");
if (errores > 0) {
  console.log(`FALLO: ${errores} hallazgo(s) Critical/High`);
  process.exit(1);
}
console.log("TODO OK — sin vulnerabilidades Critical/High");
process.exit(0);
