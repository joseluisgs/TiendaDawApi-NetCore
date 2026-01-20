using System.Runtime.CompilerServices;
using MongoDB.Driver;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using StackExchange.Redis;

namespace TiendaApi.Tests;

/// <summary>
/// Utilidades para detectar disponibilidad de servicios externos.
/// </summary>
public static class ServiceDetector
{
    /// <summary>
    /// Verifica si Docker está disponible.
    /// </summary>
    public static bool IsDockerAvailable()
    {
        try
        {
            var dockerPath = Environment.GetEnvironmentVariable("DOCKER_HOST");
            if (!string.IsNullOrEmpty(dockerPath))
                return true;

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "info",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Verifica si MongoDB está disponible.
    /// </summary>
    public static bool IsMongoDbAvailable()
    {
        try
        {
            var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION")
                ?? "mongodb://localhost:27017";

            var client = new MongoClient(connectionString);
            client.ListDatabaseNames().FirstOrDefaultAsync().Wait();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Verifica si Redis está disponible.
    /// </summary>
    public static bool IsRedisAvailable()
    {
        try
        {
            var connectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION")
                ?? "localhost:6379";

            using var connection = ConnectionMultiplexer.Connect(connectionString);
            return connection.IsConnected;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Omite el test si los servicios requeridos no están disponibles.
    /// </summary>
    public static void AssumeServicesAvailable([CallerMemberName] string? testName = null)
    {
        if (!IsDockerAvailable())
            Assert.Ignore($"Docker no disponible. Saltando test: {testName}");
    }

    /// <summary>
    /// Omite el test si MongoDB no está disponible.
    /// </summary>
    public static void AssumeMongoDbAvailable([CallerMemberName] string? testName = null)
    {
        if (!IsMongoDbAvailable())
            Assert.Ignore($"MongoDB no disponible. Saltando test: {testName}");
    }

    /// <summary>
    /// Omite el test si Redis no está disponible.
    /// </summary>
    public static void AssumeRedisAvailable([CallerMemberName] string? testName = null)
    {
        if (!IsRedisAvailable())
            Assert.Ignore($"Redis no disponible. Saltando test: {testName}");
    }
}
