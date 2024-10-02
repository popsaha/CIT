using System.Net;

namespace CIT.API.Middlewares
{
    public class ExceptionHandlerMiddleware
    {

        private readonly ILogger<ExceptionHandlerMiddleware> _logger;
        private readonly RequestDelegate _next;

        public ExceptionHandlerMiddleware(ILogger<ExceptionHandlerMiddleware> logger, RequestDelegate next)
        {
            _logger = logger;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                var errorId = Guid.NewGuid();

                //log this exception
                _logger.LogError(ex, $"{errorId} : {ex.Message}");

                //return a custom error response
                httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                httpContext.Response.ContentType = "application/json";

                var error = new
                {
                    Id = errorId,
                    ErrprMessage = "Something went wrong."
                };

                await httpContext.Response.WriteAsJsonAsync(error);

            }
        }
    }
}
