using Blazored.LocalStorage;
using DisplatePlanner;
using DisplatePlanner.Interfaces;
using DisplatePlanner.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

ConfigureServices(builder.Services, builder.HostEnvironment, builder.HostEnvironment.BaseAddress);

await builder.Build().RunAsync();

static void ConfigureServices(IServiceCollection services, IWebAssemblyHostEnvironment webHostEnv, string baseAddress)
{
    services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseAddress) });

    services.AddBlazoredLocalStorageAsSingleton();

    services.AddScoped<IWallService, WallService>();
    services.AddScoped<IPlateStateService, PlateStateService>();
    services.AddScoped<IAlignmentService, AlignmentService>();
    services.AddScoped<ISelectionService, SelectionService>();
    services.AddScoped<IClipboardService, ClipboardService>();
    services.AddScoped<IRulerService, RulerService>();
    services.AddScoped<IJsInteropService, JsInteropService>();
    services.AddSingleton<IZoomService, ZoomService>();
    services.AddSingleton<IColorManagementService, ColorManagementService>();
    services.AddSingleton<ThemeService>();
    services.AddScoped<IndexedDbService>();
}