# ADR-0007: Rate limiting propio sobre la API nativa de .NET

|               |                                                                     |
| ------------- | ------------------------------------------------------------------- |
| **ADR**       | `ADR-0007`                                                          |
| **Título**    | Rate limiting nativo (middleware propio)                            |
| **Estado**    | Aceptado                                                            |
| **Fecha**     | 2026-03                                                             |
| **Decisores** | Equipo docente                                                       |

## Contexto

La API necesita limitar el número de peticiones por cliente para resistir
fuerza bruta y abusos (login es el punto más sensible). Debe funcionar
detrás de un proxy (la IP real llega en `X-Forwarded-For`) y ser explicable
en clase sin esconderse detrás de una librería opaca.

## Decisión

Implementar el rate limiting con un **middleware propio** construido sobre
`System.Threading.RateLimiting` (API incluida en .NET, sin dependencias de
terceros), con la misma semántica que antes se cubría con una librería
externa:

- Partición por **cliente (IP + verbo + ruta)**: cada combinación tiene su
  propia ventana.
- Ventanas fijas:
  - General: **100 peticiones / 15 s**.
  - Autenticación (`/api/v1/auth/*`): **10 / minuto**.
  - Escritura (POST): **20 / minuto**.
- Si dos reglas aplican al mismo tiempo, **manda la más restrictiva**.
- Respuesta **429** con cuerpo JSON (`errorType: RateLimitError`) y cabeceras
  `RateLimit-Limit`, `RateLimit-Remaining`, `RateLimit-Reset` y `Retry-After`.

## Consecuencias

### Positivas

- Cero dependencias de terceros para esta función.
- Reglas visibles y comentadas en el código: material didáctico directo.
- Cabeceras estándar: los clientes pueden reintentar con criterio.

### Negativas o riesgos

- Mantenemos nosotros el código (tests y arranque del servidor incluidos).
- En múltiples réplicas el conteo es por instancia (no hay almacén global).

## Alternativas descartadas

| Alternativa                          | Por qué se descartó                                       |
| ------------------------------------ | --------------------------------------------------------- |
| Librería de rate limiting externa    | Configuración externa opaca y una dependencia más.        |
| Límites en el proxy/API Gateway      | No funciona en desarrollo local ni es visible en clase.   |
| Sin límite                           | Expuesto a fuerza bruta en login.                         |

## Verificación

- Tests unitarios del middleware (429 y cabeceras).
- E2E: ráfaga de peticiones desde la misma IP → 429 con `Retry-After`.

## Referencias en el código y en la documentación

- `Middleware/RateLimitMiddleware.cs` — middleware y particiones.
- `Infrastructures/RateLimitConfig.cs` — límites y ventanas.
- `doc/17-seguridad-http.md` — seguridad HTTP y abuso.
