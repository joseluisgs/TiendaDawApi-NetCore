namespace TiendaApi.Tests.Integration.TestContainers;

/// <summary>
/// Imágenes Docker fijadas para los tests con Testcontainers.
///
/// Deben ser EXACTAMENTE las mismas tags que usan docker-compose.local.yml
/// y docker-compose.prod.yml (postgres:17-alpine, mongo:7.0). Si cada vía usa
/// una tag distinta, Docker acumula entradas duplicadas en `docker images`
/// (p.ej. mongo:7 y mongo:7.0) y se descargan más bytes de los necesarios.
///
/// También evita las imágenes por defecto de Testcontainers (mongo:6.0 y
/// postgres:15.1 en Testcontainers 4.15.0), que no coinciden con las de
/// desarrollo ni con las de producción.
/// </summary>
public static class TestContainerImages
{
    /// <summary>Tag unificada con docker-compose.*.yml.</summary>
    public const string Postgres = "postgres:17-alpine";

    /// <summary>Tag unificada con docker-compose.*.yml.</summary>
    public const string Mongo = "mongo:7.0";
}
