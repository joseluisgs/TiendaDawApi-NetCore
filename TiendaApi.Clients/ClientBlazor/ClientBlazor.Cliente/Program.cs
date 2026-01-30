using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ClientBlazor.Cliente;
using ClientBlazor.Cliente.Components;
using ClientBlazor.Cliente.State;
using ClientBlazor.Cliente.Configuration;
using ClientBlazor.Cliente.Services;

// Crear el host de la aplicacion Blazor WebAssembly
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Agregar componentes raiz
builder.RootComponents.Add<App>("#app"); // Componente principal de la aplicacion
builder.RootComponents.Add<HeadOutlet>("head::after"); // Manejo del head del documento

// Registrar stores globales (estado reactivo con RX)
builder.Services.AddSingleton<AuthStore>(); // Store para gestion de autenticacion
builder.Services.AddSingleton<NotificationStore>(); // Store para notificaciones globales

// Registrar servicios de dominio
builder.Services.AddTransient<AuthService>(); // Servicio de autenticacion
builder.Services.AddTransient<RestService>(); // Servicio REST simulado
builder.Services.AddTransient<GraphQLService>(); // Servicio GraphQL simulado
builder.Services.AddTransient<WebSocketService>(); // Servicio WebSocket simulado
builder.Services.AddTransient<SignalRService>(); // Servicio SignalR simulado

// HttpClient para llamadas a la API
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(AppConfig.ApiBaseUrl) });


// Construir y ejecutar la aplicacion
await builder.Build().RunAsync();
