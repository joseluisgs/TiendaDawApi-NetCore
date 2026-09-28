# ADR-0009: Tests de integración con Testcontainers y BD por clase

|               |                                                                     |
| ------------- | ------------------------------------------------------------------- |
| **ADR**       | `ADR-0009`                                                          |
| **Título**    | Integración real con contenedores efímeros                          |
| **Estado**    | Aceptado                                                            |
| **Fecha**     | 2026-03                                                             |
| **Decisores** | Equipo docente                                                       |

## Contexto

Los tests de integración deben comprobar que la API funciona con
PostgreSQL y MongoDB de verdad (SQL generado, migraciones, driver Mongo), no
con dobles simulados. Además, cada test necesita un estado limpio: no puede
depender de lo que haya dejado el test anterior.

## Decisión

- **Testcontainers** levanta contenedores reales de `Testcontainers.PostgreSql`
  y `Testcontainers.MongoDb` cuando arranca la suite.
- **Un único reparto por ensamblado**: la fixture
  `Integration/TestContainers/AssemblyContainerFixture.cs` arranca 1
  PostgreSQL y 1 MongoDB para toda la ejecución, en lugar de uno por clase
  (el arranque de contenedores es lo caro).
- **Base de datos por clase de test**: cada clase crea su propia base de datos
  (y la elimina al terminar) sobre ese mismo contenedor, de modo que las clases
  corren aisladas sin pagar el arranque.
- Stack: **NUnit** con FluentAssertions; los tests que no necesitan contenedores
  se agrupan en espacio de nombres aparte y se pueden saltar con filtros.

## Consecuencias

### Positivas

- Cobertura real: se prueba el SQL y el driver, no un simulacro.
- Aislamiento por clase sin duplicar el coste de arrancar Docker.
- Mismo comportamiento en local y en integración continua (solo hace falta
  Docker).

### Negativas o riesgos

- Los tests requieren Docker instalado y en marcha.
- Los tests de integración son más lentos que los unitarios.

## Alternativas descartadas

| Alternativa                         | Por qué se descartó                                   |
| ----------------------------------- | ----------------------------------------------------- |
| Base de datos compartida en la nube | Datos residuales entre ejecuciones; frágil.           |
| SQLite como doble                   | No valida PostgreSQL ni MongoDB (dialectos, tipos).   |
| Mock de los repositorios            | No prueba mapeos, consultas ni migraciones.           |
| Contenedor por clase de test        | Arranque de Docker multiplicado por cada clase.       |

## Verificación

- Dos pasadas consecutivas de la suite con 0 fallos (misma BD por clase
  reproducible).
- Los tests de integración se pueden excluir con un filtro para dejar la suite
  sin dependencias.

## Referencias en el código y en la documentación

- `Integration/TestContainers/AssemblyContainerFixture.cs` — fixture por
  ensamblado con bases de datos por clase.
- `TiendaApi.Tests.csproj` — paquetes `Testcontainers.PostgreSql` y
  `Testcontainers.MongoDb`.
- `doc/24-testing.md` — estrategia de testing del proyecto.
