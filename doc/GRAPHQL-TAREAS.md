# GraphQL: Lista de Tareas

**Fecha:** 17/01/2026  
**Proyecto:** TiendaDawApi-NetCore

---

## 📝 QUERIES (Consultas - LEER)

| Tarea | Estado | Esfuerzo |
|-------|--------|----------|
| Query: obtener todos los productos | ✅ TERMINADO | 0h |
| Query: obtener producto por ID | ✅ TERMINADO | 0h |
| Query: productos paginados | ✅ TERMINADO | 0h |
| Query: obtener todas las categorías | ✅ TERMINADO | 0h |
| Query: obtener categoría por ID | ✅ TERMINADO | 0h |
| Query: categorías paginadas | ✅ TERMINADO | 0h |

**Total Queries:** 6/6 completadas (100%)

---

## ✏️ MUTATIONS (Escritura - CREAR/EDITAR/BORRAR)

### ⚠️ IMPORTANTE: Autorización por Roles

Las mutations requieren **token JWT con rol ADMIN** en el header:

```http
Authorization: Bearer <tu_token_jwt>
```

| Lo que pasa | Resultado |
|-------------|-----------|
| Sin token | ❌ Error: "Unauthorized" |
| Token de USER | ❌ Error: "Forbidden" (no tiene rol ADMIN) |
| Token de ADMIN | ✅ Funciona |

> **Nota:** Es igual que en REST. La política "AdminOnly" ya existe.

---

| Tarea | Estado | Esfuerzo | Notas |
|-------|--------|----------|-------|
| Mutation: crear producto | ⬜ PENDIENTE | 1h | Requiere rol ADMIN |
| Mutation: actualizar producto | ⬜ PENDIENTE | 1h | Requiere rol ADMIN |
| Mutation: eliminar producto | ⬜ PENDIENTE | 1h | Requiere rol ADMIN |
| Mutation: crear categoría | ⬜ PENDIENTE | 1h | Requiere rol ADMIN |
| Mutation: actualizar categoría | ⬜ PENDIENTE | 1h | Requiere rol ADMIN |
| Mutation: eliminar categoría | ⬜ PENDIENTE | 1h | Requiere rol ADMIN |

**Total Mutations:** 0/6 completadas  
**Tiempo estimado:** 4-6 horas

### ¿Cómo pasar el token?

```http
POST /graphql
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{"query": "mutation { createProducto(input: {...}) { id nombre } }"}
```

En GraphiQL: usar la sección "HTTP Headers"

---

## 📡 SUBSCRIPTIONS (Tiempo Real - NOTIFICACIONES)

| Tarea | Estado | Esfuerzo | Notas |
|-------|--------|----------|-------|
| Explicar concepto de subscriptions | ⬜ PENDIENTE | 30min | Solo documentación |
| Documentar diferencia con WebSockets | ✅ TERMINADO | - | Ya está en doc 20 |
| Subscription: producto creado | ❌ NO IMPLEMENTAR | - | Ya tenemos WebSockets |
| Subscription: producto actualizado | ❌ NO IMPLEMENTAR | - | Ya tenemos WebSockets |
| Subscription: categoría creada | ❌ NO IMPLEMENTAR | - | Ya tenemos WebSockets |
| Subscription: stock bajo | ❌ NO IMPLEMENTAR | - | Ya tenemos WebSockets |

**Total Subscriptions:** 0/6 (solo documentación explicativa)

---

## 📊 Resumen General

| Tipo | Total | Hechas | Pendientes | No implementar |
|------|-------|--------|------------|----------------|
| Queries | 6 | 6 ✅ | 0 | 0 |
| Mutations | 6 | 0 | 6 ⬜ | 0 |
| Subscriptions | 5 | 1 | 1 | 4 ❌ |
| **TOTAL** | **17** | **7** | **7** | **4** |

---

## 🎯 Siguientes Pasos Inmediatos

1. **Implementar mutations de producto** (crear, actualizar, eliminar)
   - Conectar con `IProductoService` existente
   - Usar `[Authorize(policy: "AdminOnly")]`
   - Reutilizar DTOs de REST

2. **Implementar mutations de categoría** (crear, actualizar, eliminar)
   - Conectar con `ICategoriaService` existente
   - Usar `[Authorize(policy: "AdminOnly")]`
   - Reutilizar DTOs de REST

---

## 📚 Lo que se Enseña

| Operación | Conceptos Didácticos |
|-----------|---------------------|
| REST Query | HTTP verbs, status codes, JSON |
| REST Mutation | JWT, roles, validación, Result pattern |
| WebSockets | Conexiones persistentes, pub/sub, broadcast |
| GraphQL Query | Schema, tipos, queries anidadas, proyecciones |
| **GraphQL Mutation** | Input types, mutations, autorización en GraphQL, Result pattern |
| GraphQL Subscription | Pub/sub, EventBus, tiempo real tipado (solo teoría) |

---

## 🔗 Endpoints

| Tipo | Endpoint | Autenticación |
|------|----------|---------------|
| REST | `/api/productos`, `/api/categorias` | JWT (ADMIN para writes) |
| WebSockets | `/ws/v1/productos`, `/ws/v1/pedidos` | JWT en query string |
| GraphQL | `/graphql` | JWT en header (ADMIN para mutations) |
| GraphiQL | `/graphiql` | Playground para pruebas |
