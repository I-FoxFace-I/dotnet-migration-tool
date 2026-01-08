using MigrationTool.Core.DependencyInjection;
using MigrationTool.Localization;
using MigrationTool.Blazor.Server.Components;
using MigrationTool.Blazor.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MigrationTool Core services
builder.Services.AddMigrationToolCore();

// Add Localization
builder.Services.AddSingleton<ILocalizationService, LocalizationService>();

// Add Application State
builder.Services.AddSingleton<AppState>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
