using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ClientBlazor.Cliente;
using ClientBlazor.Cliente.Components;
using ClientBlazor.Cliente.Infrastructures;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configuración de componentes raíz
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configuración de servicios mediante métodos de extensión (Patrón API)
builder.Services
    .AddStateStores()
    .AddDomainServices() // Registro de servicios de dominio (incluyendo Storage)
    .AddHttpInfrastructure() // Usa Storage en el Handler
    .AddApiClients();

var host = builder.Build();

// Inicializar el estado de la aplicación (LocalStorage -> Stores)
await host.InitializeAppStateAsync();

await host.RunAsync();