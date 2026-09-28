# ADR-0010: Contrato OpenAPI verificado y documentación Swagger didáctica

|               |                                                                     |
| ------------- | ------------------------------------------------------------------- |
| **ADR**       | `ADR-0010`                                                          |
| **Título**    | Contrato OpenAPI verificado automáticamente                         |
| **Estado**    | Aceptado                                                            |
| **Fecha**     | 2026-04                                                             |
| **Decisores** | Equipo docente                                                       |

## Contexto

La API es el contrato entre quien la expone y quien la consume. Un cambio
accidental en una ruta, un estado o un campo del JSON rompe a los clientes
sin que el compilador avise. El proyecto existe en dos variantes de
arquitectura que deben ofrecer exactamente la misma superficie HTTP.

## Decisión

- **Swagger/OpenAPI como documentación viva**: esquema generado en tiempo de
  ejecución, enriquecido con los comentarios XML del código
  (`IncludeXmlComments`) y con ejemplos de petición/respuesta.
- **Versionado de API** en el bloque de autenticación (`/api/v1/auth`) para
  poder evolucionarlo sin romper al resto de rutas.
- **Comprobación automática de contrato** (`scripts/check-openapi.mjs`): la
  aplicación se arranca en un puerto de prueba, se descarga su documento
  OpenAPI y se compara en profundidad con el de la otra variante del
  proyecto. Cualquier diferencia rompe la comprobación.
- Complemento manual: ficheros `.http` en `doc/http/` para probar cada ruta
  desde el editor.

## Consecuencias

### Positivas

- El contrato está versionado y se valida en cada revisión de código.
- La documentación sale del propio código: no se desincroniza a mano.
- Probar la API no exige instalar clientes externos: basta el editor.

### Negativas o riesgos

- Hay que mantener los comentarios XML al día (una propiedad sin documentar
  empeora el esquema).
- La comparación exige que ambas variantes arranquen igual de bien.

## Alternativas descartadas

| Alternativa                        | Por qué se descartó                                    |
| ---------------------------------- | ------------------------------------------------------ |
| Documentación en Markdown a mano   | Se desincroniza del código.                            |
| Sin contrato exportable            | Los consumidores adivinan la API.                      |
| Comparar solo la lista de rutas    | No detecta cambios en cuerpos ni estados.              |

## Verificación

- Comprobación automática de contrato OpenAPI (deep-diff) en ambos sentidos.
- Ficheros `doc/http/` con cobertura de todas las rutas de la API.

## Referencias en el código y en la documentación

- `scripts/check-openapi.mjs` — comparación del contrato entre variantes.
- `doc/http/` — peticiones de ejemplo listas para ejecutar.
- `doc/23-documentacion.md` — documentación y versionado de APIs.
