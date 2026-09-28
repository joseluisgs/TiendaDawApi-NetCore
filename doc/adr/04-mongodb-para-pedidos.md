# ADR-0004: MongoDB para pedidos, con repositorio polimórfico

|               |                                                                     |
| ------------- | ------------------------------------------------------------------- |
| **ADR**       | `ADR-0004`                                                          |
| **Título**    | Pedidos como documentos en MongoDB                                 |
| **Estado**    | Aceptado                                                            |
| **Fecha**     | 2026-01                                                             |
| **Decisores** | Equipo docente                                                       |

## Contexto

Un pedido es un **agregado**: destinatario, dirección de envío y lista de
items con su precio congelado. Se lee casi siempre completo, crece de forma
variable y no necesita joins con el resto de tablas.

## Decisión

- Guardar los pedidos en **MongoDB** como documentos embebidos (destinatario
  e items anidados dentro del pedido).
- Exponerlo tras **una única interfaz** `IPedidosRepository` con dos
  implementaciones intercambiables:
  - `PedidosNativeRepository` — driver oficial `MongoDB.Driver` (la usada por
    defecto).
  - `PedidosEfCoreRepository` — proveedor de EF Core para MongoDB (alternativa
    para comparar comportamientos).
- La elección se hace en el registro de dependencias
  (`RepositoriesConfig.cs`), sin que el resto de la aplicación lo note.

## Consecuencias

### Positivas

- El agregado de pedido se guarda y se lee en un solo documento: sin joins.
- Flexibilidad de esquema para campos opcionales (teléfono, observaciones).
- Las dos implementaciones permiten estudar las diferencias entre driver
  nativo y ORM.

### Negativas o riesgos

- Pierdes integridad referencial del lado de la BD (el producto referido en
  un item no se valida con FK).
- Dos implementaciones que deben mantener el mismo contrato.

## Alternativas descartadas

| Alternativa                              | Por qué se descartó                                  |
| ---------------------------------------- | ---------------------------------------------------- |
| Pedidos en PostgreSQL con tablas hijas   | Lee/escritura fragmentada de un agregado que se lee entero. |
| MongoDB sin interfaz compartida          | El almacén se filtraría al negocio.                  |
| Un solo ORM sin alternativas             | No se pueden comparar enfoques de acceso.            |

## Verificación

- Tests de integración de pedidos sobre MongoDB real (contenedor efímero).
- Colección E2E de pedidos (usuario y administrador) idéntica en comportamiento.

## Referencias en el código y en la documentación

- `Repositories/Pedidos/IPedidosRepository.cs` — contrato común.
- `Repositories/Pedidos/PedidosNativeRepository.cs` — driver nativo.
- `Repositories/Pedidos/PedidosEfCoreRepository.cs` — variante EF Core.
- `Infrastructures/RepositoriesConfig.cs` — selección de implementación.
- `doc/09-mongodb.md` — introducción a MongoDB.
- `doc/13-pedidos-transacciones.md` — pedidos y transacciones.
