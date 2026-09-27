# Verifica el estilo del codigo con dotnet format SIN modificar nada (P1 · PLAN-MEJORAS-24).
# Uso:  powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-style.ps1
# Fix:  dotnet format TiendaApi.slnx
$ErrorActionPreference = "Continue"
$solution = Join-Path $PSScriptRoot "..\TiendaApi.slnx"
$ok = $true

Write-Host "== dotnet format whitespace =="
dotnet format $solution whitespace --verify-no-changes
if ($LASTEXITCODE -ne 0) { $ok = $false }

Write-Host "== dotnet format style =="
dotnet format $solution style --verify-no-changes
if ($LASTEXITCODE -ne 0) { $ok = $false }

if (-not $ok) {
    Write-Host "FAIL: hay cambios de formato pendientes. Ejecuta: dotnet format $solution" -ForegroundColor Red
    exit 1
}
Write-Host "OK: estilo correcto" -ForegroundColor Green
exit 0
