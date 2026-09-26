using System.Runtime.ConstrainedExecution;

namespace ThueXe.Models
{
    public class AppUser
    {
        public long Id { get; set; }

        public string Phone { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public string FullName { get; set; } = null!;

        public string? Email { get; set; }

        public bool IsOwner { get; set; }

        public string Status { get; set; } = "HOAT_DONG";

        /// NGUOI_DUNG | VAN_HANH. Người vận hành mới gọi được các đường /admin/**.
        public string Role { get; set; } = "NGUOI_DUNG";

        /// Tài khoản nhận tiền hoàn. Chủ xe lấy được từ bản cam kết đã ký, còn khách
        /// thì trước đây không có chỗ nào lưu — lệnh hoàn cọc phải ghi "CHUA_CO" rồi
        /// người vận hành đi hỏi từng người.
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
            = DateTimeOffset.UtcNow;

        public ICollection<IdDocument> Documents { get; set; }
            = new List<IdDocument>();

        public ICollection<Car> Cars { get; set; }
            = new List<Car>();
        public ICollection<Booking> Bookings { get; set; }
    }
}