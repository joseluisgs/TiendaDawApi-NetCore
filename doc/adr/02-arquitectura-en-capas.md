# ADR-0002: Arquitectura en capas con controladores delgados

|               |                                                                     |
| ------------- | ------------------------------------------------------------------- |
| **ADR**       | `ADR-0002`                                                          |
| **Título**    | Arquitectura en capas (Onion-like híbrida)                          |
| **Estado**    | Aceptado                                                            |
| **Fecha**     | 2026-01                                                             |
| **Decisores** | Equipo docente                                                       |

## Contexto

La API concentra mucha responsabilidad: HTTP, validación, autenticación,
reglas de negocio, persistencia en tres almacenes distintos (PostgreSQL,
MongoDB, Redis), tiempo real y GraphQL. Sin una estructura clara, cualquier
cambio toca media aplicación.

## Decisión

Organizar el código en capas con dependencias hacia dentro:

```
Controllers  →  Services  →  Repositories  →  Datos (EF/Mongo/Redis)
   HTTP          negocio       abstracción         infraestructura
```

- **Controllers**: entry point HTTP; delgados (routing, model binding,
  autorización, llamada a servicio). Nunca acceden a datos directamente.
- **Services**: lógica de negocio y transacciones; devuelven `Result`.
- **Repositories**: abstracción de persistencia (interfaz por entidad).
- **Infrastructures**: extensiones de configuración (DI, pipeline, caché,
  health checks, rate limiting...) que ensamblan las piezas en `Program.cs`.

## Consecuencias

### Positivas

- Cambios de infraestructura (BD, caché, middlewares) no tocan el negocio.
- Cada capa se testea con su doble (fakes de repositorio, servicios puros).
- `Program.cs` queda legible: solo extensiones con nombre.

### Negativas o riesgos

- Exceso de ficheros y saltos de un método a otro (coste de lectura).
- Riesgo de que los controladores engorden si no se mantiene el límite.

## Alternativas descartadas

| Alternativa                          | Por qué se descartó                                  |
| ----------------------------------- | ---------------------------------------------------- |
| Todo en el controlador              | Inmantenible; imposible de testear sin HTTP.         |
| Sin capas (scripts horizontales)    | Acoplamiento total de HTTP, negocio y datos.         |
| Arquitectura hexagonal completa     | Más ceremonia de la necesaria para el alcance.       |

## Verificación

- Organización de carpetas (`Controllers`, `Services`, `Repositories`,
  `Infrastructures`) revisada en cada incorporación de código.
- Tests por capa: unitarios de servicios, integración de repositorios,
  E2E sobre HTTP.

## Referencias en el código y en la documentación

- `Infrastructures/` — extensiones de configuración por módulo.
- `doc/29-clean-architecture.md` — capas y dependencias.
- `doc/12-servicios-negocio.md` — rol de los servicios.
- `doc/30-organizacion-program.md` — organización de `Program.cs`.
