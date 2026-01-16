# TiendaApi .NET - Tests E2E con Postman y Newman

## 📁 Archivos incluidos

| Archivo | Descripción |
|---------|-------------|
| `TiendaApi.NetCore.postman_collection.json` | Colección completa de tests E2E |
| `TiendaApi.NetCore.postman_environment.json` | Variables de entorno |
| `test-image.png` | Imagen de prueba para tests de upload |
| `README.md` | Este archivo |

## 🚀 Uso rápido

### 1. Importar en Postman

1. Abrir Postman
2. Importar → Seleccionar `TiendaApi.NetCore.postman_collection.json`
3. Importar → Seleccionar `TiendaApi.NetCore.postman_environment.json`
4. Seleccionar el environment "TiendaApi .NET - Environment"

### 2. Configurar variables

Editable en Postman: **Manage Environments** o directamente en el JSON:

```json
{
    "baseUrl": "http://localhost:5000",
    "adminUsername": "admin",
    "adminPassword": "Admin1234",
    "userUsername": "user",
    "userPassword": "User1234"
}
```

### 3. Ejecutar tests en Postman

1. Seleccionar el environment
2. Ejecutar Collection Runner
3. Ejecutar todos los tests o por carpeta

### 4. Ejecutar con Newman

```bash
# Instalar Newman si no está instalado
npm install -g newman

# Ejecutar tests
newman run TiendaApi.NetCore.postman_collection.json \
  -e TiendaApi.NetCore.postman_environment.json \
  --reporters cli,junit,html \
  --reporter-junit-export results.xml \
  --reporter-html-export results.html
```

## 📋 Estructura de la colección

```
TiendaApi .NET - Tests E2E Completos
├── 0 - SETUP
│   ├── Health Check
│   └── Limpiar datos de test anteriores
├── 1 - AUTHENTICATION
│   ├── Signup - Usuario nuevo ✅
│   ├── Signup - Error username vacío ❌
│   ├── Signup - Error email inválido ❌
│   ├── Signup - Error usuario duplicado ❌
│   ├── Signup - Error email duplicado ❌
│   ├── Signin - Admin ✅
│   ├── Signin - Usuario ✅
│   ├── Signin - Error credenciales inválidas ❌
│   └── Signin - Error usuario no existe ❌
├── 2 - CATEGORÍAS
│   ├── GET All (Público) ✅
│   ├── GET All con filtros ✅
│   ├── GET By Id (Público) ✅
│   ├── GET By Id - No existe ❌
│   ├── POST - Crear (Admin) ✅
│   ├── POST - Error sin auth ❌
│   ├── POST - Error con rol USER ❌
│   ├── POST - Error nombre duplicado ❌
│   ├── PUT - Actualizar (Admin) ✅
│   ├── PUT - No existe ❌
│   ├── DELETE - Eliminar (Admin) ✅
│   └── DELETE - No existe ❌
├── 3 - PRODUCTOS
│   ├── GET All (Público) ✅
│   ├── GET All con filtros ✅
│   ├── GET By Id (Público) ✅
│   ├── GET By Id - No existe ❌
│   ├── GET By Categoria ✅
│   ├── POST - Crear (Admin) ✅
│   ├── POST - Error categoría no existe ❌
│   ├── POST - Error validación (precio negativo) ❌
│   ├── PUT - Actualizar (Admin) ✅
│   ├── PATCH - Actualizar parcial (Admin) ✅
│   ├── PATCH - Subir imagen (Admin) ✅
│   └── DELETE - Eliminar (Admin) ✅
├── 4 - PEDIDOS (Admin)
│   ├── GET All ✅
│   ├── GET All paginated ✅
│   ├── GET By Id ✅
│   ├── GET By Id - No existe ❌
│   ├── PUT - Actualizar estado ✅
│   ├── PUT - Actualizar completo ✅
│   └── DELETE ✅
├── 5 - PEDIDOS (Usuario)
│   ├── GET Mis Pedidos ✅
│   ├── POST - Crear pedido ✅
│   ├── POST - Error producto no existe ❌
│   ├── POST - Error sin auth ❌
│   ├── GET By Id (Mi pedido) ✅
│   ├── GET By Id - No es mi pedido ❌
│   └── DELETE - Cancelar pedido ✅
├── 6 - USUARIOS
│   ├── Gestión Admin
│   │   ├── GET All ✅
│   │   ├── GET By Id ✅
│   │   ├── GET By Id - No existe ❌
│   │   ├── POST - Crear usuario ✅
│   │   ├── PUT - Actualizar usuario ✅
│   │   ├── DELETE - Eliminar usuario ✅
│   │   ├── Error sin auth ❌
│   │   └── Error con rol USER ❌
│   └── Perfil Propio
│       ├── GET Mi Perfil ✅
│       ├── PUT Actualizar Mi Perfil ✅
│       ├── PATCH Actualizar Avatar ✅
│       └── DELETE Mi Cuenta ✅
├── 7 - STORAGE
│   ├── GET Image ✅
│   └── GET Image - No existe ❌
└── 9 - TEARDOWN
    └── Limpiar variables
```

## ✅ Tests incluidos

- **Happy path**: Todos los endpoints principales
- **Validaciones**: 400 Bad Request
- **Autenticación**: 401 Unauthorized
- **Autorización**: 403 Forbidden
- **Recursos no encontrados**: 404 Not Found
- **Conflictos**: 409 Conflict

## 🔧 Scripts automatizados

Los tests incluyen assertions para:
- Status codes correctos
- Estructura de respuestas JSON
- Campos requeridos
- Tipos de datos
- Valores esperados

## 📝 Notas

- Los tests de **signup** generan usernames únicos automáticamente
- Los IDs de recursos creados se guardan en variables de colección
- Los tokens se almacenan automáticamente después del login
- La carpeta **TEARDOWN** limpia las variables al final

## 🐳 Docker + Newman

```bash
# Con Docker
docker run --rm \
  -v $(pwd):/etc/newman \
  postman/newman:latest \
  run TiendaApi.NetCore.postman_collection.json \
  -e TiendaApi.NetCore.postman_environment.json \
  --reporters cli,html \
  --reporter-html-export results.html
```

## 📊 Reportes

Newman genera reportes en:
- Console (cli)
- JUnit XML (`results.xml`)
- HTML (`results.html`)
