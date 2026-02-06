# 28. CI/CD con GitHub Actions

## Indice

[28. CI/CD con GitHub Actions](#28-cicd-con-github-actions)
  - [28.1. Fundamentos de CI/CD](#281-fundamentos-de-cicd)
  - [28.2. Arquitectura del Pipeline](#282-arquitectura-del-pipeline)
  - [28.3. Anatomia del Workflow](#283-anatomia-del-workflow)
  - [28.4. Jobs y sus Dependencias](#284-jobs-y-sus-dependencias)
  - [28.5. Estrategias de Testing Automatizado](#285-estrategias-de-testing-automatizado)
  - [28.6. Gestion de Artefactos](#286-gestion-de-artefactos)
  - [28.7. Documentacion Automatizada](#287-documentacion-automatizada)
  - [28.8. GitHub CLI: Tu Aliada en la Consola](#288-github-cli-tu-aliada-en-la-consola)
  - [28.9. Ejecucion y Monitoreo de Workflows](#289-ejecucion-y-monitoreo-de-workflows)
  - [28.10. Mejores Practicas](#2810-mejores-practicas)
  - [28.11. Resumen](#2811-resumen)

---

## 28.1. Fundamentos de CI/CD

Continuous Integration (CI) y Continuous Delivery/Deployment (CD) son practicas fundamentales en el desarrollo de software moderno que transforman radicalmente la forma en que los equipos entregan valor a sus usuarios.

La Integracion Continua es una practica de desarrollo que requiere que los desarrolladores integren su codigo en un repositorio compartido frecuentemente, idealmente varias veces al dia.

Continuous Delivery extiende CI al asegurar que el codigo integrado siempre este en un estado desplegable. Los equipos pueden liberar nuevas caracteristicas, correcciones de bugs y mejoras de rendimiento de forma rapida y confiable.

Continuous Deployment lleva este concepto aun mas lejos, eliminando la intervencion humana del proceso de despliegue.

### Beneficios para Proyectos Educativos

La implementacion de CI/CD en proyectos academicos proporciona beneficios que van mas alla de la simple automatizacion. Los estudiantes desarrollan una comprension profunda de los flujos de trabajo profesionales.

Los pipelines de CI/CD funcionan como un sistema de retroalimentacion inmediata para el aprendizaje.

## 28.2. Arquitectura del Pipeline

El pipeline de CI/CD de TiendaDawApi sigue una arquitectura modular que separa claramente las responsabilidades de cada etapa del proceso.

```mermaid
flowchart TD
    A[Push a main/develop] --> B{Es push a main?}
    B -->|Si| C[Ejecutar todos los jobs]
    B -->|No| D[Ejecutar solo Build y Test]
    
    C --> E[Build]
    C --> F[Test Unit]
    C --> G[Test Integration]
    C --> H[Generate Docs]
    
    E --> I[Upload build-output]
    F --> J[Upload coverage-report]
    G --> K{Tests pasan?}
    H --> L[Upload api-documentation]
    
    K -->|Si| M[Resumen]
    K -->|No| N[Resumen con errores]
    L --> M
    J --> M
    
    M --> O[Pipeline Completado]
```

### Estructura de Ejecucion Paralela

La capacidad de ejecutar jobs en paralelo es uno de los aspectos mas valiosos de los pipelines modernos. En TiendaDawApi, los jobs de Build, Test Unit y Generate Documentation se ejecutan simultaneamente.

```mermaid
flowchart LR
    subgraph Paralelo["Ejecucion Paralela"]
        B[Build<br/>25-28s] -.-> S
        T[Test Unit<br/>22-28s] -.-> S
        D[Docs<br/>35-38s] -.-> S
    end
    
    subgraph Sec["Resumen (Secuencial)"]
        S[Summary<br/>38-40s]
    end
```

- Build: Checkout (0-10s) -> Restore+Build (10-25s) -> Upload (25-28s)
- Test Unit: Checkout (0-10s) -> Download (10-12s) -> Tests (12-22s) -> Coverage (22-28s)
- Docs: Checkout (0-8s) -> Install DocFX (8-15s) -> Build Docs (15-35s) -> Upload (35-38s)
- Summary: Espera a que terminen los demas (28-38s) -> Genera resumen (38-40s)

## 28.3. Anatomia del Workflow

Un workflow de GitHub Actions se define mediante un archivo YAML que describe los jobs, steps y condiciones de ejecucion.

### Estructura del Archivo de Workflow

```yaml
name: CI Pipeline

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]
  workflow_dispatch:

env:
  DOTNET_VERSION: '10.0.x'
  SOLUTION: 'TiendaApi.slnx'

jobs:
  build:
    name: Build
    runs-on: ubuntu-latest
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
```

### Componentes de un Job

```mermaid
flowchart LR
    Step1[Checkout] --> Step2[Setup .NET] --> Step3[Restore] --> Step4[Build] --> Step5[Upload]
```

## 28.4. Jobs y sus Dependencias

La gestion de dependencias entre jobs es crucial para pipelines eficientes. GitHub Actions permite especificar estas dependencias mediante la palabra clave needs.

```mermaid
flowchart TD
    B[build] --> T[test]
    B --> D[validate-docs]
    B --> TI[test-integration]
    T --> S[summary]
    TI --> S
    D --> S
```

### Job Build: Fundamento del Pipeline

```yaml
jobs:
  build:
    name: Build
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      
      - name: Restore dependencies
        run: dotnet restore ${{ env.SOLUTION }}
      
      - name: Build
        run: dotnet build ${{ env.SOLUTION }} --configuration Release --no-restore
      
      - name: Upload build artifacts
        uses: actions/upload-artifact@v4
        with:
          name: build-output
          path: TiendaApi.Api/bin/Release/net10.0/
          retention-days: 1
```

### Job Test: Validacion Automatizada

```yaml
test:
  name: Test (Unit - Parallel)
  needs: build
  runs-on: ubuntu-latest
  if: needs.build.result == 'success'
  
  steps:
    - name: Checkout code
      uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Download build artifacts
      uses: actions/download-artifact@v4
      with:
        name: build-output
        path: TiendaApi.Api/bin/Release/net10.0/
    
    - name: Run unit tests (parallel)
      run: dotnet test ${{ env.SOLUTION }} --configuration Release --no-build --filter "FullyQualifiedName~Unit" --verbosity minimal --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

## 28.5. Estrategias de Testing Automatizado

```mermaid
flowchart BT
    Unit[Unit Tests<br/>100+ tests<br/>segundos] --> Integration[Integration Tests<br/>20+ tests<br/>minutos]
    Integration --> E2E[E2E Tests<br/>10+ tests<br/>minutos]
```

### Tests Unitarios: Base de la Piramide

```yaml
- name: Run unit tests (parallel)
  run: dotnet test ${{ env.SOLUTION }} --configuration Release --no-build --filter "FullyQualifiedName~Unit" --verbosity minimal --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

### Tests de Integracion

```yaml
test-integration:
  name: Test (Integration)
  needs: build
  runs-on: ubuntu-latest
  if: github.event_name == 'workflow_dispatch' && github.ref == 'refs/heads/main'
  
  services:
    mongo:
      image: mongo:7.0
      ports:
        - 27017:27017
    redis:
      image: redis:7-alpine
      ports:
        - 6379:6379
  
  steps:
    - name: Download build artifacts
      uses: actions/download-artifact@v4
      with:
        name: build-output
        path: TiendaApi.Api/bin/Release/net10.0/
    
    - name: Run integration tests (sequential, non-parallel)
      run: dotnet test ${{ env.SOLUTION }} --configuration Release --no-build --filter "FullyQualifiedName~Integration" --verbosity minimal
```

## 28.6. Gestion de Artefactos

### Subida de Artefactos

```yaml
- name: Upload build artifacts
  uses: actions/upload-artifact@v4
  with:
    name: build-output
    path: TiendaApi.Api/bin/Release/net10.0/
    retention-days: 1
```

### Descarga de Artefactos

```yaml
- name: Download build artifacts
  uses: actions/download-artifact@v4
  with:
    name: build-output
    path: TiendaApi.Api/bin/Release/net10.0/
```

```mermaid
flowchart TD
    A[Job Build] -->|Upload| B[Artifact Store]
    B -->|Download| C[Job Test]
    B -->|Download| D[Job Integration]
    C --> E[Test Results]
    D --> F[Integration Results]
```

## 28.7. Documentacion Automatizada

```yaml
validate-docs:
  name: Generate & Validate Documentation
  runs-on: ubuntu-latest
  
  steps:
    - name: Checkout code
      uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Install DocFX
      run: dotnet tool install --global docfx
    
    - name: Build Documentation (HTML)
      run: docfx docfx.json
    
    - name: Upload Documentation Artifact
      uses: actions/upload-artifact@v4
      with:
        name: api-documentation-html
        path: _site
        if-no-files-found: warn
        retention-days: 7
```

## 28.8. GitHub CLI: Tu Aliada en la Consola

La GitHub CLI (gh) es una herramienta de linea de comandos que permite interactuar directamente con GitHub.

### Instalacion de GitHub CLI

```bash
# Windows (con scoop)
scoop install gh

# macOS (con Homebrew)
brew install gh

# Linux (Debian/Ubuntu)
sudo apt install gh
```

### Comandos Esenciales para CI/CD

#### Verificar Estado del Repositorio

```bash
# Ver estado del repositorio local
gh repo view

# Listar workflows disponibles
gh workflow list

# Ver detalles de un workflow especifico
gh workflow view ci.yml
```

#### Gestion de Workflows

```bash
# Ejecutar un workflow manualmente
gh workflow run ci.yml

# Ver historial de ejecuciones
gh run list --limit 10

# Ver una ejecucion especifica
gh run view <run-id>

# Ver logs en tiempo real (follow)
gh run watch <run-id> --exit-status

# Cancelar una ejecucion en progreso
gh run cancel <run-id>

# Re-ejecutar un workflow fallido
gh run rerun <run-id>
```

#### Gestion de Artefactos

```bash
# Listar artefactos de una ejecucion
gh run view <run-id> --json artifacts

# Descargar un artefacto especifico
gh run download <run-id> -n <artifact-name>

# Descargar todos los artefactos
gh run download <run-id> -D ./artefactos
```

#### Gestion de Releases y Tags

```bash
# Listar tags (versiones)
gh tag list

# Crear un tag anotado
git tag -a v1.0.0 -m "Release v1.0.0"

# Subir un tag a GitHub
git push origin v1.0.0

# Crear un release desde un tag
gh release create v1.0.0 --title "Release v1.0.0" --notes "Cambios de la version"
```

### Obtencion de IDs para Comandos

```bash
# Obtener el ID de la ultima ejecucion
gh run list --limit 1 --json id,databaseId,status,name

# Output en formato JSON para parsing
gh run list -L1 --jq '.[] | .id'
```

## 28.9. Ejecucion y Monitoreo de Workflows

### Ejemplo de Flujo de Trabajo Completo

```bash
# 1. Verificar el estado actual del repositorio
gh repo view

# 2. Listar workflows disponibles
gh workflow list

# 3. Ejecutar el pipeline de CI manualmente
gh workflow run ci.yml

# 4. Obtener el ID de la ejecucion
RUN_ID=$(gh run list -L1 --jq '.[0].id')
echo "Ejecutando: $RUN_ID"

# 5. Monitorear el progreso en tiempo real
gh run watch $RUN_ID --exit-status

# 6. Ver resultado final
gh run view $RUN_ID

# 7. Ver jobs individuales
gh run view $RUN_ID --json jobs

# 8. Descargar artefactos generados
gh run download $RUN_ID -D ./artefactos
```

### Interpretacion de Resultados

```bash
# Ver resumen del pipeline
gh run view <run-id> --json name,status,conclusion,jobs
```

### Diagnostico de Problemas

```bash
# Ver pasos fallidos
gh run view <run-id> --json jobs --jq '.jobs[] | select(.conclusion == "FAILURE")'

# Obtener logs de un job especifico
gh run view <run-id> --job=<job-id> --log

# Buscar errores especificos en los logs
gh run view <run-id> --log | grep -i error

# Ver anotaciones (warnings y errores de linting)
gh run view <run-id>
```

## 28.10. Mejores Practicas

Implementar CI/CD efectivo requiere seguir convenciones y patrones que maximizan la confiabilidad y mantenibilidad del pipeline.

### Principios Fundamentales

```mermaid
flowchart TB
    subgraph Practicas["Practicas de CI/CD"]
        direction TB
        V[Velocidad<br/>Jobs en paralelo<br/>Cache dependencias<br/>Artifacts]
        C[Confiabilidad<br/>Tests independientes<br/>Verificaciones<br/>Logs]
        M[Mantenibilidad<br/>Variables entorno<br/>Jobs modulares<br/>Documentacion]
        S[Seguridad<br/>Secretos cifrados<br/>Permisos minimos<br/>Revisiones]
    end
```

- Velocidad: Ejecutar jobs en paralelo, cachear dependencias, usar artifacts eficientemente
- Confiabilidad: Tests independientes, verificaciones condicionales, logs detallados
- Mantenibilidad: Variables de entorno, jobs modulares, documentacion
- Seguridad: Secretos cifrados, permisos minimos, revisiones de codigo

### Optimizacion de Tiempos de Ejecucion

```yaml
# Usar cache para dependencias
- name: Cache NuGet packages
  uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
    restore-keys: |
      ${{ runner.os }}-nuget-
```

### Manejo de Secretos

```yaml
# NUNCA hardcodear secrets en el workflow
env:
  # CORRECTO - Usar GitHub Secrets
  DATABASE_URL: ${{ secrets.DATABASE_URL }}
  API_KEY: ${{ secrets.API_KEY }}
```

Los secrets se configuran en Settings > Secrets and variables > Actions del repositorio.

## 28.11. Resumen

La implementacion de CI/CD con GitHub Actions representa un salto cualitativo en el desarrollo de software, transformando procesos manuales propensos a errores en flujos automatizados, confiables y reproducibles.

La GitHub CLI emerge como una herramienta indispensable para desarrolladores modernos, permitiendo interactuar con workflows directamente desde la terminal.

El dominio de estas tecnicas prepara al alumnado para entornos profesionales donde la automatizacion no es opcional sino requisito indispensable.

---

## Recursos Adicionales

- [Documentacion oficial de GitHub Actions](https://docs.github.com/es/actions)
- [GitHub CLI Documentation](https://cli.github.com/manual/)
- [Marketplace de Actions](https://github.com/marketplace?type=actions)
- [Ejemplos de Workflows](https://github.com/actions/starter-workflows)
