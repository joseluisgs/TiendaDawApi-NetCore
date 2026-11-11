using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using CSharpFunctionalExtensions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.OutputCaching;
using TiendaApi.Api.Dtos.Backup;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Dtos.Proveedores;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Backup;
using TiendaApi.Api.Infrastructures;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Repositories.Proveedores;
using TiendaApi.Api.Services.Cache;
using Path = System.IO.Path;

namespace TiendaApi.Api.Services.Backup;

/// <summary>
///     Servicio de backup: exportación e importación de productos, categorías y
///     proveedores en zip, json o csv.
///     🎓 Tres decisiones que vale la pena entender:
///     <list type="number">
///         <item>
///             <b>El export escribe en streaming</b> (nunca monta el fichero entero
///             en memoria) y <b>el import lee con límites</b>: nº de entradas y tamaño
///             descomprimido, para que un zip hostil no sature al servidor.
///         </item>
///         <item>
///             <b>Los IDs se conservan</b>: las columnas de identidad son
///             <c>GENERATED ALWAYS</c> y rechazan valores explícitos, así que el alta con
///             ID propio va por SQL con <c>OVERRIDING SYSTEM VALUE</c> y después se
///             re-sincroniza la secuencia (si no, la siguiente creación chocaría).
///         </item>
///         <item>
///             <b>Los errores de fila no abortan</b>: se acumulan en el informe y
///             <c>Completo</c> indica si todo entró limpio. <c>dryRun</c> ejecuta el mismo
///             camino sin escribir nada.
///         </item>
///     </list>
///     Orden de importación: categorías → proveedores → productos, porque los
///     productos referencian a las otras dos.
/// </summary>
public class BackupService(
    IProductoRepository productoRepository,
    ICategoriaRepository categoriaRepository,
    IProveedorRepository proveedorRepository,
    IValidator<ProductoRequestDto> productoValidator,
    IValidator<CategoriaRequestDto> categoriaValidator,
    IValidator<ProveedorRequestDto> proveedorValidator,
    ICacheService cacheService,
    IOutputCacheStore outputCacheStore,
    ILogger<BackupService> logger
) : IBackupService {
    /// <summary>Límite de tamaño del fichero subido (coincide con el RequestSizeLimit).</summary>
    public const int MaxBytesFichero = 10 * 1024 * 1024;

    /// <summary>Nº máximo de entidades de un ZIP.</summary>
    private const int MaxEntradasZip = ZipCodec.MaxEntradas;

    /// <summary>Tamaño máximo descomprimido.</summary>
    private const int MaxDescomprimido = ZipCodec.MaxBytesDescomprimidos;

    /// <summary>Formatos admitidos en export e import.</summary>
    public static readonly string[] Formatos = ["zip", "json", "csv"];

    /// <summary>Entidades exportables, en el orden en que se importan.</summary>
    public static readonly string[] Entidades = ["categorias", "proveedores", "productos"];

    /// <summary>
    ///     Opciones de JSON del backup en camelCase, como el resto de respuestas de la API.
    ///     Se comparten con los tests para que el round-trip lea lo mismo que escribe:
    ///     el contexto source-gen por defecto es PascalCase y el fichero no casaría.
    /// </summary>
    public static readonly JsonSerializerOptions OpcionesJson = new(AppJsonContext.Default.Options) {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public (string ContentType, string NombreFichero) DatosSalida(string formato, IReadOnlyList<string> entidades) {
        string fecha = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        string sufijo = entidades.Count == 1 ? entidades[0] : "backup";

        return formato switch {
            "json" => ("application/json; charset=utf-8", $"tienda-{sufijo}-{fecha}.json"),
            // El csv necesita una entidad por fichero: con más de una se empaqueta.
            "csv" => entidades.Count == 1
                ? ("text/csv; charset=utf-8", $"tienda-{sufijo}-{fecha}.csv")
                : ("application/zip", $"tienda-{sufijo}-{fecha}.zip"),
            _ => ("application/zip", $"tienda-{sufijo}-{fecha}.zip")
        };
    }

    /// <inheritdoc />
    public async Task<UnitResult<DomainError>> ExportarAsync(
        Stream salida, string formato, IReadOnlyList<string> entidades, bool incluirBorrados) {
        UnitResult<DomainError> validacion = ValidarPeticion(formato, entidades);
        if (validacion.IsFailure) {
            return UnitResult.Failure<DomainError>(validacion.Error);
        }

        // Se carga TODO antes de escribir: si algo falla, la respuesta aún no se ha tocado.
        DatosBackup datos = await CargarAsync(entidades, incluirBorrados);

        switch (formato) {
            case "json":
                await EscribirJsonAsync(salida, datos, entidades, incluirBorrados);
                break;
            case "csv":
                if (entidades.Count == 1) {
                    await EscribirCsvAsync(salida, entidades[0], datos);
                } else {
                    await EscribirZipAsync(salida, datos, incluirBorrados, true);
                }

                break;
            default:
                await EscribirZipAsync(salida, datos, incluirBorrados, false);
                break;
        }

        logger.LogInformation(
            "Backup exportado en formato {Formato}: {Categorias} categorías, {Productos} productos, {Proveedores} proveedores",
            formato, datos.Categorias.Count, datos.Productos.Count, datos.Proveedores.Count);

        return UnitResult.Success<DomainError>();
    }

    /// <inheritdoc />
    public async Task<Result<ImportReportDto, DomainError>> ImportarAsync(
        Stream fichero, string formato, IReadOnlyList<string> entidades, bool dryRun) {
        string[] formatos = string.IsNullOrWhiteSpace(formato) ? Formatos : [formato.Trim().ToLowerInvariant()];
        if (formatos.Length != 1 || !Formatos.Contains(formatos[0])) {
            return Result.Failure<ImportReportDto, DomainError>(BackupError.FormatoDesconocido(formato));
        }

        UnitResult<DomainError> validacion = ValidarPeticion(formatos[0], entidades);
        if (validacion.IsFailure) {
            return Result.Failure<ImportReportDto, DomainError>(validacion.Error);
        }

        var reloj = Stopwatch.StartNew();

        BackupDto? backup;
        try {
            backup = await LeerAsync(fichero, formatos[0], entidades);
        } catch (BackupException ex) {
            return Result.Failure<ImportReportDto, DomainError>(ex.Error);
        } catch (JsonException ex) {
            return Result.Failure<ImportReportDto, DomainError>(
                BackupError.FormatoInvalido($"JSON inválido ({ex.Message})"));
        } catch (InvalidDataException ex) {
            return Result.Failure<ImportReportDto, DomainError>(
                BackupError.FormatoInvalido(ex.Message));
        }

        if (backup is null) {
            return Result.Failure<ImportReportDto, DomainError>(
                BackupError.FormatoInvalido("el fichero está vacío o no contiene ninguna entidad pedida"));
        }

        var informes = new List<EntidadImportReportDto>();

        // Orden: primero las referencias, después quien las referencia.
        if (entidades.Contains("categorias") && backup.Categorias is not null) {
            informes.Add(await ImportarCategoriasAsync(backup.Categorias, dryRun));
        }

        if (entidades.Contains("proveedores") && backup.Proveedores is not null) {
            informes.Add(await ImportarProveedoresAsync(backup.Proveedores, dryRun));
        }

        if (entidades.Contains("productos") && backup.Productos is not null) {
            informes.Add(await ImportarProductosAsync(backup.Productos, dryRun));
        }

        if (!dryRun) {
            // Altas con ID explícito: la secuencia debe quedar por detrás del mayor
            // ID existente, o la siguiente creación chocaría por clave duplicada.
            if (informes.Any(i => i.Entidad == "categorias" && i.Creados > 0)) {
                await categoriaRepository.ResincronizarSecuenciaAsync();
            }

            if (informes.Any(i => i.Entidad == "productos" && i.Creados > 0)) {
                await productoRepository.ResincronizarSecuenciaAsync();
            }

            await InvalidarCacheAsync();
        }

        reloj.Stop();

        bool completo = informes.All(i => i.Errores.Count == 0);
        var informe = new ImportReportDto(dryRun, formatos[0], completo, reloj.ElapsedMilliseconds, informes);

        logger.LogInformation(
            "Backup importado ({Formato}, dryRun={DryRun}): completo={Completo} en {Ms} ms",
            formatos[0], dryRun, completo, informe.DuracionMs);

        return Result.Success<ImportReportDto, DomainError>(informe);
    }

    /// <inheritdoc />
    public Task<Result<CatalogoFormatosDto, DomainError>> ObtenerFormatosAsync() {
        var catalogo = new CatalogoFormatosDto(
            new List<FormatoDto> {
                new("zip", "application/zip", ".zip", "Manifest más un fichero por entidad (json o csv)"),
                new("json", "application/json; charset=utf-8", ".json", "Un único documento con manifest y entidades"),
                new("csv", "text/csv; charset=utf-8", ".csv",
                    "Una entidad por fichero (con más de una se empaqueta en zip)")
            },
            new List<EntidadCatalogoDto> {
                new("categorias", "id", ["id", "nombre", "descripcion", "isDeleted", "createdAt", "updatedAt"]),
                new("proveedores", "codigo",
                    ["codigo", "nombre", "descripcion", "contacto", "isDeleted", "createdAt", "updatedAt"]),
                new("productos", "id",
                [
                    "id", "nombre", "descripcion", "precio", "stock", "imagen", "categoriaId", "proveedorId",
                    "isDeleted", "createdAt", "updatedAt"
                ])
            },
            new LimitesBackupDto(MaxBytesFichero, MaxDescomprimido, MaxEntradasZip),
            new ReglasBackupDto(
                "por clave: id en categorías y productos, codigo en proveedores",
                "dryRun=true valida sin escribir nada",
                "categorias → proveedores → productos"));

        return Task.FromResult(Result.Success<CatalogoFormatosDto, DomainError>(catalogo));
    }

    // ────────────────────────────────────────────────────────────
    // Export
    // ────────────────────────────────────────────────────────────

    /// <summary>Carga las entidades pedidas (activas y, si se pide, también las borradas).</summary>
    private async Task<DatosBackup> CargarAsync(IReadOnlyList<string> entidades, bool incluirBorrados) {
        var datos = new DatosBackup();

        if (entidades.Contains("categorias")) {
            datos.Categorias = await CargarCategoriasAsync(incluirBorrados);
        }

        if (entidades.Contains("proveedores")) {
            datos.Proveedores = await CargarProveedoresAsync(incluirBorrados);
        }

        if (entidades.Contains("productos")) {
            datos.Productos = await CargarProductosAsync(incluirBorrados);
        }

        return datos;
    }

    private async Task<List<BackupCategoriaDto>> CargarCategoriasAsync(bool incluirBorrados) {
        var activas = (await categoriaRepository.FindAllAsync()).ToList();
        if (!incluirBorrados) {
            return activas.Select(Mapear).ToList();
        }

        List<Categoria> borradas = await PaginarAsync(page => categoriaRepository.FindAllPagedAsync(
            new CategoriaFilterDto {
                IsDeleted = true,
                Page = page,
                Size = 100,
                SortBy = "id",
                Direction = "asc"
            }));

        return activas.Concat(borradas).Select(Mapear).ToList();
    }

    private async Task<List<BackupProveedorDto>> CargarProveedoresAsync(bool incluirBorrados) {
        var activos = (await proveedorRepository.FindAllAsync()).ToList();
        if (!incluirBorrados) {
            return activos.Select(Mapear).ToList();
        }

        List<Proveedor> borrados = await PaginarAsync(page => proveedorRepository.FindAllPagedAsync(
            new ProveedorFilterDto {
                IsDeleted = true,
                Page = page,
                Size = 100,
                SortBy = "codigo",
                Direction = "asc"
            }));

        return activos.Concat(borrados).Select(Mapear).ToList();
    }

    private async Task<List<BackupProductoDto>> CargarProductosAsync(bool incluirBorrados) {
        var activos = (await productoRepository.FindAllAsync()).ToList();
        if (!incluirBorrados) {
            return activos.Select(Mapear).ToList();
        }

        List<Producto> borrados = await PaginarAsync(page => productoRepository.FindAllPagedAsync(new ProductoFilterDto(
            null,
            null,
            true,
            null,
            null,
            page,
            100)));

        return activos.Concat(borrados).Select(Mapear).ToList();
    }

    /// <summary>Recorre un listado paginado hasta agotarlo (para incluir borrados).</summary>
    private static async Task<List<T>>
        PaginarAsync<T>(Func<int, Task<(IEnumerable<T> Items, int TotalCount)>> consulta) {
        var salida = new List<T>();
        int pagina = 0;

        while (true) {
            (IEnumerable<T> items, int total) = await consulta(pagina);
            salida.AddRange(items);
            pagina++;
            if (salida.Count >= total || !items.Any()) {
                break;
            }
        }

        return salida;
    }

    private static async Task EscribirJsonAsync(Stream salida, DatosBackup datos, IReadOnlyList<string> entidades,
        bool incluirBorrados) {
        // Las entidades no pedidas salen en null (documentado en el DTO): así el
        // cliente distingue "no lo pedí" de "no hay nada".
        var backup = new BackupDto(CrearManifest("json", datos, incluirBorrados),
            entidades.Contains("categorias") ? datos.Categorias : null,
            entidades.Contains("productos") ? datos.Productos : null,
            entidades.Contains("proveedores") ? datos.Proveedores : null);

        await JsonSerializer.SerializeAsync(salida, backup, OpcionesJson);
        await salida.FlushAsync();
    }

    private static async Task EscribirCsvAsync(Stream salida, string entidad, DatosBackup datos) {
        using StreamWriter escritor = CsvCodec.CrearEscritor(salida);
        switch (entidad) {
            case "categorias":
                CsvCodec.EscribirCabecera(escritor,
                    ["id", "nombre", "descripcion", "isDeleted", "createdAt", "updatedAt"]);
                foreach (BackupCategoriaDto c in datos.Categorias) {
                    CsvCodec.EscribirFila(escritor,
                    [
                        c.Id.ToString(CultureInfo.InvariantCulture), c.Nombre, c.Descripcion,
                        c.IsDeleted ? "true" : "false",
                        CsvCodec.Formatear(c.CreatedAt), CsvCodec.Formatear(c.UpdatedAt)
                    ]);
                }

                break;
            case "proveedores":
                CsvCodec.EscribirCabecera(escritor,
                    ["codigo", "nombre", "descripcion", "contacto", "isDeleted", "createdAt", "updatedAt"]);
                foreach (BackupProveedorDto p in datos.Proveedores) {
                    CsvCodec.EscribirFila(escritor,
                    [
                        p.Codigo, p.Nombre, p.Descripcion, p.Contacto, p.IsDeleted ? "true" : "false",
                        CsvCodec.Formatear(p.CreatedAt), CsvCodec.Formatear(p.UpdatedAt)
                    ]);
                }

                break;
            default:
                CsvCodec.EscribirCabecera(escritor,
                [
                    "id", "nombre", "descripcion", "precio", "stock", "imagen", "categoriaId", "proveedorId",
                    "isDeleted", "createdAt", "updatedAt"
                ]);
                foreach (BackupProductoDto p in datos.Productos) {
                    CsvCodec.EscribirFila(escritor,
                    [
                        p.Id.ToString(CultureInfo.InvariantCulture), p.Nombre, p.Descripcion,
                        CsvCodec.Formatear(p.Precio), p.Stock.ToString(CultureInfo.InvariantCulture), p.Imagen,
                        p.CategoriaId.ToString(CultureInfo.InvariantCulture), p.ProveedorId,
                        p.IsDeleted ? "true" : "false", CsvCodec.Formatear(p.CreatedAt), CsvCodec.Formatear(p.UpdatedAt)
                    ]);
                }

                break;
        }

        await escritor.FlushAsync();
    }

    private static async Task EscribirZipAsync(Stream salida, DatosBackup datos, bool incluirBorrados, bool csv) {
        using ZipArchive archivo = ZipCodec.Crear(salida);

        // Manifest siempre como primer fichero.
        ManifestBackupDto manifest = CrearManifest(csv ? "csv" : "zip", datos, incluirBorrados);
        await EscribirEntradaAsync(archivo, "manifest.json",
            JsonSerializer.Serialize(manifest, OpcionesJson));

        if (csv) {
            if (datos.Categorias.Count > 0) {
                await EscribirEntradaAsync(archivo, "categorias.csv", SerializarCsv("categorias", datos));
            }

            if (datos.Proveedores.Count > 0) {
                await EscribirEntradaAsync(archivo, "proveedores.csv", SerializarCsv("proveedores", datos));
            }

            if (datos.Productos.Count > 0) {
                await EscribirEntradaAsync(archivo, "productos.csv", SerializarCsv("productos", datos));
            }
        } else {
            if (datos.Categorias.Count > 0) {
                await EscribirEntradaAsync(archivo, "categorias.json",
                    JsonSerializer.Serialize(datos.Categorias, OpcionesJson));
            }

            if (datos.Proveedores.Count > 0) {
                await EscribirEntradaAsync(archivo, "proveedores.json",
                    JsonSerializer.Serialize(datos.Proveedores, OpcionesJson));
            }

            if (datos.Productos.Count > 0) {
                await EscribirEntradaAsync(archivo, "productos.json",
                    JsonSerializer.Serialize(datos.Productos, OpcionesJson));
            }
        }

        await archivo.DisposeAsync();
        await salida.FlushAsync();
    }

    private static async Task EscribirEntradaAsync(ZipArchive archivo, string nombre, string contenido) {
        ZipArchiveEntry entrada = archivo.CreateEntry(nombre, CompressionLevel.Optimal);
        await using Stream flujo = entrada.Open();
        await using var escritor = new StreamWriter(flujo, new UTF8Encoding(false));
        await escritor.WriteAsync(contenido);
    }

    /// <summary>Serializa una entidad a CSV (para incluirlo dentro de un ZIP).</summary>
    private static string SerializarCsv(string entidad, DatosBackup datos) {
        using var texto = new StringWriter();
        switch (entidad) {
            case "categorias":
                CsvCodec.EscribirCabecera(texto,
                    ["id", "nombre", "descripcion", "isDeleted", "createdAt", "updatedAt"]);
                foreach (BackupCategoriaDto c in datos.Categorias) {
                    CsvCodec.EscribirFila(texto,
                    [
                        c.Id.ToString(CultureInfo.InvariantCulture), c.Nombre, c.Descripcion,
                        c.IsDeleted ? "true" : "false",
                        CsvCodec.Formatear(c.CreatedAt), CsvCodec.Formatear(c.UpdatedAt)
                    ]);
                }

                break;
            case "proveedores":
                CsvCodec.EscribirCabecera(texto,
                    ["codigo", "nombre", "descripcion", "contacto", "isDeleted", "createdAt", "updatedAt"]);
                foreach (BackupProveedorDto p in datos.Proveedores) {
                    CsvCodec.EscribirFila(texto,
                    [
                        p.Codigo, p.Nombre, p.Descripcion, p.Contacto, p.IsDeleted ? "true" : "false",
                        CsvCodec.Formatear(p.CreatedAt), CsvCodec.Formatear(p.UpdatedAt)
                    ]);
                }

                break;
            default:
                CsvCodec.EscribirCabecera(texto,
                [
                    "id", "nombre", "descripcion", "precio", "stock", "imagen", "categoriaId", "proveedorId",
                    "isDeleted", "createdAt", "updatedAt"
                ]);
                foreach (BackupProductoDto p in datos.Productos) {
                    CsvCodec.EscribirFila(texto,
                    [
                        p.Id.ToString(CultureInfo.InvariantCulture), p.Nombre, p.Descripcion,
                        CsvCodec.Formatear(p.Precio), p.Stock.ToString(CultureInfo.InvariantCulture), p.Imagen,
                        p.CategoriaId.ToString(CultureInfo.InvariantCulture), p.ProveedorId,
                        p.IsDeleted ? "true" : "false", CsvCodec.Formatear(p.CreatedAt), CsvCodec.Formatear(p.UpdatedAt)
                    ]);
                }

                break;
        }

        return texto.ToString();
    }

    private static ManifestBackupDto CrearManifest(string formato, DatosBackup datos, bool incluirBorrados) {
        return new ManifestBackupDto(
            "1.0",
            DateTime.UtcNow,
            new Dictionary<string, int> {
                ["categorias"] = datos.Categorias.Count,
                ["proveedores"] = datos.Proveedores.Count,
                ["productos"] = datos.Productos.Count
            },
            incluirBorrados,
            formato);
    }

    // ────────────────────────────────────────────────────────────
    // Import
    // ────────────────────────────────────────────────────────────

    /// <summary>Valida formato y entidades antes de tocar el fichero.</summary>
    private static UnitResult<DomainError> ValidarPeticion(string formato, IReadOnlyList<string> entidades) {
        if (!Formatos.Contains(formato)) {
            return UnitResult.Failure<DomainError>(BackupError.FormatoDesconocido(formato));
        }

        if (entidades.Count == 0) {
            return UnitResult.Failure<DomainError>(BackupError.EntidadDesconocida("(ninguna)"));
        }

        foreach (string entidad in entidades) {
            if (!Entidades.Contains(entidad)) {
                return UnitResult.Failure<DomainError>(BackupError.EntidadDesconocida(entidad));
            }
        }

        return UnitResult.Success<DomainError>();
    }

    /// <summary>
    ///     Lee el fichero según el formato y devuelve solo las entidades pedidas.
    ///     El ZIP pasa por stream (binario); el json y el csv se leen como texto.
    /// </summary>
    private static async Task<BackupDto?> LeerAsync(Stream fichero, string formato, IReadOnlyList<string> entidades) {
        return formato switch {
            "zip" => LeerZip(fichero, entidades),
            "json" => JsonSerializer.Deserialize<BackupDto>(
                await LeerTextoAsync(fichero), OpcionesJson),
            _ => LeerCsv(await LeerTextoAsync(fichero), entidades)
        };
    }

    private static async Task<string> LeerTextoAsync(Stream fichero) {
        using var lector = new StreamReader(fichero, Encoding.UTF8, leaveOpen: true);
        string texto = await lector.ReadToEndAsync();
        if (texto.Length > MaxBytesFichero) {
            throw new BackupException(
                BackupError.DemasiadoGrande($"{texto.Length} caracteres, máximo {MaxBytesFichero}"));
        }

        return texto;
    }

    /// <summary>
    ///     Construye el backup desde las entradas de un ZIP (límites aplicados por el codec).
    ///     Se aceptan ficheros .json y .csv: el nombre del fichero dice qué entidad contiene.
    /// </summary>
    private static BackupDto? LeerZip(Stream fichero, IReadOnlyList<string> entidades) {
        IReadOnlyList<ZipCodec.EntradaZip> entradas = ZipCodec.Leer(fichero);
        var datos = new DatosBackup();

        foreach (ZipCodec.EntradaZip entrada in entradas) {
            string nombre = Path.GetFileName(entrada.Nombre).ToLowerInvariant();

            if (nombre == "categorias.json") {
                datos.Categorias = JsonSerializer.Deserialize<List<BackupCategoriaDto>>(
                    entrada.Contenido, OpcionesJson) ?? [];
            } else if (nombre == "proveedores.json") {
                datos.Proveedores = JsonSerializer.Deserialize<List<BackupProveedorDto>>(
                    entrada.Contenido, OpcionesJson) ?? [];
            } else if (nombre == "productos.json") {
                datos.Productos = JsonSerializer.Deserialize<List<BackupProductoDto>>(
                    entrada.Contenido, OpcionesJson) ?? [];
            } else if (nombre == "categorias.csv") {
                datos.Categorias = MapearCategoriasCsv(entrada.Contenido);
            } else if (nombre == "proveedores.csv") {
                datos.Proveedores = MapearProveedoresCsv(entrada.Contenido);
            } else if (nombre == "productos.csv") {
                datos.Productos = MapearProductosCsv(entrada.Contenido);
            }
        }

        if (datos.Categorias.Count == 0 && datos.Proveedores.Count == 0 && datos.Productos.Count == 0) {
            return null;
        }

        return new BackupDto(CrearManifest("zip", datos, false),
            datos.Categorias, datos.Productos, datos.Proveedores);
    }

    /// <summary>Convierte el contenido de un CSV en la lista de su entidad.</summary>
    private static List<BackupCategoriaDto> MapearCategoriasCsv(string contenido) {
        (string[] cabecera, string[][] cuerpo) filas = LeerFilas(contenido);
        return filas.cuerpo.Select(f => MapaCategoriaCsv(filas.cabecera, f)).ToList();
    }

    private static List<BackupProveedorDto> MapearProveedoresCsv(string contenido) {
        (string[] cabecera, string[][] cuerpo) filas = LeerFilas(contenido);
        return filas.cuerpo.Select(f => MapaProveedorCsv(filas.cabecera, f)).ToList();
    }

    private static List<BackupProductoDto> MapearProductosCsv(string contenido) {
        (string[] cabecera, string[][] cuerpo) filas = LeerFilas(contenido);
        return filas.cuerpo.Select(f => MapaProductoCsv(filas.cabecera, f)).ToList();
    }

    /// <summary>Lee un CSV devolviendo la cabecera y las filas de datos.</summary>
    private static (string[] cabecera, string[][] cuerpo) LeerFilas(string contenido) {
        using var lector = new StringReader(contenido);
        List<string[]> todas = CsvCodec.Leer(lector);
        if (todas.Count < 2) {
            return ([], []);
        }

        return (todas[0], todas.Skip(1).ToArray());
    }

    /// <summary>
    ///     Construye el backup desde un CSV de una sola entidad (la indica `entidades`).
    /// </summary>
    private static BackupDto? LeerCsv(string contenido, IReadOnlyList<string> entidades) {
        using var lector = new StringReader(contenido);
        List<string[]> filas = CsvCodec.Leer(lector);
        if (filas.Count < 2) {
            return null;
        }

        string entidad = entidades[0];
        string[] cabecera = filas[0];
        var datos = new DatosBackup();
        string[][] cuerpo = filas.Skip(1).ToArray();

        switch (entidad) {
            case "categorias":
                datos.Categorias = cuerpo.Select(f => MapaCategoriaCsv(cabecera, f)).ToList();
                break;
            case "proveedores":
                datos.Proveedores = cuerpo.Select(f => MapaProveedorCsv(cabecera, f)).ToList();
                break;
            default:
                datos.Productos = cuerpo.Select(f => MapaProductoCsv(cabecera, f)).ToList();
                break;
        }

        return new BackupDto(CrearManifest("csv", datos, false),
            datos.Categorias, datos.Productos, datos.Proveedores);
    }

    // Mapeo CSV por nombre de columna: el orden de las columnas no importa.
    private static BackupCategoriaDto MapaCategoriaCsv(string[] cabecera, string[] fila) {
        return new BackupCategoriaDto(
            AId(Campo(cabecera, fila, "id")),
            Campo(cabecera, fila, "nombre"),
            NuloSiVacio(Campo(cabecera, fila, "descripcion")),
            CsvCodec.ABooleano(Campo(cabecera, fila, "isDeleted")),
            CsvCodec.AFecha(Campo(cabecera, fila, "createdAt")) ?? DateTime.UtcNow,
            CsvCodec.AFecha(Campo(cabecera, fila, "updatedAt")) ?? DateTime.UtcNow);
    }

    private static BackupProveedorDto MapaProveedorCsv(string[] cabecera, string[] fila) {
        return new BackupProveedorDto(
            Campo(cabecera, fila, "codigo"),
            Campo(cabecera, fila, "nombre"),
            NuloSiVacio(Campo(cabecera, fila, "descripcion")),
            NuloSiVacio(Campo(cabecera, fila, "contacto")),
            CsvCodec.ABooleano(Campo(cabecera, fila, "isDeleted")),
            CsvCodec.AFecha(Campo(cabecera, fila, "createdAt")) ?? DateTime.UtcNow,
            CsvCodec.AFecha(Campo(cabecera, fila, "updatedAt")) ?? DateTime.UtcNow);
    }

    private static BackupProductoDto MapaProductoCsv(string[] cabecera, string[] fila) {
        return new BackupProductoDto(
            AId(Campo(cabecera, fila, "id")),
            Campo(cabecera, fila, "nombre"),
            Campo(cabecera, fila, "descripcion"),
            CsvCodec.ADecimal(Campo(cabecera, fila, "precio")) ?? 0m,
            CsvCodec.AEntero(Campo(cabecera, fila, "stock")) ?? 0,
            NuloSiVacio(Campo(cabecera, fila, "imagen")),
            CsvCodec.AEnteroLargo(Campo(cabecera, fila, "categoriaId")) ?? 0,
            NuloSiVacio(Campo(cabecera, fila, "proveedorId")),
            CsvCodec.ABooleano(Campo(cabecera, fila, "isDeleted")),
            CsvCodec.AFecha(Campo(cabecera, fila, "createdAt")) ?? DateTime.UtcNow,
            CsvCodec.AFecha(Campo(cabecera, fila, "updatedAt")) ?? DateTime.UtcNow);
    }

    /// <summary>
    ///     Una celda vacía en el CSV significa "sin valor": en base de datos eso es
    ///     NULL, no una cadena vacía (si no, el round-trip marcaría todo como cambiado).
    /// </summary>
    private static string? NuloSiVacio(string valor) {
        return string.IsNullOrWhiteSpace(valor) ? null : valor;
    }

    private static string Campo(string[] cabecera, string[] fila, string nombre) {
        int indice = Array.FindIndex(cabecera, c => c.Trim().Equals(nombre, StringComparison.OrdinalIgnoreCase));
        return indice >= 0 && indice < fila.Length ? fila[indice] : string.Empty;
    }

    private static long AId(string texto) {
        return CsvCodec.AEnteroLargo(texto) ??
               throw new BackupException(BackupError.FormatoInvalido($"id no numérico '{texto}'"));
    }

    /// <summary>Importa categorías con upsert por id.</summary>
    private async Task<EntidadImportReportDto> ImportarCategoriasAsync(
        IReadOnlyList<BackupCategoriaDto> filas, bool dryRun) {
        var errores = new List<ErrorFilaDto>();
        int creados = 0;
        int actualizados = 0;
        int sinCambios = 0;

        for (int i = 0; i < filas.Count; i++) {
            BackupCategoriaDto fila = filas[i];
            int numeroFila = i + 1;

            ValidationResult validacion = await categoriaValidator.ValidateAsync(new CategoriaRequestDto {
                Nombre = fila.Nombre,
                Descripcion = fila.Descripcion
            });

            if (!validacion.IsValid) {
                errores.Add(new ErrorFilaDto(numeroFila, "nombre", Mensaje(validacion)));
                continue;
            }

            Categoria? existente = await categoriaRepository.FindByIdAsync(fila.Id);
            if (existente is null) {
                creados++;
                if (!dryRun) {
                    await CrearConIdExplicitoAsync(fila);
                }

                continue;
            }

            if (existente.Nombre == fila.Nombre && existente.Descripcion == fila.Descripcion
                                                && existente.IsDeleted == fila.IsDeleted) {
                sinCambios++;
                continue;
            }

            existente.Nombre = fila.Nombre;
            existente.Descripcion = fila.Descripcion;
            existente.IsDeleted = fila.IsDeleted;
            actualizados++;
            if (!dryRun) {
                await categoriaRepository.UpdateAsync(existente);
            }
        }

        return new EntidadImportReportDto("categorias", filas.Count, creados, actualizados, sinCambios, errores);
    }

    /// <summary>Importa proveedores con upsert por código (clave de negocio, sin identidad).</summary>
    private async Task<EntidadImportReportDto> ImportarProveedoresAsync(
        IReadOnlyList<BackupProveedorDto> filas, bool dryRun) {
        var errores = new List<ErrorFilaDto>();
        int creados = 0;
        int actualizados = 0;
        int sinCambios = 0;

        for (int i = 0; i < filas.Count; i++) {
            BackupProveedorDto fila = filas[i];
            int numeroFila = i + 1;

            ValidationResult validacion = await proveedorValidator.ValidateAsync(new ProveedorRequestDto {
                Codigo = fila.Codigo,
                Nombre = fila.Nombre,
                Descripcion = fila.Descripcion,
                Contacto = fila.Contacto
            });

            if (!validacion.IsValid) {
                errores.Add(new ErrorFilaDto(numeroFila, "codigo", Mensaje(validacion)));
                continue;
            }

            Proveedor? existente = await proveedorRepository.FindByIdAsync(fila.Codigo);
            if (existente is null) {
                creados++;
                if (!dryRun) {
                    await proveedorRepository.CreateAsync(new Proveedor {
                        Codigo = fila.Codigo,
                        Nombre = fila.Nombre,
                        Descripcion = fila.Descripcion,
                        Contacto = fila.Contacto,
                        IsDeleted = fila.IsDeleted,
                        CreatedAt = fila.CreatedAt,
                        UpdatedAt = fila.UpdatedAt
                    });
                }

                continue;
            }

            if (existente.Nombre == fila.Nombre && existente.Descripcion == fila.Descripcion
                                                && existente.Contacto == fila.Contacto &&
                                                existente.IsDeleted == fila.IsDeleted) {
                sinCambios++;
                continue;
            }

            existente.Nombre = fila.Nombre;
            existente.Descripcion = fila.Descripcion;
            existente.Contacto = fila.Contacto;
            existente.IsDeleted = fila.IsDeleted;
            actualizados++;
            if (!dryRun) {
                await proveedorRepository.UpdateAsync(existente);
            }
        }

        return new EntidadImportReportDto("proveedores", filas.Count, creados, actualizados, sinCambios, errores);
    }

    /// <summary>
    ///     Importa productos con upsert por id, validando fila a fila: formato,
    ///     existencia de la categoría y del proveedor (los errores no abortan).
    /// </summary>
    private async Task<EntidadImportReportDto> ImportarProductosAsync(
        IReadOnlyList<BackupProductoDto> filas, bool dryRun) {
        var errores = new List<ErrorFilaDto>();
        int creados = 0;
        int actualizados = 0;
        int sinCambios = 0;

        for (int i = 0; i < filas.Count; i++) {
            BackupProductoDto fila = filas[i];
            int numeroFila = i + 1;

            ValidationResult validacion = await productoValidator.ValidateAsync(new ProductoRequestDto {
                Nombre = fila.Nombre,
                Descripcion = fila.Descripcion,
                Precio = fila.Precio,
                Stock = fila.Stock,
                Imagen = fila.Imagen,
                CategoriaId = fila.CategoriaId,
                ProveedorCodigo = fila.ProveedorId
            });

            if (!validacion.IsValid) {
                errores.Add(new ErrorFilaDto(numeroFila, "producto", Mensaje(validacion)));
                continue;
            }

            if (!await categoriaRepository.ExistsByIdAsync(fila.CategoriaId)) {
                errores.Add(new ErrorFilaDto(numeroFila, "categoriaId",
                    $"La categoría con ID {fila.CategoriaId} no existe"));
                continue;
            }

            if (fila.ProveedorId is not null && !await proveedorRepository.ExistsByIdAsync(fila.ProveedorId)) {
                errores.Add(new ErrorFilaDto(numeroFila, "proveedorId",
                    $"El proveedor con código {fila.ProveedorId} no existe"));
                continue;
            }

            Producto? existente = await productoRepository.FindByIdAsync(fila.Id);
            if (existente is null) {
                creados++;
                if (!dryRun) {
                    await CrearProductoConIdExplicitoAsync(fila);
                }

                continue;
            }

            if (existente.Nombre == fila.Nombre && existente.Descripcion == fila.Descripcion
                                                && existente.Precio == fila.Precio && existente.Stock == fila.Stock
                                                && existente.CategoriaId == fila.CategoriaId &&
                                                existente.ProveedorId == fila.ProveedorId
                                                && existente.IsDeleted == fila.IsDeleted) {
                sinCambios++;
                continue;
            }

            existente.Nombre = fila.Nombre;
            existente.Descripcion = fila.Descripcion;
            existente.Precio = fila.Precio;
            existente.Stock = fila.Stock;
            existente.Imagen = fila.Imagen;
            existente.CategoriaId = fila.CategoriaId;
            existente.ProveedorId = fila.ProveedorId;
            existente.IsDeleted = fila.IsDeleted;
            actualizados++;
            if (!dryRun) {
                await productoRepository.UpdateAsync(existente);
            }
        }

        return new EntidadImportReportDto("productos", filas.Count, creados, actualizados, sinCambios, errores);
    }

    // ────────────────────────────────────────────────────────────
    // Altas con ID explícito y re-sincronización de secuencias
    // ────────────────────────────────────────────────────────────

    /// <summary>
    ///     Inserta una categoría con su ID original. Las columnas de identidad son
    ///     <c>GENERATED ALWAYS</c> y rechazan valores explícitos, así que se inserta
    ///     con <c>OVERRIDING SYSTEM VALUE</c> y después se re-sincroniza la secuencia.
    /// </summary>
    private async Task CrearConIdExplicitoAsync(BackupCategoriaDto fila) {
        await categoriaRepository.CrearConIdExplicitoAsync(new Categoria {
            Id = fila.Id,
            Nombre = fila.Nombre,
            Descripcion = fila.Descripcion,
            IsDeleted = fila.IsDeleted,
            CreatedAt = Utc(fila.CreatedAt),
            UpdatedAt = Utc(fila.UpdatedAt)
        });
    }

    private async Task CrearProductoConIdExplicitoAsync(BackupProductoDto fila) {
        await productoRepository.CrearConIdExplicitoAsync(new Producto {
            Id = fila.Id,
            Nombre = fila.Nombre,
            Descripcion = fila.Descripcion,
            Precio = fila.Precio,
            Stock = fila.Stock,
            Imagen = fila.Imagen,
            CategoriaId = fila.CategoriaId,
            ProveedorId = fila.ProveedorId,
            IsDeleted = fila.IsDeleted,
            CreatedAt = Utc(fila.CreatedAt),
            UpdatedAt = Utc(fila.UpdatedAt),
            RowVersion = new byte[8]
        });
    }

    /// <summary>Npgsql exige Kind=Utc para timestamp with time zone.</summary>
    private static DateTime Utc(DateTime valor) {
        return valor.Kind == DateTimeKind.Utc ? valor : DateTime.SpecifyKind(valor, DateTimeKind.Utc);
    }

    /// <summary>
    ///     Invalida las cachés que cuelgan de productos, categorías y proveedores.
    ///     Las claves por id (productos:{id}) no se enumeran: caducan solas con su
    ///     TTL; aquí se limpian las de listado y la caché HTTP por tag.
    /// </summary>
    private async Task InvalidarCacheAsync() {
        try {
            await cacheService.RemoveAsync("categorias:all");
            await cacheService.RemoveAsync("proveedores:all");
            await outputCacheStore.EvictByTagAsync("productos", CancellationToken.None);
            await outputCacheStore.EvictByTagAsync("categorias", CancellationToken.None);
            await outputCacheStore.EvictByTagAsync("proveedores", CancellationToken.None);
        } catch (Exception ex) {
            logger.LogWarning(ex, "No se pudieron invalidar las cachés tras el import");
        }
    }

    private static string Mensaje(ValidationResult validacion) {
        return string.Join("; ", validacion.Errors.Select(e => e.ErrorMessage));
    }

    // ────────────────────────────────────────────────────────────
    // Mapeos a DTO de backup
    // ────────────────────────────────────────────────────────────

    private static BackupCategoriaDto Mapear(Categoria c) {
        return new BackupCategoriaDto(c.Id, c.Nombre, c.Descripcion, c.IsDeleted, c.CreatedAt, c.UpdatedAt);
    }

    private static BackupProveedorDto Mapear(Proveedor p) {
        return new BackupProveedorDto(p.Codigo, p.Nombre, p.Descripcion, p.Contacto, p.IsDeleted, p.CreatedAt,
            p.UpdatedAt);
    }

    private static BackupProductoDto Mapear(Producto p) {
        return new BackupProductoDto(p.Id, p.Nombre, p.Descripcion, p.Precio, p.Stock, p.Imagen,
            p.CategoriaId, p.ProveedorId, p.IsDeleted, p.CreatedAt, p.UpdatedAt);
    }

    /// <summary>Datos ya cargados de las tres entidades para el export.</summary>
    private sealed class DatosBackup {
        public List<BackupCategoriaDto> Categorias { get; set; } = [];
        public List<BackupProveedorDto> Proveedores { get; set; } = [];
        public List<BackupProductoDto> Productos { get; set; } = [];
    }
}
