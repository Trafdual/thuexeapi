using ThueXe.Helpers;

namespace ThueXe.Controllers
{
    [ApiController]
    [Route("admin/payments")]
    [Authorize(Roles = Vai.VanHanh)]
    public class AdminPaymentsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AdminPaymentsController(ApplicationDbContext db) => _db = db;

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

            var don = thu.Booking;
            var lech = req.ReceivedAmount - thu.Amount;

            thu.ReceivedAmount = req.ReceivedAmount;
            thu.BankNote = req.BankNote;

            // C2: chuyển THIẾU thì KHÔNG tự xác nhận. Ghi lại số thực nhận để người vận hành
            // gọi khách chuyển bù; đơn đứng nguyên ở CHO_THANH_TOAN.
            if (lech < 0)
            {
                await _db.SaveChangesAsync();
                return new ConfirmPaymentResult(
                    thu.ToDto(), don.ToDto(), KhopSoTien: false, LechSoTien: lech, RefundPayout: null);
            }

            if (don.Status != TrangThaiDon.ChoThanhToan)
                throw new BizException("WRONG_STATE", $"Đơn đang ở {don.Status}");

            thu.Status = TrangThaiThanhToan.DaNhan;
            thu.ConfirmedBy = UserId;
            thu.ConfirmedAt = DateTimeOffset.UtcNow;

            // Tiền vào ví treo: bút toán kép, chỉ ghi thêm.
            GhiSo(don.Id, TaiKhoanSoCai.Khach, Chieu.No, thu.Amount, LoaiChungTu.ThanhToan, thu.Id);
            GhiSo(don.Id, TaiKhoanSoCai.ViTreo, Chieu.Co, thu.Amount, LoaiChungTu.ThanhToan, thu.Id);

            // Đơn sang đã xác nhận: từ đây địa chỉ giao xe và số điện thoại mở cho hai bên.
            don.Status = TrangThaiDon.DaXacNhan;
            don.HoldExpiresAt = null;

            // C3: chuyển THỪA thì vẫn xác nhận đơn, sinh thêm lệnh hoàn phần thừa cho khách.
            Payout? hoanThua = null;
            if (lech > 0)
            {
                hoanThua = new Payout
                {
                    BookingId = don.Id,
                    PayeeType = Ben.Khach,
                    PayeeId = don.RenterId,
                    BankAccount = ChiTra.ChuaCoSoTaiKhoan,
                    BankName = ChiTra.ChuaCoSoTaiKhoan,
                    Amount = lech,
                    Status = TrangThaiChiTra.Cho
                };
                _db.Payouts.Add(hoanThua);
            }

            await _db.SaveChangesAsync();

            return new ConfirmPaymentResult(
                thu.ToDto(), don.ToDto(),
                KhopSoTien: lech == 0, LechSoTien: lech,
                RefundPayout: hoanThua?.ToDto(don.Code));
        }

        private void GhiSo(long donId, string taiKhoan, string chieu, long soTien,
                           string loaiChungTu, long chungTuId)
        {
            _db.LedgerEntries.Add(new LedgerEntry
            {
                BookingId = donId,
                Account = taiKhoan,
                Direction = chieu,
                Amount = soTien,
                RefType = loaiChungTu,
                RefId = chungTuId
            });
        }
    }
}
