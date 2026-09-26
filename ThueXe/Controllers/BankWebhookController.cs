using ThueXe.Helpers;

namespace ThueXe.Controllers
{
    /// <summary>
    /// Nhận thông báo tiền về từ ngân hàng. Dịch vụ đọc biến động số dư (SePay, Casso)
    /// bắn vào đây; lúc demo thì bắn bằng curl, thân yêu cầu giống hệt nhau.
    ///
    /// Không dùng JWT vì bên gọi là máy chứ không phải người — xác thực bằng khoá bí mật
    /// dùng chung đặt ở cấu hình Bank:WebhookSecret.
    /// </summary>
    [ApiController]
    [Route("webhooks/bank")]
    public class BankWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ThanhToanService _thanhToan;
        private readonly IConfiguration _cfg;
        private readonly ILogger<BankWebhookController> _log;

        public BankWebhookController(ApplicationDbContext db, ThanhToanService thanhToan,
                                     IConfiguration cfg, ILogger<BankWebhookController> log)
        {
            _db = db;
            _thanhToan = thanhToan;
            _cfg = cfg;
            _log = log;
        }

        [HttpPost]
        public async Task<ActionResult<BankWebhookResult>> Nhan(BankWebhookRequest req)
        {
            var khoa = _cfg["Bank:WebhookSecret"];
            if (string.IsNullOrWhiteSpace(khoa))
                throw new BizException("WEBHOOK_CHUA_CAU_HINH",
                    "Chưa đặt Bank:WebhookSecret, không nhận thông báo nào cả");

            if (!Request.Headers.TryGetValue("X-Bank-Secret", out var gui) || gui != khoa)
                throw new BizException("FORBIDDEN", "Sai khoá bí mật");

            if (req.Amount <= 0)
                throw new BizException("INVALID_INPUT", "Số tiền phải lớn hơn 0");
            if (string.IsNullOrWhiteSpace(req.TransferRef))
                throw new BizException("INVALID_INPUT", "Thiếu mã giao dịch của ngân hàng");

            // Dùng chung luật dò mã với màn xác nhận tay, xem MaDon.TimTrong.
            var ma = MaDon.TimTrong(req.Content);
            if (ma is null)
                return CanNguoiXem(req, "KHONG_THAY_MA_DON",
                    "Nội dung chuyển khoản không chứa mã đơn nào");

            var thu = await _db.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.TransferCode == ma);

            if (thu is null)
                return CanNguoiXem(req, "MA_DON_LA",
                    $"Không có phiếu thu nào mang mã {ma}");

            // Cùng một mã giao dịch bắn lại thì bỏ qua. Dịch vụ đọc biến động số dư hay
            // gửi lại khi không nhận được phản hồi, không chặn là ghi sổ hai lần.
            //
            // So khớp CẢ TOKEN chứ không dùng Contains: mã giao dịch ngắn rất dễ lọt vào
            // trong mã dài đã lưu, hoặc vào phần ngày giờ, rồi chặn oan lần chuyển bù.
            if (DaGhiGiaoDich(thu.BankNote, req.TransferRef))
                return new BankWebhookResult(ThanhToanService.TinhHuong.DaXuLyTruocDo,
                    thu.Id, thu.BookingId, thu.Booking.Status, thu.Booking.Code,
                    "Giao dịch này đã ghi nhận trước đó");

            var ghiChu = $"{req.TransferRef} · {req.OccurredAt:yyyy-MM-dd HH:mm} · {req.Content}";
            var kq = await _thanhToan.GhiTienVao(thu, req.Amount, ghiChu, nguoiXacNhan: null);

            _log.LogInformation(
                "Tiền về: {Ma} {SoTien} đ, giao dịch {Ref} → {TinhHuong}, đơn {DonMa} sang {TrangThai}",
                ma, req.Amount, req.TransferRef, kq.TinhHuongXuLy, kq.Don.Code, kq.Don.Status);

            var loi = kq.TinhHuongXuLy switch
            {
                ThanhToanService.TinhHuong.ThieuTien =>
                    $"Chuyển thiếu {-kq.LechSoTien:N0} đ, đơn giữ nguyên chờ khách chuyển bù",
                _ when kq.LechSoTien > 0 =>
                    $"Chuyển thừa {kq.LechSoTien:N0} đ, đã sinh lệnh hoàn cho khách",
                _ => "Đã ghi tiền vào ví treo",
            };

            return new BankWebhookResult(kq.TinhHuongXuLy, thu.Id, thu.BookingId,
                kq.Don.Status, kq.Don.Code, loi);
        }

        /// BankNote là chuỗi nối các lần chuyển, mỗi lần một đoạn "mã · giờ · nội dung",
        /// ngăn nhau bằng " | ". Tách ra rồi so đúng phần mã, không so chuỗi con.
        private static bool DaGhiGiaoDich(string? ghiChu, string maGiaoDich)
        {
            if (string.IsNullOrWhiteSpace(ghiChu)) return false;
            return ghiChu
                .Split(" | ", StringSplitOptions.RemoveEmptyEntries)
                .Select(doan => doan.Split(" · ", 2)[0].Trim())
                .Any(ma => string.Equals(ma, maGiaoDich, StringComparison.Ordinal));
        }

        /// Ba ca không tự quyết được thì ghi log rồi để người vận hành xử bằng tay.
        /// Trả 200 để dịch vụ ngân hàng khỏi gửi lại mãi một thông báo hỏng.
        private ActionResult<BankWebhookResult> CanNguoiXem(BankWebhookRequest req,
                                                            string ma, string loi)
        {
            _log.LogWarning("Thông báo tiền về cần người xem: {Ma} — {SoTien} đ, nội dung {ND!r}",
                ma, req.Amount, req.Content);
            return new BankWebhookResult(ma, null, null, null, null, loi);
        }
    }
}
