namespace ThueXe.Models
{
    /// Mã thiết bị FCM của một người dùng. Một người có thể đăng nhập trên nhiều máy.
    public class DeviceToken
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string Token { get; set; } = null!;
        public string Platform { get; set; } = "android";
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
