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

builder.Services.AddSingleton<IHistoryService, HistoryService>();
builder.Services.AddSingleton<IAlignmentService, AlignmentService>();
await builder.Build().RunAsync();