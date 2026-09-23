using ThueXe.Helpers;

namespace ThueXe.Controllers
{
    [ApiController]
    [Route("admin/bookings")]
    [Authorize(Roles = Vai.VanHanh)]
    public class AdminBookingsController : ControllerBase
    {
        private const int HoaHongPhanTram = 15;

        private readonly ApplicationDbContext _db;

        public AdminBookingsController(ApplicationDbContext db) => _db = db;

        // POST /admin/bookings/{id}/settle — chốt phí, ghi sổ cái, sinh 2 lệnh chi
        [HttpPost("{id:long}/settle")]
        public async Task<ActionResult<SettleResult>> Settle(long id, SettleRequest req)
        {
            var don = await _db.Bookings
                .Include(b => b.Car).ThenInclude(c => c.Photos)
                .Include(b => b.Renter)
                .FirstOrDefaultAsync(b => b.Id == id)
                ?? throw new BizException("WRONG_STATE", "Không tìm thấy đơn");

            if (don.Status != TrangThaiDon.ChoQuyetToan)
                throw new BizException("WRONG_STATE", $"Đơn đang ở {don.Status}, chưa chốt được");

            var phi = req.Charges is { Count: > 0 }
                ? TuDanhSach(req.Charges)
                : await TuBienBan(don);

            foreach (var c in phi) _db.Charges.Add(c);

            var tongPhi = phi.Sum(c => c.Amount);

            // Phí trừ vào cọc. Phí lớn hơn cọc thì trừ hết cọc, phần thiếu thành khoản nợ (F2):
            // sàn không tự ứng tiền cho chủ xe phần vượt.
            var phiTruVaoCoc = Math.Min(tongPhi, don.Deposit);

            var chuXeNhan = don.RentTotal - don.Commission + phiTruVaoCoc;
            var sanNhan = don.Commission;
            var khachHoan = don.Deposit - phiTruVaoCoc;

            var soCai = new List<LedgerEntry>();
            void GhiCap(string taiKhoanCo, long soTien, string loai, long chungTuId)
            {
                if (soTien <= 0) return;   // ràng buộc CSDL: amount > 0
                soCai.Add(Them(don.Id, TaiKhoanSoCai.ViTreo, Chieu.No, soTien, loai, chungTuId));
                soCai.Add(Them(don.Id, taiKhoanCo, Chieu.Co, soTien, loai, chungTuId));
            }

            var chiChuXe = new Payout
            {
                BookingId = don.Id,
                PayeeType = Ben.ChuXe,
                PayeeId = don.Car.OwnerId,
                Amount = chuXeNhan,
                Status = TrangThaiChiTra.Cho,
                BankAccount = ChiTra.ChuaCoSoTaiKhoan,
                BankName = ChiTra.ChuaCoSoTaiKhoan
            };

            // Số tài khoản chủ xe lấy từ bản cam kết họ đã ký.
            var camKet = await _db.OwnerAgreements
                .Where(a => a.OwnerId == don.Car.OwnerId)
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();
            if (camKet is not null)
            {
                chiChuXe.BankAccount = camKet.BankAccount;
                chiChuXe.BankName = camKet.BankName;
            }

            // Cọc là tiền bảo đảm, không phải tiền phạt — phần còn dư luôn trả lại khách.
            var hoanKhach = new Payout
            {
                BookingId = don.Id,
                PayeeType = Ben.Khach,
                PayeeId = don.RenterId,
                Amount = khachHoan,
                // Hoàn 0 đồng thì không phải chuyển khoản, đóng luôn cho người vận hành
                // khỏi phải bấm một lệnh rỗng mỗi tối.
                Status = khachHoan > 0 ? TrangThaiChiTra.Cho : TrangThaiChiTra.DaChi,
                TransferRef = khachHoan > 0 ? null : "KHONG_CAN_CHUYEN",
                PaidAt = khachHoan > 0 ? null : DateTimeOffset.UtcNow,
                BankAccount = ChiTra.ChuaCoSoTaiKhoan,
                BankName = ChiTra.ChuaCoSoTaiKhoan
            };

            _db.Payouts.Add(chiChuXe);
            _db.Payouts.Add(hoanKhach);
            await _db.SaveChangesAsync();   // lấy id hai lệnh chi trước khi ghi sổ

            GhiCap(TaiKhoanSoCai.ChuXe, chuXeNhan, LoaiChungTu.ChiTra, chiChuXe.Id);
            GhiCap(TaiKhoanSoCai.San, sanNhan, LoaiChungTu.ChiTra, chiChuXe.Id);
            GhiCap(TaiKhoanSoCai.Khach, khachHoan, LoaiChungTu.ChiTra, hoanKhach.Id);

            foreach (var e in soCai) _db.LedgerEntries.Add(e);

            don.Status = TrangThaiDon.ChoChiTra;
            await _db.SaveChangesAsync();

            return new SettleResult(
                don.ToDto(),
                phi.Select(c => new ChargeDto(c.Id, c.Type, c.Amount, c.Note)).ToList(),
                new List<PayoutDto> { chiChuXe.ToDto(don.Code), hoanKhach.ToDto(don.Code) },
                soCai.Select(e => e.ToDto()).ToList());
        }

        private static LedgerEntry Them(long donId, string taiKhoan, string chieu,
                                        long soTien, string loai, long chungTuId) => new()
        {
            BookingId = donId,
            Account = taiKhoan,
            Direction = chieu,
            Amount = soTien,
            RefType = loai,
            RefId = chungTuId
        };

        private List<Charge> TuDanhSach(List<ChargeRequest> ds)
        {
            var hopLe = new[] { LoaiPhi.QuaGio, LoaiPhi.QuaKm, LoaiPhi.NhienLieu };
            foreach (var c in ds)
            {
                if (!hopLe.Contains(c.Type))
                    throw new BizException("INVALID_INPUT", $"Loại phí lạ: {c.Type}");
                if (c.Amount < 0)
                    throw new BizException("INVALID_INPUT", "Phí không được âm");
            }
            return ds.Where(c => c.Amount > 0)
                     .Select(c => new Charge { Type = c.Type, Amount = c.Amount, Note = c.Note })
                     .ToList();
        }

        /// Không gửi danh sách phí thì tự tính từ hai biên bản giao và trả.
        private async Task<List<Charge>> TuBienBan(Booking don)
        {
            var bienBan = await _db.Handovers
                .Where(h => h.BookingId == don.Id)
                .ToListAsync();

            var giao = bienBan.FirstOrDefault(h => h.Kind == LoaiBienBan.Giao);
            var tra = bienBan.FirstOrDefault(h => h.Kind == LoaiBienBan.Tra);
            if (giao is null || tra is null)
                throw new BizException("INVALID_INPUT",
                    "Thiếu biên bản giao hoặc trả, không tự tính phí được. Gửi kèm charges.");

            var treGio = (int)Math.Max(0,
                Math.Ceiling((tra.CreatedAt - don.EndDate.ToDateTime(TimeOnly.MinValue)).TotalHours));

            var phi = PhiPhatSinhService.Tinh(
                don.PricePerDay, don.Days, don.Car.MaxKmDay,
                giao.Odo, tra.Odo, giao.FuelLevel, tra.FuelLevel, treGio);

            var ds = new List<Charge>();
            if (phi.QuaGio > 0) ds.Add(new Charge { Type = LoaiPhi.QuaGio, Amount = phi.QuaGio, Note = $"trễ {treGio} giờ" });
            if (phi.QuaKm > 0) ds.Add(new Charge { Type = LoaiPhi.QuaKm, Amount = phi.QuaKm, Note = $"ODO {giao.Odo} → {tra.Odo}" });
            if (phi.NhienLieu > 0) ds.Add(new Charge { Type = LoaiPhi.NhienLieu, Amount = phi.NhienLieu, Note = $"nhiên liệu {giao.FuelLevel} → {tra.FuelLevel} nấc" });
            foreach (var c in ds) c.BookingId = don.Id;
            return ds;
        }
    }
}
