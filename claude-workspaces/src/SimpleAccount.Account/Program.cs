using SimpleAccount.Account.Data;
using SimpleAccount.Account.Endpoints;
using SimpleAccount.Account.Services;
using SimpleAccount.Contracts;
using SimpleAccount.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddInternalJwtAuth(InternalAudiences.Account);

builder.AddNpgsqlDbContext<AccountDbContext>("accountsdb");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<UserService>();
builder.Services.AddHostedService<DbInitializer>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapUserEndpoints();

app.Run();

/// <summary>Exposed so WebApplicationFactory can boot this service in tests.</summary>
public partial class Program;
