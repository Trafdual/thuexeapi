namespace ThueXe.Services
{
    public class OtpService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<OtpService> _log;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _cfg;

        public OtpService(IMemoryCache cache, ILogger<OtpService> log,
                          IWebHostEnvironment env, IConfiguration cfg)
        {
            _cache = cache;
            _log = log;
            _env = env;
            _cfg = cfg;
        }

        /// Cố định 000000 cho đỡ phải đọc log khi thử. Phải hội đủ CẢ HAI điều kiện:
        /// đang ở môi trường dev, và có người bật cờ Otp:CoDinhChoDev một cách rõ ràng.
        /// Mặc định là sinh ngẫu nhiên — quên cấu hình thì an toàn, không phải mở toang.
        private bool DungOtpCoDinh =>
            _env.IsDevelopment() && _cfg.GetValue("Otp:CoDinhChoDev", false);

        public string Generate(string phone)
        {
            var otp = DungOtpCoDinh
                ? "000000"
                : Random.Shared.Next(0, 1_000_000).ToString("D6");
            _cache.Set(Key(phone), otp, TimeSpan.FromMinutes(5));
            _log.LogInformation("OTP cho {Phone}: {Otp}", phone, otp);
            return otp;
        }

        public bool Verify(string phone, string otp)
        {
            if (_cache.TryGetValue(Key(phone), out string? saved) && saved == otp)
            {
                _cache.Remove(Key(phone));
                return true;
            }
            return false;
        }

        private static string Key(string phone) => $"otp:{phone}";
    }
}
