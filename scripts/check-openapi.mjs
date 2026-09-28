#!/usr/bin/env node
/**
 * check-openapi.mjs — Paridad del contrato OpenAPI entre ambos repos.
 *
 * Arranca cada API en su puerto (R1: 5041, R2: 5042), descarga
 * /swagger/v1/swagger.json y compara los documentos con un deep-diff
 * (ignorando el orden en arrays no semánticos: tags, required, parameters,
 * servers, security, y el puerto de localhost en URLs).
 *
 * Uso:  node scripts/check-openapi.mjs
 * Exit: 0 = contratos idénticos · 1 = diferencias o error
 */

import { spawn, spawnSync } from 'node:child_process';
import { setTimeout as sleep } from 'node:timers/promises';

const REPOS = [
  {
    nombre: 'R1 (TiendaDawApi-NetCore)',
    root: 'C:\\Users\\joseluisgs\\Projects\\Tienda\\TiendaDawApi-NetCore',
    puerto: 5041,
  },
  {
    nombre: 'R2 (TiendaDawApi-Cqrs-MediatR-NetCore)',
    root: 'C:\\Users\\joseluisgs\\Projects\\Tienda\\TiendaDawApi-Cqrs-MediatR-NetCore',
    puerto: 5042,
  },
];

const ARRANQUE_MAX_MS = 180000;
const MAX_DIFERENCIAS = 60;

/** Arrays cuyo orden no forma parte del contrato. */
const CLAVES_SIN_ORDEN = new Set(['tags', 'required', 'parameters', 'servers', 'security']);

/** Campos de texto libre: se comparan estructuralmente pero su contenido no. */
const CLAVES_TEXTO = new Set(['summary', 'description', 'title', 'example', 'examples']);

function liberarPuerto(puerto) {
  spawnSync(
    'powershell',
    [
      '-NoProfile',
      '-Command',
      `(Get-NetTCPConnection -LocalPort ${puerto} -State Listen -ErrorAction SilentlyContinue) | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }`,
    ],
    { windowsHide: true, stdio: 'ignore' },
  );
}

/**
 * Limpia el entorno: procesos TiendaApi previos (bloquearían la copia del
 * apphost durante el rebuild) y los puertos de E2E/de esta comprobación.
 */
function limpiarEntorno() {
  spawnSync(
    'powershell',
    [
      '-NoProfile',
      '-Command',
      'Get-Process -Name TiendaApi.Api -ErrorAction SilentlyContinue | Stop-Process -Force; ' +
        'Start-Sleep 2; ' +
        [5031, 5041, 5042]
          .map(
            (p) =>
              `(Get-NetTCPConnection -LocalPort ${p} -State Listen -ErrorAction SilentlyContinue) | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }`,
          )
          .join('; '),
    ],
    { windowsHide: true, stdio: 'ignore' },
  );
  return new Promise((r) => setTimeout(r, 1000));
}

function matarArbol(pid) {
  if (pid) {
    spawnSync('taskkill', ['/PID', String(pid), '/T', '/F'], { windowsHide: true, stdio: 'ignore' });
  }
}

function normalizar(doc) {
  const texto = JSON.stringify(doc)
    .replace(/localhost:\d+/g, 'localhost:PORT')
    .replace(/127\.0\.0\.1:\d+/g, '127.0.0.1:PORT');
  return JSON.parse(texto);
}

function comparable(valor, clave) {
  if (Array.isArray(valor) && CLAVES_SIN_ORDEN.has(clave)) {
    return valor
      .map((v) => JSON.stringify(v))
      .sort()
      .map((s) => JSON.parse(s));
  }
  return valor;
}

function tipo(v) {
  if (v === null) return 'null';
  if (Array.isArray(v)) return 'array';
  return typeof v;
}

function deepDiff(a, b, path, out) {
  if (out.length >= MAX_DIFERENCIAS) return;
  if (Object.is(a, b)) return;

  const ta = tipo(a);
  const tb = tipo(b);
  if (ta !== tb) {
    out.push(`${path}: tipo ${ta} vs ${tb}`);
    return;
  }

  if (ta === 'array') {
    if (a.length !== b.length) {
      out.push(`${path}: longitud ${a.length} vs ${b.length}`);
    }
    const n = Math.min(a.length, b.length);
    for (let i = 0; i < n; i++) {
      deepDiff(comparable(a[i], ''), comparable(b[i], ''), `${path}[${i}]`, out);
    }
    return;
  }

  if (ta === 'object') {
    const claves = [...new Set([...Object.keys(a), ...Object.keys(b)])].sort();
    for (const k of claves) {
      const pathHijo = path ? `${path}.${k}` : k;
      if (CLAVES_TEXTO.has(k) && (typeof a[k] === 'string' || typeof b[k] === 'string' || a[k] == null || b[k] == null)) {
        continue;
      }
      if (!(k in a)) {
        out.push(`${pathHijo}: solo en R2`);
        continue;
      }
      if (!(k in b)) {
        out.push(`${pathHijo}: solo en R1`);
        continue;
      }
      deepDiff(comparable(a[k], k), comparable(b[k], k), pathHijo, out);
    }
    return;
  }

  out.push(`${path}: ${JSON.stringify(a)} vs ${JSON.stringify(b)}`);
}

function resumen(doc) {
  const rutas = Object.keys(doc.paths ?? {}).length;
  let operaciones = 0;
  for (const item of Object.values(doc.paths ?? {})) {
    operaciones += Object.keys(item).filter((m) =>
      ['get', 'put', 'post', 'delete', 'patch', 'head', 'options', 'trace'].includes(m),
    ).length;
  }
  return { rutas, operaciones };
}

async function obtenerSwagger({ root, puerto }) {
  liberarPuerto(puerto);
  const url = `http://localhost:${puerto}/swagger/v1/swagger.json`;

  const proc = spawn(
    'dotnet',
    ['run', '--project', 'TiendaApi.Api/TiendaApi.csproj', '--urls', `http://localhost:${puerto}`],
    {
      cwd: root,
      env: {
        ...process.env,
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: `http://localhost:${puerto}`,
      },
      stdio: ['ignore', 'pipe', 'pipe'],
      windowsHide: true,
    },
  );

  let log = '';
  proc.stdout.on('data', (d) => (log += d));
  proc.stderr.on('data', (d) => (log += d));
  proc.on('error', (e) => {
    log += `\n[spawn error] ${e.message}`;
  });

  try {
    const inicio = Date.now();
    let doc = null;
    while (Date.now() - inicio < ARRANQUE_MAX_MS) {
      if (proc.exitCode !== null) {
        throw new Error(`la API terminó antes de responder (exit ${proc.exitCode})`);
      }
      try {
        const r = await fetch(url, { signal: AbortSignal.timeout(5000) });
        if (r.ok) {
          doc = await r.json();
          break;
        }
      } catch {
        /* aún arrancando */
      }
      await sleep(2000);
    }
    if (!doc) {
      throw new Error(`timeout esperando ${url}`);
    }
    return normalizar(doc);
  } catch (e) {
    console.error(`--- últimas líneas del log de la API (${puerto}) ---`);
    console.error(log.split('\n').slice(-25).join('\n'));
    throw e;
  } finally {
    matarArbol(proc.pid);
    await sleep(500);
    liberarPuerto(puerto);
  }
}

async function main() {
  console.log('=== CHECK OPENAPI: paridad de contrato entre repos ===\n');
  await limpiarEntorno();

  const docs = [];
  for (const repo of REPOS) {
    console.log(`→ Descargando swagger.json de ${repo.nombre} (puerto ${repo.puerto})...`);
    const t0 = Date.now();
    const doc = await obtenerSwagger(repo);
    const { rutas, operaciones } = resumen(doc);
    console.log(`  OK ${rutas} rutas · ${operaciones} operaciones · ${(Date.now() - t0) / 1000}s`);
    docs.push(doc);
  }

  const diferencias = [];
  deepDiff(docs[0], docs[1], '', diferencias);

  console.log('\n=== RESULTADO ===');
  if (diferencias.length === 0) {
    console.log('Diferencias: 0');
    console.log('OK: contratos OpenAPI idénticos entre ambos repos');
    process.exit(0);
  }

  console.log(`Diferencias: ${diferencias.length}${diferencias.length >= MAX_DIFERENCIAS ? ` (máx. ${MAX_DIFERENCIAS} mostradas)` : ''}`);
  for (const d of diferencias) console.log(`  X ${d}`);
  process.exit(1);
}

main().catch((e) => {
  console.error(`ERROR: ${e.message}`);
  process.exit(1);
});
