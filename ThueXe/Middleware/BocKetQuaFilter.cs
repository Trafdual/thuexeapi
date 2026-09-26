using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ThueXe.Middleware
{
    /// Khuôn phản hồi duy nhất, thành công hay lỗi đều đủ bốn trường:
    /// { status, data, code, message }. Thành công: code/message null. Lỗi: data null.
    public record ApiResponse(int Status, object? Data, string? Code, string? Message)
    {
        public static ApiResponse Ok(object? data) => new(200, data, null, null);
        public static ApiResponse Fail(int status, string code, string message) =>
            new(status, null, code, message);
    }

    /// Bọc mọi kết quả thành công của controller vào ApiResponse. Lỗi đi đường khác
    /// (BizExceptionMiddleware và các điểm bắt lỗi trong Program.cs) nên không qua đây.
    public class BocKetQuaFilter : IResultFilter
    {
        public void OnResultExecuting(ResultExecutingContext ctx)
        {
            switch (ctx.Result)
            {
                case ObjectResult { Value: ApiResponse }:
                    return;
                case ObjectResult { StatusCode: null or 200 } r:
                    ctx.Result = new ObjectResult(ApiResponse.Ok(r.Value)) { StatusCode = 200 };
                    break;
                case StatusCodeResult { StatusCode: 200 }:
                    ctx.Result = new ObjectResult(ApiResponse.Ok(null)) { StatusCode = 200 };
                    break;
            }
        }

        public void OnResultExecuted(ResultExecutedContext ctx) { }
    }
}
