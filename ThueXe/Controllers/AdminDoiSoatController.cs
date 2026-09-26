using ThueXe.Helpers;

namespace ThueXe.Controllers
{
    /// <summary>
    /// Đối soát cuối ngày: so số dư sổ cái với số dư thật trong tài khoản ngân hàng.
    ///
    /// Sổ cái chỉ cộng ra số dư LÝ THUYẾT. Tiền thật có thể lệch vì phí chuyển khoản,
    /// chuyển nhầm, hoặc quên ghi nhận — và hiện không có gì phát hiện. Mục 8.6 của
    /// báo cáo đặt ra kỷ luật đối soát mỗi tối; đây là chỗ làm việc đó.
    /// </summary>
    [ApiController]
    [Route("admin/doi-soat")]
    [Authorize(Roles = Vai.VanHanh)]
    public class AdminDoiSoatController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AdminDoiSoatController(ApplicationDbContext db) => _db = db;

        // GET /admin/doi-soat?soDuNganHang=12345678
        [HttpGet]
        public async Task<ActionResult<DoiSoatResult>> Get([FromQuery] long? soDuNganHang)
        {
            // Tiền thật đã chảy vào: chỉ tính phiếu thu đã xác nhận.
            var daThu = await _db.Payments
                .Where(p => p.Status == TrangThaiThanhToan.DaNhan)
                .SumAsync(p => (long?)(p.ReceivedAmount ?? p.Amount)) ?? 0;

            // Tiền thật đã chảy ra: chỉ tính lệnh chi đã chuyển khoản.
            var daChi = await _db.Payouts
                .Where(p => p.Status == TrangThaiChiTra.DaChi)
                .SumAsync(p => (long?)p.Amount) ?? 0;

            // Ví treo theo sổ cái: tổng CÓ trừ tổng NỢ trên tài khoản VI_TREO.
            var buts = await _db.LedgerEntries
                .Where(e => e.Account == TaiKhoanSoCai.ViTreo)
                .Select(e => new { e.Direction, e.Amount })
                .ToListAsync();
            var viTreoTheoSo = buts.Sum(e => e.Direction == Chieu.Co ? e.Amount : -e.Amount);

            // Còn nợ người dùng: lệnh chi đã sinh nhưng chưa chuyển.
            var conPhaiChi = await _db.Payouts
                .Where(p => p.Status != TrangThaiChiTra.DaChi)
                .SumAsync(p => (long?)p.Amount) ?? 0;

            var kyVong = daThu - daChi;
            long? lech = soDuNganHang.HasValue ? soDuNganHang.Value - kyVong : null;

            return new DoiSoatResult(
                TinhDenLuc: DateTimeOffset.UtcNow,
                TongDaThu: daThu,
                TongDaChi: daChi,
                SoDuKyVong: kyVong,
                SoDuNganHang: soDuNganHang,
                Lech: lech,
                ViTreoTheoSoCai: viTreoTheoSo,
                ConPhaiChi: conPhaiChi,
                Dat: lech == 0,
                Loi: soDuNganHang is null
                    ? "Chưa nhập số dư ngân hàng, chỉ tính được số dư kỳ vọng."
                    : lech == 0
                        ? "Khớp. Sổ cái và tài khoản ngân hàng bằng nhau."
                        : lech > 0
                            ? $"Ngân hàng NHIỀU HƠN sổ {lech:N0} đ — có tiền vào chưa ghi nhận."
                            : $"Ngân hàng ÍT HƠN sổ {-lech:N0} đ — dừng lại tra trước khi chi tiếp.");
        }
    }
}
