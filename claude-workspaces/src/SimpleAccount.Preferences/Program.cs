using SimpleAccount.Contracts;
using SimpleAccount.Preferences;
using SimpleAccount.Preferences.Data;
using SimpleAccount.Preferences.Endpoints;
using SimpleAccount.Preferences.Services;
using SimpleAccount.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddInternalJwtAuth(InternalAudiences.Preferences);

builder.AddNpgsqlDbContext<PreferencesDbContext>("prefsdb");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<PreferenceService>();
builder.Services.AddHostedService<DbInitializer>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapPreferenceEndpoints();

app.Run();

/// <summary>Exposed so WebApplicationFactory can boot this service in tests.</summary>
public partial class Program;
