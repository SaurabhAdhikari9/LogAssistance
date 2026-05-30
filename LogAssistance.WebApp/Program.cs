using LogAssistance.WebApp.Components;
using LogAssistance.WebApp.Services.Assistance;
using LogAssistance.WebApp.Services.LogTool;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHttpClient();
builder.Services.AddSerilog((services, config) =>
    config.ReadFrom.Configuration(builder.Configuration)
    );
builder.Services.AddHttpClient<IAssistanceService, AssistanceService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["AIAssistnace:OllamaBaseUrl"]);
});

builder.Services.AddScoped<ILogService, LogService>();
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
