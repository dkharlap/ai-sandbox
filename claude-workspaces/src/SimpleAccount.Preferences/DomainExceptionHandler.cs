using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SimpleAccount.Preferences.Services;

namespace SimpleAccount.Preferences;

/// <summary>
/// Maps expected domain failures to 400 ProblemDetails. Anything else falls through to the
/// default handler, which returns a bare 500 without leaking internals.
/// </summary>
public class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        var detail = exception switch
        {
            UnknownModelException e => e.Message,
            DuplicateModelException e => e.Message,
            _ => null,
        };

        if (detail is null)
        {
            return false;
        }

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Title = "Invalid preference request",
                Detail = detail,
                Status = StatusCodes.Status400BadRequest,
            },
        });
    }
}
