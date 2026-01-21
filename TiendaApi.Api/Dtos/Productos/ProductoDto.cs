using System.ComponentModel.DataAnnotations;

namespace TiendaApi.Api.Dtos.Productos;

/// <summary>
/// DTO (Data Transfer Object) de producto para respuestas de API.
/// Objeto de transferencia que encapsula toda la información de un producto
/// incluyendo sus datos básicos y la información de la categoría asociada.
///
/// Este DTO es inmutable (record) y se utiliza exclusivamente para respuestas del servidor.
/// Expone información desnormalizada incluyendo el nombre de la categoría para evitar
/// consultas adicionales en el cliente.
///
/// <remarks>
/// Uso típico:
/// - Catálogos de productos
/// - Tarjetas de producto en interfaz
/// - Búsquedas y filtrados
/// - Exportaciones de inventario
///
/// Relación con otras entidades:
/// - CategoriaId: Referencia a la categoría del producto
/// - CategoriaNombre: Desnormalización para evitar joins en el cliente
/// </remarks>
/// </summary>
/// <example>
/// Respuesta JSON típica:
/// <code>
/// {
///   "id": 101,
///   "nombre": "iPhone 15 Pro Max",
///   "descripcion": "Último modelo de Apple con chip A17 Pro",
///   "precio": 1199.99,
///   "stock": 50,
///   "imagen": "https://ejemplo.com/iphone15.jpg",
///   "categoriaId": 1,
///   "categoriaNombre": "Electrónica",
///   "createdAt": "2024-01-10T08:00:00Z",
///   "updatedAt": "2024-01-15T12:30:00Z"
/// }
/// </code>
/// </example>
public record ProductoDto(
    /// <summary>
    /// Identificador único del producto en el sistema.
    /// Clave primaria generada automáticamente por la base de datos.
    /// Valor único e incremental que identifica inequívocamente al producto.
    /// </summary>
    /// <example>101</example>
    long Id,

    /// <summary>
    /// Nombre comercial del producto.
    /// Texto visible que identifica el producto en catálogos y búsquedas.
    /// Optimizado para SEO y reconocimiento del usuario.
    /// </summary>
    /// <example>iPhone 15 Pro Max</example>
    string Nombre,

    /// <summary>
    /// Descripción detallada del producto.
    /// Proporciona información adicional sobre características,
    /// especificaciones técnicas, materiales, dimensiones y más.
    /// </summary>
    /// <example>Pantalla Super Retina XDR de 6.7 pulgadas con tecnología ProMotion.</example>
    string Descripcion,

    /// <summary>
    /// Precio unitario del producto en la moneda base del sistema.
    /// Valor decimal con precisión monetaria (2 decimales típicamente).
    /// Puede incluir descuentos aplicados según promociones activas.
    /// </summary>
    /// <remarks>
    /// Consideraciones:
    /// - El precio se muestra con formato monetario en la interfaz
    /// - Puede haber precios diferentes por variante de producto
    /// - Los impuestos se calculan aparte en el checkout
    /// </remarks>
    /// <example>1199.99</example>
    decimal Precio,

    /// <summary>
    /// Cantidad de unidades disponibles en inventario.
    /// Controla la disponibilidad del producto para venta.
    /// Valor cero indica producto agotado temporalmente.
    /// </summary>
    /// <remarks>
    /// Comportamiento:
    /// - Productos con stock = 0 pueden mostrarse como "Agotado"
    /// - Se recomienda hide o deshabilitar botón de compra cuando stock = 0
    /// - Stock negativo no debería ocurrir en operación normal
    /// </remarks>
    /// <example>50</example>
    int Stock,

    /// <summary>
    /// URL de la imagen representativa del producto.
    /// Enlace a recursos multimedia almacenados en el servidor o CDN.
    /// Valor nulo cuando no hay imagen configurada.
    /// </summary>
    /// <remarks>
    /// Formatos recomendados:
    /// - JPG, PNG, WebP para fotos de producto
    /// - SVG para logotipos e iconos
    /// - Tamaño máximo: 2MB por imagen
    /// </remarks>
    /// <example>https://ejemplo.com/imagenes/iphone15-pro-max.jpg</example>
    string? Imagen,

    /// <summary>
    /// Identificador de la categoría a la que pertenece el producto.
    /// Clave foránea que relaciona el producto con su categoría.
    /// Útil para filtrados y organización jerárquica.
    /// </summary>
    /// <example>1</example>
    long CategoriaId,

    /// <summary>
    /// Nombre de la categoría del producto (desnormalizado).
    /// Repetido aquí para evitar consulta adicional a la tabla de categorías.
    /// Mejora el rendimiento en listados y reduce llamadas al servidor.
    /// </summary>
    /// <example>Electrónica</example>
    string CategoriaNombre,

    /// <summary>
    /// Fecha y hora de creación del registro en formato UTC.
    /// Establecida automáticamente por el sistema al insertar el producto.
    /// Utilizado para auditoría, ordenamiento y cálculo de antiguedad.
    /// </summary>
    /// <example>2024-01-10T08:00:00Z</example>
    DateTime CreatedAt,

    /// <summary>
    /// Fecha y hora de última modificación en formato UTC.
    /// Actualizada automáticamente cada vez que se guarda el producto.
    /// Permite detectar cambios recientes y sincronización de cachés.
    /// </summary>
    /// <example>2024-01-15T12:30:00Z</example>
    DateTime UpdatedAt
);

/// <summary>
/// DTO de producto para solicitudes de creación o actualización.
/// Objeto de transferencia utilizado en operaciones POST (crear) y PUT (reemplazar).
/// Define la estructura de datos esperada del cliente para manipulating productos.
///
/// <remarks>
/// Este DTO NO incluye campos de solo lectura como Id, CreatedAt, UpdatedAt.
/// Los campos de sistema se gestionan automáticamente por el servidor.
///
/// Casos de uso:
/// - POST /api/productos: Crear nuevo producto
/// - PUT /api/productos/{id}: Reemplazar producto existente
/// - Formularios de administración de inventario
/// </remarks>
/// </summary>
public record ProductoRequestDto
{
    /// <summary>
    /// Nombre del producto.
    /// Identificador público que aparece en catálogos y resultados de búsqueda.
    /// Debe ser único dentro de su categoría para evitar confusión.
    /// </summary>
    /// <remarks>
    /// Buenas prácticas:
    /// - Incluir marca y modelo cuando aplique
    /// - Evitar abreviaturas que dificulten comprensión
    /// - Longitud óptima: 50-150 caracteres
    /// </remarks>
    /// <example>iPhone 15 Pro Max 256GB</example>
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MinLength(3, ErrorMessage = "El nombre debe tener al menos 3 caracteres")]
    [MaxLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string Nombre { get; init; } = string.Empty;

    /// <summary>
    /// Descripción detallada del producto.
    /// Proporciona información complementaria sobre características,
    /// especificaciones técnicas, materiales, dimensiones y más.
    /// </summary>
    /// <remarks>
    /// Esta descripción se muestra en la página de detalle del producto.
    /// Soporta formato HTML en algunos casos para dar formato enriquecido.
    /// Mantener entre 100-1000 caracteres para optimal lectura.
    /// </remarks>
    /// <example>Pantalla Super Retina XDR de 6.7 pulgadas con chip A17 Pro.</example>
    [MaxLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
    public string Descripcion { get; init; } = string.Empty;

    /// <summary>
    /// Precio del producto en la moneda base.
    /// Valor decimal que representa el costo unitario antes de impuestos.
    /// Debe ser mayor a cero para productos en venta.
    /// </summary>
    /// <remarks>
    /// Validaciones de negocio:
    /// - No se permiten precios negativos o cero
    /// - Precisión máxima: 4 decimales internally
    /// - Visualización: 2 decimales con separador local
    /// </remarks>
    /// <example>1199.99</example>
    [Required(ErrorMessage = "El precio es obligatorio")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
    public decimal Precio { get; init; }

    /// <summary>
    /// Cantidad en stock disponible para venta.
    /// Entero que representa las unidades físicas disponibles.
    /// Utilizado para control de inventario y disponibilidad.
    /// </summary>
    /// <remarks>
    /// Comportamiento del sistema:
    /// - Stock se decrementa con cada venta confirmada
    /// - Stock puede incrementarse manualmente o por recepción
    /// - Alertas automáticas cuando stock bajo configurable
    /// </remarks>
    /// <example>50</example>
    [Required(ErrorMessage = "El stock es obligatorio")]
    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
    public int Stock { get; init; }

    /// <summary>
    /// URL de la imagen del producto.
    /// Enlace público al recurso de imagen almacenado.
    /// Validador asegura formato URL válido.
    /// </summary>
    /// <remarks>
    /// Requisitos de imagen:
    /// - Dimensiones recomendadas: 800x800 píxeles (cuadrado)
    /// - Formatos soportados: JPG, PNG, WebP
    /// - El servidor puede generar miniaturas automáticamente
    /// - Si está vacío, se muestra imagen por defecto
    /// </remarks>
    /// <example>https://cdn.tienda.com/products/iphone-15-pro-max.jpg</example>
    [MaxLength(500, ErrorMessage = "La URL de la imagen no puede exceder 500 caracteres")]
    [Url(ErrorMessage = "Debe ser una URL válida")]
    public string? Imagen { get; init; }

    /// <summary>
    /// Identificador de la categoría del producto.
    /// Clave foránea obligatoria que establece la clasificación del producto.
    /// Debe existir una categoría con el ID proporcionado.
    /// </summary>
    /// <remarks>
    /// Este campo determina:
    /// - En qué sección aparece el producto
    /// - Filtros disponibles para el usuario
    /// - Productos relacionados sugeridos
    /// - Reglas de clasificación especiales
    /// </remarks>
    /// <example>1</example>
    [Required(ErrorMessage = "La categoría es obligatoria")]
    [Range(1, long.MaxValue, ErrorMessage = "Debe seleccionar una categoría válida")]
    public long CategoriaId { get; init; }
}

/// <summary>
/// DTO para actualización parcial de producto (método PATCH).
/// Objeto especializado para operaciones de actualización incremental.
/// Permite modificar campos específicos sin enviar todos los datos del producto.
///
/// <remarks>
/// Características distintivas:
/// - Todos los campos son opcionales (nullable)
/// - Solo los campos presentes en la solicitud se actualizan
/// - Campos omitidos mantienen su valor actual
/// - Ideal para cambios frecuentes como precio o stock
///
/// Uso típico:
/// - Ajuste de precios por promoción
/// - Actualización de inventario
/// - Cambios menores en descripción
/// - Modificación de imagen
/// </remarks>
/// <example>
/// Petición PATCH típica:
/// <code>
/// {
///   "precio": 999.99,
///   "stock": 45
/// }
/// </code>
/// Este ejemplo solo actualiza precio y stock, dejando otros campos sin cambios.
/// </example>
public record ProductoPatchDto
{
    /// <summary>
    /// Nombre del producto (opcional).
    /// Si se proporciona, actualiza el nombre actual.
    /// Mismas restricciones que en creación.
    /// </summary>
    /// <example>iPhone 15 Pro</example>
    [MaxLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string? Nombre { get; init; }

    /// <summary>
    /// Descripción del producto (opcional).
    /// Actualiza la descripción si se incluye en la solicitud.
    /// </summary>
    /// <example>Nuevo modelo con mejores características</example>
    [MaxLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
    public string? Descripcion { get; init; }

    /// <summary>
    /// Precio del producto (opcional).
    /// Útil para cambios de precio temporales o permanentes.
    /// Debe ser mayor a 0 si se incluye.
    /// </summary>
    /// <example>999.99</example>
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
    public decimal? Precio { get; init; }

    /// <summary>
    /// Cantidad en stock disponible (opcional).
    /// Permite ajustar inventario sin actualizar otros campos.
    /// No puede ser negativo.
    /// </summary>
    /// <example>45</example>
    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
    public int? Stock { get; init; }

    /// <summary>
    /// URL de la imagen del producto (opcional).
    /// Actualiza la imagen del producto si se proporciona.
    /// Valor null elimina la imagen actual.
    /// </summary>
    /// <example>https://cdn.tienda.com/products/iphone-15-pro-nuevo.jpg</example>
    [MaxLength(500, ErrorMessage = "La URL de la imagen no puede exceder 500 caracteres")]
    [Url(ErrorMessage = "Debe ser una URL válida")]
    public string? Imagen { get; init; }
}
