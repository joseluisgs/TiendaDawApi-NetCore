# ADR-0003: Repositorios sobre EF Core y PostgreSQL para los datos relacionales

|               |                                                                     |
| ------------- | ------------------------------------------------------------------- |
| **ADR**       | `ADR-0003`                                                          |
| **Título**    | Repositorios + EF Core sobre PostgreSQL                             |
| **Estado**    | Aceptado                                                            |
| **Fecha**     | 2026-01                                                             |
| **Decisores** | Equipo docente                                                       |

## Contexto

Usuarios, categorías y productos son datos relacionales con integridad
referencial (un producto pertenece a una categoría; la BD los restringe).
Hay que decidir cómo accede la capa de negocio a PostgreSQL.

## Decisión

- **EF Core** como ORM sobre **PostgreSQL** (`Npgsql`).
- **Repository Pattern**: una interfaz por entidad en `Repositories/` que
  oculta el `DbContext` y el lenguaje de LINQ al resto de capas.
- Las migraciones se versionan en `Migrations/` y el esquema se siembra con
  un seeder (`Data/Seed/Sql/SqlSeeder.cs`) que crea los usuarios demo y los
  datos de ejemplo.
- `TiendaDbContextFactory` permite ejecutar las herramientas de diseño
  (migraciones) sin arrancar la aplicación completa.

## Consecuencias

### Positivas

- El negocio no conoce EF Core: se puede sustituir el ORM tras la interfaz.
- Integridad real a nivel de base de datos (claves foráneas, únicas).
- Migraciones reproducibles y semilla determinista para demos y tests.

### Negativas o riesgos

- Riesgo de «fuga de IQueryable»: los repositorios deben devolver datos
  materializados, no consultas vivas.
- Overhead de una capa más por cada entidad.

## Alternativas descartadas

| Alternativa                       | Por qué se descartó                                    |
| --------------------------------- | ------------------------------------------------------ |
| Acceso directo al DbContext       | Acopla servicios a EF y complica los tests unitarios.  |
| Dapper / SQL a mano               | Más control, pero pierde migraciones y tipado.         |
| Entity Framework sin repositorio  | El ORM se filtra por toda la arquitectura.             |

## Verificación

- Tests de integración sobre PostgreSQL real (contenedor efímero).
- Comprobación de que las migraciones aplican limpias en arranque.

## Referencias en el código y en la documentación

- `Data/TiendaDbContext.cs` — `DbContext` de la aplicación.
- `Repositories/` — interfaces e implementaciones.
- `Data/Seed/Sql/SqlSeeder.cs` — siembra de datos demo.
- `doc/07-repository-pattern.md` — patrón de repositorio.
- `doc/08-ef-core-postgresql.md` — EF Core con PostgreSQL.
