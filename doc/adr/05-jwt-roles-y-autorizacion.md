# ADR-0005: Autenticación JWT con roles ADMIN/USER

|               |                                                                     |
| ------------- | ------------------------------------------------------------------- |
| **ADR**       | `ADR-0005`                                                          |
| **Título**    | JWT Bearer + autorización por roles                                 |
| **Estado**    | Aceptado                                                            |
| **Fecha**     | 2026-01                                                             |
| **Decisores** | Equipo docente                                                       |

## Contexto

La API expone operaciones públicas (listar productos) y operaciones
restringidas (crear categorías solo para administradores; «mis pedidos» solo
para el usuario autenticado). La API es consumida por web, móvil y scripts:
la autenticación debe ser sin estado.

## Decisión

- **JWT Bearer** para autenticación: `POST /api/v1/auth/signin` devuelve un
  token firmado con los claims del usuario (incluido el rol).
- **Dos roles**: `ADMIN` y `USER`, declarados como claims en el token.
- **Autorización declarativa** con políticas (`[Authorize(Policy =
  "RequireAdminRole")]`) y `[Authorize]` para cualquier usuario.
- El bloque de login/registro se **versiona** (`/api/v1/auth/...`) mientras
  el resto de la API mantiene las rutas sin versión.
- Contraseñas con hash (BCrypt); el token nunca se persiste en servidor.

## Consecuencias

### Positivas

- Sin estado en el servidor: la escala horizontal no necesita sesiones.
- El controlador declara sus requisitos con atributos, legibles de un vistazo.
- Cada recurso puede exigir su política sin código imperativo.

### Negativas o riesgos

- Un JWT válido no se puede revocar antes de caducar → hay que mantener
  ventanas de caducidad cortas.
- Los errores de autorización (401 vs 403) deben distinguirse bien para no
  filtrar información.

## Alternativas descartadas

| Alternativa                       | Por qué se descartó                                    |
| --------------------------------- | ------------------------------------------------------ |
| Cookies de sesión                 | Estado en servidor; peor para clientes móviles/SPA.    |
| API Keys fijas por usuario        | Sin roles ni expiración granular.                      |
| OAuth2 con proveedor externo      | Fuera del alcance didáctico del proyecto.              |

## Verificación

- E2E de autenticación: registro, login, token inválido (401), rol insufi-
  ciente (403), recursos públicos sin token.
- Pruebas de forma de errores: 401 con cabecera `WWW-Authenticate`.

## Referencias en el código y en la documentación

- `Controllers/AuthController.cs` — versionado `/api/v1/auth`.
- Políticas de autorización registradas en la configuración de controllers.
- `doc/15-jwt-authentication.md` — JWT: tokens y claims.
- `doc/16-autorizacion-roles.md` — políticas y roles.
