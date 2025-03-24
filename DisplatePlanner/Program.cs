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

builder.Services.AddSingleton<IPlateStateService, PlateStateService>();
builder.Services.AddSingleton<IAlignmentService, AlignmentService>();
builder.Services.AddSingleton<ISelectionService, SelectionService>();
builder.Services.AddSingleton<IClipboardService, ClipboardService>();

await builder.Build().RunAsync();