using ThueXe.Helpers;

namespace ThueXe.Controllers
{
    [ApiController]
    [Route("admin/payments")]
    [Authorize(Roles = Vai.VanHanh)]
    public class AdminPaymentsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ThanhToanService _thanhToan;

        public AdminPaymentsController(ApplicationDbContext db, ThanhToanService thanhToan)
        {
            _db = db;
            _thanhToan = thanhToan;
        }

        private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // POST /admin/payments/{id}/confirm — xác nhận đã nhận tiền
        [HttpPost("{id:long}/confirm")]
        public async Task<ActionResult<ConfirmPaymentResult>> Confirm(long id, ConfirmPaymentRequest req)
        {
            var thu = await _db.Payments
                .Include(p => p.Booking).ThenInclude(b => b.Car).ThenInclude(c => c.Photos)
                .Include(p => p.Booking).ThenInclude(b => b.Renter)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new BizException("NOT_FOUND", "Không tìm thấy phiếu thu");

            if (thu.Status == TrangThaiThanhToan.DaNhan)
                throw new BizException("WRONG_STATE", "Phiếu thu này đã xác nhận rồi");

            // Lỗi tốn tiền nhất ở đường bấm tay là CHỌN NHẦM PHIẾU THU: người vận hành
            // nhìn một dòng sao kê rồi bấm nhầm đơn khác. Dán nội dung chuyển khoản vào
            // thì máy đối chiếu hộ — cùng luật dò mã với webhook.
            var maTrongNoiDung = MaDon.TimTrong(req.BankNote);
            if (maTrongNoiDung is not null && maTrongNoiDung != thu.TransferCode)
                throw new BizException("MA_DON_KHONG_KHOP",
                    $"Nội dung chuyển khoản mang mã {maTrongNoiDung}, " +
                    $"nhưng phiếu thu này là {thu.TransferCode}. Kiểm lại xem có chọn nhầm đơn không.");

            var kq = await _thanhToan.GhiTienVao(thu, req.ReceivedAmount, req.BankNote, UserId);

            return new ConfirmPaymentResult(
                kq.Thu.ToDto(), kq.Don.ToDto(),
                KhopSoTien: kq.KhopSoTien, LechSoTien: kq.LechSoTien,
                RefundPayout: kq.HoanThua?.ToDto(kq.Don.Code),
                DaDoiChieuNoiDung: maTrongNoiDung is not null);
        }
    }
}
