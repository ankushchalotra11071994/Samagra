using Microsoft.AspNetCore.Diagnostics;
using Samagra.Application.Exceptions;

namespace Samagra.API.ExceptionHandlers;

internal sealed class AiBudgetExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        if (exception is not AiBudgetExceededException budget)
            return false;   // दूसरी exceptions हम नहीं संभालते

        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.Response.WriteAsJsonAsync(new
        {
            error = budget.Message,
            spentUsd = budget.SpentUsd,
            limitUsd = budget.LimitUsd
        }, ct);

        return true;
    }
}