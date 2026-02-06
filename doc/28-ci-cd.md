# 28. CI/CD con GitHub Actions

## Índice

[28. CI/CD con GitHub Actions](#28-cicd-con-github-actions)
  - [28.1. Fundamentos de CI/CD](#281-fundamentos-de-cicd)
  - [28.2. Arquitectura del Pipeline](#282-arquitectura-del-pipeline)
  - [28.3. Anatomía del Workflow](#283-anatomía-del-workflow)
  - [28.4. Jobs y sus Dependencias](#284-jobs-y-sus-dependencias)
  - [28.5. Estrategias de Testing Automatizado](#285-estrategias-de-testing-automatizado)
  - [28.6. Gestión de Artefactos](#286-gestión-de-artefactos)
  - [28.7. Documentación Automatizada](#287-documentación-automatizada)
  - [28.8. GitHub CLI: Tu Aliada en la Consola](#288-github-cli-tu-aliada-en-la-consola)
  - [28.9. Ejecución y Monitoreo de Workflows](#289-ejecución-y-monitoreo-de-workflows)
  - [28.10. Mejores Prácticas](#2810-mejores-prácticas)
  - [28.11. Resumen](#2811-resumen)

---

## 28.1. Fundamentos de CI/CD

Continuous Integration (CI) y Continuous Delivery/Deployment (CD) son prácticas fundamentales en el desarrollo de software moderno que transforman radicalmente la forma en que los equipos entregan valor a sus usuarios. Estas metodologías automatizan los procesos de construcción, prueba y despliegue de aplicaciones, reduciendo significativamente el riesgo de errores en producción y acelerando el ciclo de desarrollo. En el contexto de proyectos educativos como TiendaDawApi, implementar CI/CD no solo demuestra competencia técnica avanzada, sino que también prepara al alumnado para entornos profesionales donde estas prácticas son obligatorias.

La Integración Continua es una práctica de desarrollo que requiere que los desarrolladores integren su código en un repositorio compartido frecuentemente, idealmente varias veces al día. Cada integración se verifica automáticamente mediante una construcción y pruebas automatizadas, detectando problemas de integración lo antes posible. Esta aproximación contrasta radicalmente con los modelos tradicionales donde los desarrolladores trabajaban aisladamente durante semanas o meses antes de intentar integrar sus cambios, lo cual generaba "infierno de integraciones" con conflictos masivos y bugs difíciles de rastrear.

Continuous Delivery extiende CI al asegurar que el código integrado siempre esté en un estado desplegable. Esto significa que además de las pruebas automatizadas, el proceso de despliegue está completamente automatizado y puede ejecutarse en cualquier momento con solo pulsar un botón. Los equipos pueden liberar nuevas características, correcciones de bugs y mejoras de rendimiento de forma rápida y confiable, reduciendo el tiempo desde que se escribe una línea de código hasta que llega a manos del usuario final.

Continuous Deployment lleva este concepto aún más lejos, eliminando la intervención humana del proceso de despliegue. Cada cambio que pasa todas las pruebas y verificaciones se despliega automáticamente en producción. Aunque esta práctica requiere una suite de pruebas exhaustiva y una cultura de monitoreo robusta, representa el pináculo de la automatización en DevOps y demuestra la máxima confianza en el proceso de calidad del software.

### Beneficios Tangibles para Proyectos Educativos

La implementación de CI/CD en proyectos académicos proporciona beneficios que van más allá de la simple automatización. En primer lugar, los estudiantes desarrollan una comprensión profunda de los flujos de trabajo profesionales que encontrarán en cualquier empresa de desarrollo de software. Esta experiencia práctica es invaluable para la empleabilidad y diferencia significativamente a los graduados que han trabajado con metodologías modernas frente a aquellos que solo conocen procesos manuales.

En segundo lugar, los pipelines de CI/CD funcionan como un sistema de retroalimentación inmediata para el aprendizaje. Cuando un estudiante comete un error de sintaxis, una dependencia faltante o una prueba que falla, lo descubre en minutos en lugar de horas o días. Esta iteración rápida acelera dramáticamente el proceso de aprendizaje y refuerza buenas prácticas desde el primer momento.

Finalmente, la documentación generada automáticamente por los pipelines (artefactos, informes de cobertura, logs de ejecución) proporciona evidencia tangible del trabajo realizado, útil tanto para evaluación académica como para portafolios profesionales. Los entrevistadores técnicos valoran enormemente candidatos que pueden demostrar experiencia con herramientas de automatización modernas.

## 28.2. Arquitectura del Pipeline

El pipeline de CI/CD de TiendaDawApi sigue una arquitectura modular que separa claramente las responsabilidades de cada etapa del proceso. Esta separación permite ejecuciones paralelas cuando es posible, reduciendo significativamente el tiempo total de ejecución del pipeline completo.

```mermaid
flowchart TD
    A[Push a main/develop] --> B{¿Es push a main?}
    B -->|Sí| C[Ejecutar todos los jobs]
    B -->|No| D[Ejecutar solo Build y Test]
    
    C --> E[Build]
    C --> F[Test Unit]
    C --> G[Test Integration]
    C --> H[Generate Docs]
    
    E --> I[Upload build-output]
    F --> J[Upload coverage-report]
    G --> K{Tests pasan?}
    H --> L[Upload api-documentation]
    
    K -->|Sí| M[Resumen]
    K -->|No| N[Resumen con errores]
    L --> M
    J --> M
    
    M --> O[Pipeline Completado]
```

El flujo comienza con un push a las ramas protegidas (main o develop) o con la apertura de un pull request. El sistema evalúa las condiciones del trigger y decide qué jobs ejecutar. Esta lógica condicional es crucial para optimizar el uso de recursos de GitHub Actions, ejecutando pruebas más ligeras durante el desarrollo y pruebas completas solo cuando es necesario.

### Estructura de Ejecución Paralela

La capacidad de ejecutar jobs en paralelo es uno de los aspectos más valiosos de los pipelines modernos. En TiendaDawApi, los jobs de Build, Test Unit y Generate Documentation se ejecutan simultáneamente, aprovechando la infraestructura distribuida de GitHub Actions.

```mermaid
gantt
    title Ejecución Paralela del Pipeline
    dateFormat  HH:mm:ss
    section Build
    Checkout + Setup    :b1, 0s, 10s
    Restore + Build      :b2, 10s, 25s
    Upload Artifacts    :b3, 25s, 28s
    
    section Test Unit
    Checkout + Setup    :t1, 0s, 10s
    Download Artifacts  :t2, 10s, 12s
    Run Tests           :t3, 12s, 22s
    Coverage Report     :t4, 22s, 28s
    
    section Docs
    Checkout + Setup    :d1, 0s, 8s
    Install DocFX       :d2, 8s, 15s
    Build Docs          :d3, 15s, 35s
    Upload Docs         :d4, 35s, 38s
    
    section Summary
    Wait for all       :s1, 28s, 38s
    Generate Summary   :s2, 38s, 40s
```

Este diagrama de Gantt ilustra cómo los jobs se superponen temporalmente, con el job Summary esperando pacientemente a que todos los jobs anteriores completen. El tiempo total del pipeline está dominado por el job más lento (Docs en este ejemplo) en lugar de la suma de todos los tiempos.

## 28.3. Anatomía del Workflow

Un workflow de GitHub Actions se define mediante un archivo YAML que describe los jobs, steps y condiciones de ejecución. Comprender cada componente es esencial para crear pipelines efectivos y mantenibles.

### Estructura del Archivo de Workflow

```yaml
# GitHub Actions CI Pipeline for TiendaDawApi
name: CI Pipeline

# Triggers: eventos que inician el workflow
on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]
  workflow_dispatch:

# Variables de entorno globales
env:
  DOTNET_VERSION: '10.0.x'
  SOLUTION: 'TiendaApi.slnx'

# Definición de jobs
jobs:
  build:
    name: Build
    runs-on: ubuntu-latest
    # ...
```

El elemento `name` establece el nombre visible del workflow en la interfaz de GitHub. El elemento `on` define los eventos que disparan la ejecución, soportando múltiples tipos de triggers como pushes, pull requests, releases, y eventos manuales mediante `workflow_dispatch`. Las variables de entorno globales proporcionan valores compartidos entre todos los jobs, simplificando la configuración y reduciendo duplicación.

### Componentes de un Job

```mermaid
flowchart LR
    subgraph Job["Job: build"]
        direction LR
        S1["Step: Checkout"]
        S2["Step: Setup .NET"]
        S3["Step: Restore"]
        S4["Step: Build"]
        S5["Step: Upload"]
        
        S1 --> S2 --> S3 --> S4 --> S5
    end
```

Cada job es una unidad de trabajo que se ejecuta en un entorno aislado. El selector `runs-on` determina la máquina virtual donde se ejecuta, típicamente `ubuntu-latest` para proyectos multiplataforma. Los steps dentro de un job se ejecutan secuencialmente, y cada uno puede ser una acción predefinida (usando `uses`) o un comando personalizado (usando `run`).

### Configuración de Steps con Acciones

Las acciones (actions) son unidades reutilizables de código que encapsulan tareas comunes. GitHub Marketplace ofrece miles de acciones mantenidas por la comunidad y empresas.

```yaml
# Acción predefinida: checkout
- name: Checkout code
  uses: actions/checkout@v4

# Acción predefinida: setup-dotnet
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: ${{ env.DOTNET_VERSION }}

# Comando personalizado
- name: Restore dependencies
  run: dotnet restore ${{ env.SOLUTION }}
```

La acción `actions/checkout@v4` descarga el código del repositorio en el runner, mientras que `actions/setup-dotnet@v4` configura el SDK de .NET con la versión especificada. El parámetro `with` permite pasar configuraciones adicionales a las acciones, haciendo uso de la sintaxis de expresiones `${{ }}` para inyectar valores dinámicos.

## 28.4. Jobs y sus Dependencias

La gestión de dependencias entre jobs es crucial para pipelines eficientes. GitHub Actions permite especificar estas dependencias mediante la palabra clave `needs`, creando un grafo Directed Acyclic Graph (DAG) de ejecuciones.

```mermaid
flowchart TD
    subgraph Jobs["Grafo de Jobs"]
        B[build<br/>Build] --> T[test<br/>Test Unit]
        B --> D[validate-docs<br/>Docs]
        B --> TI[test-integration<br/>Integration]
        
        T --> S[summary<br/>Summary]
        TI --> S
        D --> S
    end
    
    style B fill:#90EE90
    style T fill:#87CEEB
    style TI fill:#FFB6C1
    style D fill:#DDA0DD
    style S fill:#FFD700
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

El job de Build es el punto de entrada del pipeline. Su función principal es compilar el código y generar los artefactos que posteriormente utilizarán los jobs de prueba. La estrategia `--no-restore` en el comando de build optimiza la ejecución al evitar restauraciones redundantes, asumiendo que las dependencias ya fueron restauradas en el paso anterior.

### Job Test: Validación Automatizada

```yaml
test:
  name: Test (Unit - Parallel)
  needs: build
  runs-on: ubuntu-latest
  
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

La dependencia `needs: build` garantiza que este job solo se ejecute si el Build completa exitosamente. El filtro `--filter "FullyQualifiedName~Unit"` asegura que solo se ejecuten tests unitarios, excluyendo tests de integración que requieren servicios externos como bases de datos.

### Job de Tests de Integración

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
    # ...
    - name: Run integration tests (sequential, non-parallel)
      run: dotnet test ${{ env.SOLUTION }} --configuration Release --no-build --filter "FullyQualifiedName~Integration" --verbosity minimal
```

Los tests de integración requieren servicios externos, por lo que GitHub Actions permite definir contenedores de servicios que se ejecutan junto con el job. La condición `if` restringe la ejecución a eventos manuales en la rama principal, evitando el costo computacional de estos tests durante el desarrollo diario.

## 28.5. Estrategias de Testing Automatizado

Una estrategia de testing robusta es el corazón de cualquier pipeline de CI efectivo. TiendaDawApi implementa una jerarquía de pruebas que balancea velocidad de ejecución con cobertura exhaustiva.

```mermaid
flowchart TD
    subgraph Pirámide["Pirámide de Tests"]
        direction BT
        A[Unit Tests<br/>100+ tests<br/>Ejecución: segundos] --> B[Integration Tests<br/>20+ tests<br/>Ejecución: minutos]
        B --> C[E2E Tests<br/>10+ tests<br/>Ejecución: minutos]
    end
    
    style A fill:#4CAF50
    style B fill:#2196F3
    style C fill:#FF9800
```

### Tests Unitarios: Base de la Pirámide

Los tests unitarios constituyen la base de la pirámide de pruebas debido a su velocidad y aislamiento. Estos tests verifican componentes individuales del código (métodos, clases, funciones) de forma aislada, sin dependencias externas como bases de datos o servicios web.

```yaml
- name: Run unit tests (parallel)
  run: dotnet test ${{ env.SOLUTION }} --configuration Release --no-build --filter "FullyQualifiedName~Unit" --verbosity minimal --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

El filtro `FullyQualifiedName~Unit` utiliza la sintaxis de NUnit para ejecutar solo tests que contengan la palabra "Unit" en su nombre completamente calificado. La opción `--collect:"XPlat Code Coverage"` activa la recopilación de métricas de cobertura compatibles con múltiples plataformas.

### Generación de Informes de Cobertura

```yaml
- name: Install ReportGenerator
  run: dotnet tool install --global dotnet-reportgenerator-globaltool

- name: Find coverage files
  id: find-coverage
  run: |
    if ls ./TestResults/**/coverage.cobertura.xml 2>/dev/null; then
      echo "found=true" >> $GITHUB_OUTPUT
    else
      echo "found=false" >> $GITHUB_OUTPUT
    fi

- name: Generate Coverage HTML Report
  if: steps.find-coverage.outputs.found == 'true'
  run: reportgenerator "-reports:./TestResults/**/coverage.cobertura.xml" "-targetdir:./TestResults/CoverageReport" "-reporttypes:HtmlInline_AzurePipelines"

- name: Upload coverage report
  if: steps.find-coverage.outputs.found == 'true'
  uses: actions/upload-artifact@v4
  with:
    name: coverage-report
    path: ./TestResults/CoverageReport
    retention-days: 7
```

El proceso de generación de informes de cobertura incluye verificación condicional para evitar errores cuando no se generan archivos de cobertura. El uso de `$GITHUB_OUTPUT` permite compartir datos entre steps de forma limpia y moderna.

### Tests de Integración: Con Servicios Reales

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

La configuración de servicios permite crear contenedores Docker que proporcionan MongoDB y Redis durante la ejecución de los tests. GitHub Actions会自动管理 la red entre el contenedor del job y los servicios, exponiendo los puertos especificados.

## 28.6. Gestión de Artefactos

Los artefactos son archivos generados durante la ejecución del pipeline que se persisten más allá del job que los creó. Son esenciales para compartir archivos entre jobs y para mantener registros de despliegues.

### Subida de Artefactos

```yaml
- name: Upload build artifacts
  uses: actions/upload-artifact@v4
  with:
    name: build-output
    path: TiendaApi.Api/bin/Release/net10.0/
    retention-days: 1
```

El nombre del artefacto (`build-output`) debe ser único dentro del workflow. El path puede ser un directorio o archivo específico. El parámetro `retention-days` controla cuánto tiempo GitHub conserva el artefacto, balanceando el costo de almacenamiento con la necesidad de mantener registros históricos.

### Descarga de Artefactos

```yaml
- name: Download build artifacts
  uses: actions/download-artifact@v4
  with:
    name: build-output
    path: TiendaApi.Api/bin/Release/net10.0/
```

Por defecto, `actions/download-artifact@v4` descarga los artefactos en el directorio actual. El parámetro `path` permite especificar una ubicación diferente. Si existen múltiples artefactos con el mismo nombre, el último descargará sobrescribirlo.

```mermaid
flowchart TD
    A[Job: Build] -->|Upload: build-output| B[Artifact Store]
    B -->|Download| C[Job: Test]
    B -->|Download| D[Job: Integration]
    
    C --> E[Coverage Report]
    D --> F[Test Results]
    
    style B fill:#FFE4B5
    style E fill:#98FB98
    style F fill:#98FB98
```

## 28.7. Documentación Automatizada

La generación automática de documentación garantiza que la documentación técnica permanezca sincronizada con el código fuente.

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
    
    - name: Create fallback docfx.json
      run: |
        if [ ! -f docfx.json ]; then
          # Crear configuración mínima
          cat > docfx.json << 'EOF'
          {
            "metadata": [
              {
                "src": [
                  {
                    "files": ["TiendaApi.Api/TiendaApi.Api.csproj"],
                    "properties": {
                      "TargetFramework": "net10.0"
                    }
                  }
                ],
                "dest": "api"
              }
            ],
            "build": {
              "content": [
                {"files": ["api/*.yml", "doc/*.md", "*.md"]},
                {"files": ["api/**.yml"], "src": "api"}
              ],
              "dest": "_site",
              "template": ["default"]
            }
          }
          EOF
        fi
    
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

DocFX analiza el código fuente y los comentarios XML para generar documentación API en formato HTML. El parámetro `if-no-files-found: warn` previene que el paso falle silenciosamente si no hay archivos que procesar.

## 28.8. GitHub CLI: Tu Aliada en la Consola

La GitHub CLI (`gh`) es una herramienta de línea de comandos que permite interactuar directamente con GitHub desde tu terminal, eliminando la necesidad de abrir el navegador para muchas tareas comunes.

### Instalación de GitHub CLI

```bash
# Windows (con scoop)
scoop install gh

# Windows (con winget)
winget install GitHub.cli

# macOS (con Homebrew)
brew install gh

# Linux ( Debian/Ubuntu)
sudo apt install gh
```

Tras la instalación, necesitas autenticarte con tu cuenta de GitHub:

```bash
gh auth login
```

### Comandos Esenciales para CI/CD

#### Verificar Estado del Repositorio

```bash
# Ver estado del repositorio local
gh repo view --json name,defaultBranchRef,nameWithOwner

# Listar workflows disponibles
gh workflow list

# Ver detalles de un workflow específico
gh workflow view ci.yml
```

#### Gestión de Workflows

```bash
# Ejecutar un workflow manualmente
gh workflow run ci.yml

# Ejecutar con parámetros (si el workflow los acepta)
gh workflow run ci.yml -f parametro=valor

# Habilitar/deshabilitar un workflow
gh workflow enable ci.yml
gh workflow disable ci.yml

# Ver historial de ejecuciones
gh run list --limit 10

# Ver una ejecución específica
gh run view <run-id>

# Ver logs de una ejecución
gh run view <run-id> --log

# Ver logs de un job específico
gh run view <run-id> --job=<job-id>

# Ver logs en tiempo real (follow)
gh run watch <run-id>

# Cancelar una ejecución en progreso
gh run cancel <run-id>

# Re-ejecutar un workflow fallido
gh run rerun <run-id>
```

#### Gestión de Artefactos

```bash
# Listar artefactos de una ejecución
gh run view <run-id> --json artifacts

# Descargar un artefacto específico
gh run download <run-id> -n <artifact-name>

# Descargar todos los artefactos
gh run download <run-id>

# Ver detalles de un artefacto
gh api repos/:owner/:repo/actions/artifacts/<artifact-id>
```

#### Gestión de Releases y Tags

```bash
# Listar tags (versiones)
gh tag list

# Crear un tag anotado
git tag -a v1.0.0 -m "Release v1.0.0"

# Subir un tag a GitHub
git push origin v1.0.0

# Crear un release desde un tag
gh release create v1.0.0 --title "Release v1.0.0" --notes "Cambios de la versión"

# Ver historial de releases
gh release list
```

### Obtención de IDs para Comandos

Para muchos comandos de `gh`, necesitas el ID de la ejecución del workflow. Puedes obtenerlo fácilmente:

```bash
# Obtener el ID de la última ejecución
gh run list --limit 1 --json id,databaseId,status,name

# Output en formato JSON para parsing
gh run list -L1 --jq '.[] | .id'
```

## 28.9. Ejecución y Monitoreo de Workflows

El monitoreo efectivo de workflows requiere comprender tanto la interfaz web como las herramientas de CLI.

### Ejemplo de Flujo de Trabajo Completo

```bash
# 1. Verificar el estado actual del repositorio
gh repo view

# 2. Listar workflows disponibles
gh workflow list

# 3. Ejecutar el pipeline de CI manualmente
gh workflow run ci.yml

# 4. Obtener el ID de la ejecución (en otra terminal o script)
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

### Interpretación de Resultados

```bash
# Ver resumen del pipeline
gh run view <run-id> --json name,status,conclusion,jobs

# Salida típica en JSON
{
  "name": "CI Pipeline",
  "status": "COMPLETED",
  "conclusion": "SUCCESS",
  "jobs": [
    {"name": "Build", "status": "COMPLETED", "conclusion": "SUCCESS"},
    {"name": "Test (Unit - Parallel)", "status": "COMPLETED", "conclusion": "SUCCESS"},
    {"name": "Test (Integration)", "status": "COMPLETED", "conclusion": "SUCCESS"},
    {"name": "Generate & Validate Documentation", "status": "COMPLETED", "conclusion": "SUCCESS"},
    {"name": "Summary", "status": "COMPLETED", "conclusion": "SUCCESS"}
  ]
}
```

### Diagnóstico de Problemas

Cuando un workflow falla, la CLI proporciona herramientas de diagnóstico:

```bash
# Ver pasos fallidos
gh run view <run-id> --json jobs --jq '.jobs[] | select(.conclusion == "FAILURE")'

# Obtener logs de un job específico
gh run view <run-id> --job=<job-id> --log

# Buscar errores específicos en los logs
gh run view <run-id> --log | grep -i error

# Ver anotaciones (warnings y errores de linting)
gh run view <run-id>
```

## 28.10. Mejores Prácticas

Implementar CI/CD efectivo requiere seguir convenciones y patrones que maximizan la confiabilidad y mantenibilidad del pipeline.

### Principios Fundamentales

```mermaid
flowchart TB
    subgraph BP["Mejores Prácticas de CI/CD"]
        direction TB
        subgraph S1["Velocidad"]
            direction LR
            S1a[Ejecutar jobs en paralelo]
            S1b[Cachear dependencias]
            S1c[Usar artifacts eficientemente]
        end
        subgraph S2["Confiabilidad"]
            direction LR
            S2a[Tests independientes]
            S2b[Verificaciones condicionales]
            S2c[Logs detallados]
        end
        subgraph S3["Mantenibilidad"]
            direction LR
            S3a[Variables de entorno]
            S3b[Jobs modulares]
            S3c[Documentación]
        end
        subgraph S4["Seguridad"]
            direction LR
            S4a[Secretos cifrados]
            S4b[Permisos mínimos]
            S4c[Revisiones de código]
        end
    end
```

### Optimización de Tiempos de Ejecución

```yaml
# Usar caché para dependencias
- name: Cache NuGet packages
  uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
    restore-keys: |
      ${{ runner.os }}-nuget-

# Ejecutar trabajos en paralelo
jobs:
  test-1:
    runs-on: ubuntu-latest
    steps:
      - run: dotnet test Tests.Part1/Tests.Part1.csproj
  
  test-2:
    runs-on: ubuntu-latest
    steps:
      - run: dotnet test Tests.Part2/Tests.Part2.csproj
```

### Manejo de Secretos

```yaml
# NUNCA hardcodear secrets en el workflow
env:
  # INCORRECTO - Visible en logs
  # DATABASE_URL: "postgres://user:pass@host:5432/db"
  
  # CORRECTO - Usar GitHub Secrets
  DATABASE_URL: ${{ secrets.DATABASE_URL }}
  API_KEY: ${{ secrets.API_KEY }}
```

Los secrets se configuran en Settings > Secrets and variables > Actions del repositorio. Nunca expongas credenciales en el código ni en archivos de workflow públicos.

### Estrategias de Retry

```yaml
steps:
  - name: Flaky operation
    run: |
      # Reintentar hasta 3 veces con backoff exponencial
      for i in {1..3}; do
        if tu-comando-que-puede-fallar; then
          exit 0
        fi
        if [ $i -lt 3 ]; then
          sleep $((2 ** i))
        fi
      done
      exit 1
```

## 28.11. Resumen

La implementación de CI/CD con GitHub Actions representa un salto cualitativo en el desarrollo de software, transformando procesos manuales propensos a errores en flujos automatizados, confiables y reproducibles. A lo largo de este documento hemos explorado los fundamentos teóricos y prácticos necesarios para construir pipelines profesionales.

El pipeline de TiendaDawApi ejemplifica las mejores prácticas de la industria, incluyendo la separación clara de responsabilidades entre jobs, la ejecución paralela de tareas independientes, la gestión eficiente de artefactos y la generación automática de documentación. Cada componente ha sido diseñado para maximizar tanto la velocidad de ejecución como la confiabilidad del proceso de integración continua.

La GitHub CLI emerge como una herramienta indispensable para desarrolladores modernos, permitiendo interactuar con workflows directamente desde la terminal. Esta capacidad democratiza el acceso a la información del pipeline y facilita la automatización de tareas administrativas mediante scripts. Los comandos presentados en este documento proporcionan un toolkit completo para la gestión del ciclo de vida de integraciones y despliegues.

El dominio de estas técnicas prepara al alumnado para entornos profesionales donde la automatización no es opcional sino requisito indispensable. La capacidad de diseñar, implementar y mantener pipelines de CI/CD efectiva diferencia significativamente a los profesionales que pueden garantizar la calidad del software que entregan a sus usuarios.

---

## Recursos Adicionales

- [Documentación oficial de GitHub Actions](https://docs.github.com/es/actions)
- [GitHub CLI Documentation](https://cli.github.com/manual/)
- [Marketplace de Actions](https://github.com/marketplace?type=actions)
- [Ejemplos de Workflows](https://github.com/actions/starter-workflows)
