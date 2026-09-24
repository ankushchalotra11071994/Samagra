using Microsoft.AspNetCore.Diagnostics;
using Samagra.Application.Exceptions;

namespace Samagra.API.ExceptionHandlers;

internal sealed class UnsafeInputExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        if (exception is not UnsafeInputException)
            return false;

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new
        {
            error = exception.Message
        }, ct);

        return true;
    }
}