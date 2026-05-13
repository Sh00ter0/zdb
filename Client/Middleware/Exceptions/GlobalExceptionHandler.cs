using Application.Exceptions.API;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Client.Middleware.Exceptions
{
    public class GlobalExceptionHandler(
        IProblemDetailsService details,
        ILogger<GlobalExceptionHandler> _logger) : IExceptionHandler
    {

        public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is not ProblemException)
            {
                return true;
            }
            
            ProblemException problemException = (ProblemException)exception;

            _logger.LogError("[{Exception}] - Encountered an while processing the request at: {Path}", problemException.Error, context.Request.Path.Value);

            var problemDetails = new ProblemDetails
            {
                Status = problemException.StatusCode,
                Type = problemException.Error,
                Detail = problemException.Message
            };

            context.Response.StatusCode = problemException.StatusCode;
            return await details.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = problemDetails
            });
        }
    }
}
