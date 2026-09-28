# ADR-0008: Caché multinivel (Cache-Aside + OutputCache con ETag)

|               |                                                                     |
| ------------- | ------------------------------------------------------------------- |
| **ADR**       | `ADR-0008`                                                          |
| **Título**    | Caché en dos niveles con invalidación por etiquetas                 |
| **Estado**    | Aceptado                                                            |
| **Fecha**     | 2026-02                                                             |
| **Decisores** | Equipo docente                                                       |

## Contexto

Los catálogos (productos, categorías) se leen mucho y se escriben poco. Hay
dos puntos donde cachear: dentro del servicio (antes de tocar la base de
datos) y a nivel HTTP (antes de serializar la respuesta).

## Decisión

- **Nivel aplicación — Cache-Aside**: `Services/Cache/RedisCacheService.cs`
  almacena objetos serializados en Redis con TTL; el servicio primero consulta
  la caché y solo en fallo va a la base de datos (y rellena la caché).
- **Nivel HTTP — OutputCache + ETag**: solo los endpoints que lo declaran con
  `[OutputCache(...)]` se cachean (`Infrastructures/OutputCacheConfig.cs`);
  las respuestas llevan ETag y se contesta **304 Not Modified** cuando el
  cliente trae un `If-None-Match` vigente.
- **Invalidación por etiquetas**: tras cada escritura se expulsan las entradas
  con la etiqueta correspondiente (`EvictByTagAsync`), también desde GraphQL.

## Consecuencias

### Positivas

- Menos viajes a la base de datos y respuestas HTTP más baratas.
- El ETag ahorra ancho de banda: el cliente recibe 304 en lugar del cuerpo.
- La invalidación por etiquetas evita servir catálogos obsoletos.

### Negativas o riesgos

- Una escritura olvidada sin evicción deja datos viejos en caché.
- Cachear en dos sitios duplica el efecto: hay que saber en qué nivel tocar.

## Alternativas descartadas

| Alternativa                       | Por qué se descartó                                     |
| --------------------------------- | ------------------------------------------------------- |
| Solo caché en memoria             | Se pierde entre reinicios y no se comparte.              |
| Cachear todo sin declarar         | Riesgo de respuestas privadas o obsoletas.               |
| Invalidación por tiempo (TTL)     | Demasiada ventana con datos anticuados.                  |

## Verificación

- Tests del servicio de caché (hit/miss/evicción).
- E2E de ETag: segunda petición con `If-None-Match` → 304.

## Referencias en el código y en la documentación

- `Services/Cache/RedisCacheService.cs` — caché de aplicación.
- `Infrastructures/OutputCacheConfig.cs` — OutputCache + ETag + 304.
- `doc/10-redis-caching.md` — patrón Cache-Aside con Redis.
- `doc/27-optimizacion.md` — técnicas de optimización HTTP.
