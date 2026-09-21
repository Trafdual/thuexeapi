namespace ThueXe.Services
{
    public class OtpService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<OtpService> _log;

        public OtpService(IMemoryCache cache, ILogger<OtpService> log)
        {
            _cache = cache;
            _log = log;
        }

        public string Generate(string phone)
        {
            var otp = Random.Shared.Next(100000, 999999).ToString();
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
