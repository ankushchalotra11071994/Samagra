using System;
using Microsoft.AspNetCore.Diagnostics;

namespace samagra.API.Middleware
{

    public class GlobalExceptionHandler : IExceptionHandler,IMiddleware
    {

     public RequestDelegate _next { get; }
       public GlobalExceptionHandler(RequestDelegate next)
        {
            _next = next;
        }
        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
             _next(context);
             return Task.CompletedTask;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is not null)
            {

                httpContext.Response.StatusCode = 500;
                httpContext.Response.ContentType = "application/json";
                var errorResponse = new { message = "An unexpected error occurred." };
                return await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken).ContinueWith(_ => true, cancellationToken);
            }

            return  await ValueTask.FromResult(false);
        }
    }
}