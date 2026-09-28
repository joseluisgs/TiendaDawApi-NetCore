# ADR-0001: Errores de dominio con patrón Result en lugar de excepciones

|               |                                                                     |
| ------------- | ------------------------------------------------------------------- |
| **ADR**       | `ADR-0001`                                                          |
| **Título**    | Errores de dominio con patrón Result                                |
| **Estado**    | Aceptado                                                            |
| **Fecha**     | 2026-01                                                             |
| **Decisores** | Equipo docente                                                       |

## Contexto

Las operaciones de negocio pueden fallar de formas esperadas: un producto no
existe, una categoría ya está duplicada, un pedido no pertenece al usuario.
En una API REST cada uno de esos fallos debe traducirse en un código HTTP
concreto (404, 409, 403...) y en un cuerpo de respuesta coherente.

## Decisión

Los errores **esperados** del negocio se modelan como **valores**, no como
excepciones:

- Cada operación devuelve `Result<T, TError>` (patrón Result / Railway
  Oriented Programming).
- Los errores son tipos tipados de dominio (`DomainError`) que se traducen a
  HTTP con `DomainErrorExtensions.ToHttpResult()`.
- Solo las situaciones **inesperadas** (fallos de programación, infraestruc-
  tura caída) se lanzan como excepciones y las captura un único punto:
  `Middleware/GlobalExceptionHandler.cs`.

## Consecuencias

### Positivas

- El compilador obliga a manejar el fallo: no se puede ignorar un `Result`.
- Flujo lineal con `Bind`/`Map`/`Match` en lugar de `try/catch` anidados.
- Las respuestas de error son consistentes en toda la API (404 con `{message}`,
  409 con `{message}`, etc.).
- Los tests de error son simples: se verifica el tipo de error y el estado.

### Negativas o riesgos

- Hay dos vías de error que el alumnado debe distinguir: dominio (valores) y
  excepciones (handler global).
- Requiere disciplina: los servicios deben devolver `Result` y no lanzar.

## Alternativas descartadas

| Alternativa                            | Por qué se descartó                                       |
| ------------------------------------- | --------------------------------------------------------- |
| Excepciones para todo el negocio      | Pierde el flujo controlado; el error no está en la firma. |
| Solo `GlobalExceptionHandler`         | No distingue errores esperados de fallos reales.          |
| Códigos de error solo en el cuerpo    | El estado HTTP queda ambiguo y cuesta de testear.         |

## Verificación

- Tests de integración de forma de errores (400/401/404/409/429) sobre los
  endpoints públicos.
- Tests unitarios de los servicios con casos de fallo.

## Referencias en el código y en la documentación

- `Extensions/DomainErrorExtensions.cs` — traducción dominio → HTTP.
- `Middleware/GlobalExceptionHandler.cs` — excepciones inesperadas.
- `doc/11-patron-result.md` — explicación didáctica del patrón Result.
