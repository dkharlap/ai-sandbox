using System.Security.Claims;
using SimpleAccount.Contracts;
using SimpleAccount.Account.Services;
using SimpleAccount.ServiceDefaults;

namespace SimpleAccount.Account.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        // Called by the gateway during the OIDC callback, before any user id exists,
        // so it is authorized by scope rather than by user claim.
        app.MapPut("/internal/users/by-sub", async (
                UpsertUserRequest request,
                UserService users,
                CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(request.GoogleSubject) || string.IsNullOrWhiteSpace(request.Email))
                {
                    return Results.Problem(
                        title: "GoogleSubject and Email are required.",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                return Results.Ok(await users.UpsertAsync(request, ct));
            })
            .RequireAuthorization(InternalPolicies.Provisioning)
            .WithName("UpsertUserBySubject");

        var me = app.MapGroup("/v1/users/me").RequireAuthorization(InternalPolicies.User);

        me.MapGet("/", async (ClaimsPrincipal principal, UserService users, CancellationToken ct) =>
            {
                var userId = principal.GetUserId();
                var user = await users.GetAsync(userId, ct);
                return user is null ? Results.NotFound() : Results.Ok(user);
            })
            .WithName("GetCurrentUser");

        me.MapPut("/", async (
                UpdateUserRequest request,
                ClaimsPrincipal principal,
                UserService users,
                CancellationToken ct) =>
            {
                var userId = principal.GetUserId();
                var user = await users.UpdateNameAsync(userId, request, ct);
                return user is null ? Results.NotFound() : Results.Ok(user);
            })
            .WithName("UpdateCurrentUser");
    }
}
