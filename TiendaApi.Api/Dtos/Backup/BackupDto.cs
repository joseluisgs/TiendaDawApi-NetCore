namespace TiendaApi.Api.Dtos.Backup;

/// <summary>
/// Descriptor de un backup: qué contiene, cuándo se generó y con qué formato.
/// Va dentro del propio backup (manifest.json) para poder leerlo sin contexto.
/// </summary>
public record ManifestBackupDto(
    string Version,
    DateTime GeneradoEn,
    IReadOnlyDictionary<string, int> Entidades,
    bool IncluirBorrados,
    string Formato
);

/// <summary>
/// Un producto tal como se guarda en el backup: plano, con las claves foráneas
/// tal cual (categoriaId y proveedorId) para poder reinsertarlo sin ambigüedad.
/// </summary>
public record BackupProductoDto(
    long Id,
    string Nombre,
    string Descripcion,
    decimal Precio,
    int Stock,
    string? Imagen,
    long CategoriaId,
    string? ProveedorId,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>Una categoría tal como se guarda en el backup.</summary>
public record BackupCategoriaDto(
    long Id,
    string Nombre,
    string? Descripcion,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>Un proveedor tal como se guarda en el backup. El código es su clave.</summary>
public record BackupProveedorDto(
    string Codigo,
    string Nombre,
    string? Descripcion,
    string? Contacto,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// Contenido de un backup en formato JSON. Las entidades no pedidas llegan a null.
/// </summary>
public record BackupDto(
    ManifestBackupDto Manifest,
    IReadOnlyList<BackupCategoriaDto>? Categorias,
    IReadOnlyList<BackupProductoDto>? Productos,
    IReadOnlyList<BackupProveedorDto>? Proveedores
);

// ────────────────────────────────────────────────────────────────
// Import
// ────────────────────────────────────────────────────────────────

/// <summary>
/// Informe de una importación. Los errores de fila NO abortan: se acumulan aquí
/// y <c>Completo</c> indica si se escribió todo sin incidencias.
/// </summary>
public record ImportReportDto(
    bool DryRun,
    string Formato,
    bool Completo,
    long DuracionMs,
    IReadOnlyList<EntidadImportReportDto> Entidades
);

/// <summary>Contadores de una entidad dentro del informe de importación.</summary>
public record EntidadImportReportDto(
    string Entidad,
    int Leidos,
    int Creados,
    int Actualizados,
    int SinCambios,
    IReadOnlyList<ErrorFilaDto> Errores
);

/// <summary>Un error de fila: en qué línea, de qué campo y por qué.</summary>
public record ErrorFilaDto(
    int Fila,
    string Campo,
    string Mensaje
);

// ────────────────────────────────────────────────────────────────
// Catálogo (GET formatos)
// ────────────────────────────────────────────────────────────────

/// <summary>
/// Lo que la API admite en backup: formatos, entidades, límites y reglas.
/// Pensado para documentar el contrato sin tener que leer la fuente.
/// </summary>
public record CatalogoFormatosDto(
    IReadOnlyList<FormatoDto> Formatos,
    IReadOnlyList<EntidadCatalogoDto> Entidades,
    LimitesBackupDto Limites,
    ReglasBackupDto Reglas
);

/// <summary>Un formato soportado por export e import.</summary>
public record FormatoDto(
    string Nombre,
    string ContentType,
    string Extension,
    string Descripcion
);

/// <summary>Una entidad exportable y sus campos.</summary>
public record EntidadCatalogoDto(
    string Nombre,
    string Clave,
    IReadOnlyList<string> Campos
);

/// <summary>Límites de tamaño para proteger al servidor.</summary>
public record LimitesBackupDto(
    int MaxBytes,
    int MaxDescomprimido,
    int MaxEntradasZip
);

/// <summary>Reglas de comportamiento del import.</summary>
public record ReglasBackupDto(
    string Upsert,
    string DryRun,
    string Orden
);
