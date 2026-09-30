using SimpleAccount.Gateway.Auth;
using SimpleAccount.Gateway.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddGatewayAuth();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(context => context.AddInternalTokenTransform())
    .AddServiceDiscoveryDestinationResolver();

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapBffEndpoints();

// Registered only when there are no Google credentials and the environment is not
// Production, so it can never act as a bypass in a deployed environment.
if (app.Services.GetRequiredService<AuthMode>().DevSignInEnabled)
{
    app.MapDevAuthEndpoints();
}

// Per-route AuthorizationPolicy in config: API routes require a session, the SPA route is
// anonymous. A blanket RequireAuthorization here would lock the user out of the login page.
app.MapReverseProxy();

app.Run();

/// <summary>Exposed so the Aspire test host can reference this entry point.</summary>
public partial class Program;
