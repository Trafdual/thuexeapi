using ThueXe.Common;

namespace ThueXe.Middleware
{
    public class BizExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<BizExceptionMiddleware> _log;

        public BizExceptionMiddleware(RequestDelegate next, ILogger<BizExceptionMiddleware> log)
        {
            _next = next;
            _log = log;
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            try
            {
                await _next(ctx);
            }
            catch (BizException ex)
            {
                ctx.Response.StatusCode = 422;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsJsonAsync(new { code = ex.Code, message = ex.Message });
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Lỗi không mong đợi tại {Path}", ctx.Request.Path);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsJsonAsync(new { code = "INTERNAL_ERROR", message = "Có lỗi xảy ra" });
            }
        }
    }
}
