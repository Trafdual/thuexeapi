namespace ThueXe.Common
{
    // Mọi lỗi nghiệp vụ ném ra kiểu này. BizExceptionMiddleware bắt và trả { code, message }.
    public class BizException : Exception
    {
        public string Code { get; }

        public BizException(string code, string? message = null) : base(message ?? code)
            => Code = code;
    }
}
