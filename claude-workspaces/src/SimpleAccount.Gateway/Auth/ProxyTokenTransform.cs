using System.Security.Claims;
using SimpleAccount.Contracts;
using Yarp.ReverseProxy.Model;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace SimpleAccount.Gateway.Auth;

public static class ProxyTokenTransform
{
    /// <summary>
    /// Replaces the browser's cookie with a short-lived, per-audience JWT. Any client-supplied
    /// Authorization header is dropped first so a caller cannot present its own token, and the
    /// cookie is stripped so downstream services never see session material.
    /// </summary>
    public static void AddInternalTokenTransform(this TransformBuilderContext context)
    {
        context.RequestTransforms.Add(new RequestHeaderRemoveTransform("Authorization"));
        context.RequestTransforms.Add(new RequestHeaderRemoveTransform("Cookie"));

        context.AddRequestTransform(transformContext =>
        {
            var user = transformContext.HttpContext.User;
            if (user.Identity?.IsAuthenticated is not true)
            {
                return ValueTask.CompletedTask;
            }

            var clusterId = transformContext.HttpContext
                .GetReverseProxyFeature().Route.Config.ClusterId;
            // The SPA cluster must never receive an internal token.
            if (clusterId is not (InternalAudiences.Account or InternalAudiences.Preferences))
            {
                return ValueTask.CompletedTask;
            }

            var issuer = transformContext.HttpContext.RequestServices
                .GetRequiredService<IInternalTokenIssuer>();

            var token = issuer.GetOrMintUserToken(
                userId: Guid.Parse(user.FindFirstValue(ClaimNames.UserId)!),
                googleSubject: user.FindFirstValue(ClaimNames.GoogleSubject)!,
                email: user.FindFirstValue(ClaimTypes.Email),
                audience: clusterId);

            transformContext.ProxyRequest.Headers.Authorization = new("Bearer", token);
            return ValueTask.CompletedTask;
        });
    }
}
