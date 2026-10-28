# Reset Database Script
# This script stops Docker containers, removes volumes, and restarts them to reset the database

Write-Host "=== Resetting Tienda API Database ===" -ForegroundColor Cyan

# Stop and remove containers with volumes
# down -v borra TODOS los volumenes, incluido cassandra-data: el keyspace queda
# a cien y InitializeSchemaAsync lo vuelve a crear en el proximo arranque.
Write-Host "Stopping and removing containers..." -ForegroundColor Yellow
docker-compose -f docker-compose.local.yml down -v

# Start containers again
Write-Host "Starting containers..." -ForegroundColor Yellow
docker-compose -f docker-compose.local.yml up -d

# Wait for services to be healthy
Write-Host "Waiting for services to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

Write-Host ""
Write-Host "=== Database reset complete! ===" -ForegroundColor Green
Write-Host "PostgreSQL: localhost:5432" -ForegroundColor White
Write-Host "MongoDB: localhost:27017" -ForegroundColor White
Write-Host "Adminer: localhost:8080" -ForegroundColor White
Write-Host "Mongo Express: localhost:8081" -ForegroundColor White
Write-Host "Cassandra: localhost:9042 (keyspace tienda; la JVM tarda ~30-60s)" -ForegroundColor White
