using ClientBlazor.Cliente.Domain.Errors;
using ClientBlazor.Cliente.Services.GraphQL;
using ClientBlazor.Cliente.State.Auth;
using ClientBlazor.Cliente.State.Notifications;
using FluentAssertions;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ClientBlazor.Tests.Services;

/// <summary>
/// Pruebas unitarias para el servicio de comunicación GraphQL.
/// Objetivo: Validar el procesamiento de respuestas con errores de grafo y el manejo de seguridad.
/// </summary>
[TestFixture]
public class GraphQLServiceTests
{
    private Mock<IAuthStore> _authStoreMock = null!;
    private Mock<INotificationStore> _notificationStoreMock = null!;
    private GraphQLHttpClient _graphqlClient = null!;
    private GraphQLService _service = null!;
    private MockHttpMessageHandler _httpHandler = null!;

    /// <summary>
    /// Inicializa el cliente GraphQL con un handler de red simulado.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _authStoreMock = new Mock<IAuthStore>();
        _notificationStoreMock = new Mock<INotificationStore>();
        
        _httpHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(_httpHandler);
        
        var options = new GraphQLHttpClientOptions { EndPoint = new Uri("http://test.com/graphql") };
        _graphqlClient = new GraphQLHttpClient(options, new SystemTextJsonSerializer(), httpClient);
        
        _service = new GraphQLService(_graphqlClient, _authStoreMock.Object, _notificationStoreMock.Object);
    }

    /// <summary>
    /// Verifica que se devuelvan los datos correctamente cuando la respuesta de GraphQL es exitosa.
    /// </summary>
    [Test]
    public async Task ExecuteQueryAsync_Should_Return_Data_When_Successful()
    {
        // Arrange
        var responseObj = new { data = new { productos = new[] { new { id = 1, nombre = "Test" } } } };
        _httpHandler.ResponseContent = JsonSerializer.Serialize(responseObj);

        // Act
        var result = await _service.ExecuteQueryAsync<dynamic>("query { productos { id } }");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _notificationStoreMock.Verify(n => n.Error(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    /// <summary>
    /// Comprueba que el servicio detecte los errores internos de GraphQL (lista 'errors') y notifique al usuario.
    /// </summary>
    [Test]
    public async Task ExecuteQueryAsync_Should_Return_Failure_And_Notify_When_GraphQLErrors_Present()
    {
        // Arrange
        var responseObj = new { 
            errors = new[] { new { message = "Campo no existe" } },
            data = (object?)null 
        };
        _httpHandler.ResponseContent = JsonSerializer.Serialize(responseObj);

        // Act
        var result = await _service.ExecuteQueryAsync<dynamic>("query { error }");

        // Assert
        result.IsFailure.Should().BeTrue();
        _notificationStoreMock.Verify(n => n.Error("Campo no existe", It.IsAny<string>(), It.IsAny<int>()), Times.Once);
    }

    /// <summary>
    /// Valida que las mutaciones sean bloqueadas localmente si el usuario no ha iniciado sesión.
    /// </summary>
    [Test]
    public async Task ExecuteMutationAsync_Should_Return_Error_If_Not_Authenticated()
    {
        // Arrange
        _authStoreMock.Setup(s => s.GetState()).Returns(new AuthStore.AuthState { Token = null });

        // Act
        var result = await _service.ExecuteMutationAsync<dynamic>("mutation { test }");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AuthErrors.LoginRequired.Code);
    }

    /// <summary>
    /// Verifica que un fallo de red real sea gestionado y notificado como error global.
    /// </summary>
    [Test]
    public async Task ExecuteQueryAsync_Should_Return_ConnectionFailed_On_Network_Error()
    {
        // Arrange
        _httpHandler.ShouldThrow = true;

        // Act
        var result = await _service.ExecuteQueryAsync<dynamic>("query { test }");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(NetworkErrors.ConnectionFailed.Code);
        _notificationStoreMock.Verify(n => n.Error(It.IsAny<string>(), "Error de Red", It.IsAny<int>()), Times.Once);
    }

    /// <summary>
    /// Clase interna para simular el comportamiento de red del servidor GraphQL.
    /// </summary>
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        public string ResponseContent { get; set; } = "{}";
        public bool ShouldThrow { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (ShouldThrow) throw new HttpRequestException("Network Error");

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ResponseContent, Encoding.UTF8, "application/json")
            });
        }
    }
}