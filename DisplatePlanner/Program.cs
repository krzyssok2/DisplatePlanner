using Blazored.LocalStorage;
using DisplatePlanner;
using DisplatePlanner.Interfaces;
using DisplatePlanner.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddBlazoredLocalStorageAsSingleton();

builder.Services.AddScoped<IPlateStateService, PlateStateService>();
builder.Services.AddScoped<IAlignmentService, AlignmentService>();
builder.Services.AddScoped<ISelectionService, SelectionService>();
builder.Services.AddScoped<IClipboardService, ClipboardService>();
builder.Services.AddSingleton<ThemeService>();
builder.Services.AddScoped<IndexedDbService>();

await builder.Build().RunAsync();