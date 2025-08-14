using Blazored.LocalStorage;
using DisplatePlanner;
using DisplatePlanner.Interfaces;
using DisplatePlanner.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

ConfigureServices(builder.Services, builder.HostEnvironment.BaseAddress);

await builder.Build().RunAsync();

static void ConfigureServices(IServiceCollection services, string baseAddress)
{
    services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseAddress) });
    services.AddBlazoredLocalStorageAsSingleton();

    services.AddScoped<IPlateStateService, PlateStateService>();
    services.AddScoped<IAlignmentService, AlignmentService>();
    services.AddScoped<ISelectionService, SelectionService>();
    services.AddScoped<IClipboardService, ClipboardService>();
    services.AddScoped<IJsInteropService, JsInteropService>();
    services.AddSingleton<IZoomService, ZoomService>();
    services.AddSingleton<IColorManagementService, ColorManagementService>();
    services.AddSingleton<ThemeService>();
    services.AddScoped<IndexedDbService>();
}