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

        private readonly ThongBaoService _tb;

        private readonly HoanTraService _hoanTra;

        private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public AdminBookingsController(ApplicationDbContext db, ThongBaoService tb, HoanTraService hoanTra)
        {
            _db = db;
            _tb = tb;
            _hoanTra = hoanTra;
        }

        // GET /admin/bookings?status= — toàn bộ đơn của sàn, mới nhất trước
        [HttpGet]
        public async Task<ActionResult<List<BookingDto>>> DanhSach([FromQuery] string? status)
        {
            var q = _db.Bookings
                .Include(b => b.Car).ThenInclude(c => c.Photos)
                .Include(b => b.Renter)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(b => b.Status == status);

            var don = await q.OrderByDescending(b => b.CreatedAt).Take(500).ToListAsync();

            var khachId = don.Select(b => b.RenterId).Distinct().ToList();
            var giayTo = await _db.IdDocuments
                .Where(d => khachId.Contains(d.UserId))
                .GroupBy(d => d.UserId)
                .Select(g => new { UserId = g.Key, Status = g.OrderByDescending(x => x.Id).First().Status })
                .ToDictionaryAsync(x => x.UserId, x => x.Status);

            return don.Select(b => b.ToDto(giayTo.GetValueOrDefault(b.RenterId, "CHUA_NOP"))).ToList();
        }

        // GET /admin/bookings/{khoa} — chi tiết một đơn bất kỳ (người vận hành không phải chủ xe hay khách)
        [HttpGet("{khoa}")]
        public async Task<ActionResult<BookingDetailDto>> ChiTiet(string khoa)
        {
            var don = await _db.Bookings
                .Include(b => b.Car).ThenInclude(c => c.Photos)
                .Include(b => b.Renter)
                .TheoKhoa(khoa)
                .FirstOrDefaultAsync()
                ?? throw new BizException("NOT_FOUND", "Không tìm thấy đơn");

            var thanhToan = await _db.Payments.Where(p => p.BookingId == don.Id)
                .OrderByDescending(p => p.Id).FirstOrDefaultAsync();
            var bienBan = await _db.Handovers.Include(h => h.Photos)
                .Where(h => h.BookingId == don.Id).ToListAsync();
            var phi = await _db.Charges.Where(c => c.BookingId == don.Id).ToListAsync();
            var suCo = await _db.Incidents.Include(i => i.Photos)
                .Where(i => i.BookingId == don.Id).ToListAsync();
            var giayTo = await _db.IdDocuments.Where(d => d.UserId == don.RenterId)
                .OrderByDescending(d => d.Id).Select(d => d.Status).FirstOrDefaultAsync();

            return new BookingDetailDto(
                don.ToDto(giayTo ?? "CHUA_NOP"),
                thanhToan is null ? null : new PaymentDto(
                    thanhToan.Id, thanhToan.BookingId, thanhToan.Amount, thanhToan.TransferCode,
                    thanhToan.QrUrl, thanhToan.Status, thanhToan.ReceivedAmount,
                    thanhToan.ConfirmedAt, thanhToan.BankNote),
                bienBan.Select(h => h.ToDto()).ToList(),
                phi.Select(c => new ChargeDto(c.Id, c.Type, c.Amount, c.Note)).ToList(),
                suCo.Select(i => i.ToDto()).ToList());
        }

        // POST /admin/bookings/{id}/dispute/resolve — kết luận một vụ tranh chấp.
        [HttpPost("{khoa}/dispute/resolve")]
        public async Task<ActionResult<BookingDto>> ResolveDispute(string khoa, ResolveDisputeRequest req)
        {
            var don = await _db.Bookings
                .Include(b => b.Car).ThenInclude(c => c.Photos)
                .Include(b => b.Renter)
                .TheoKhoa(khoa)
                .FirstOrDefaultAsync()
                ?? throw new BizException("NOT_FOUND", "Không tìm thấy đơn");

            if (don.Status != TrangThaiDon.TranhChap)
                throw new BizException("WRONG_STATE", "Đơn này không ở trạng thái tranh chấp");

            switch (req.Outcome)
            {
                case "RESUME":
                    if (don.PreDisputeStatus is null)
                        throw new BizException("WRONG_STATE", "Đơn này không có trạng thái để quay lại, dùng CANCEL_*");
                    don.Status = don.PreDisputeStatus;
                    don.PreDisputeStatus = null;
                    break;

                // Lỗi thuộc chủ xe (không giao xe, xe không đúng cam kết…): huỷ đơn, hoàn 100%
                // tiền thuê + cọc cho khách.
                case "CANCEL_REFUND_RENTER":
                    ChanHuyKhiDaChayXongChuyen(don);
                    await HuyVaChiaTien(don, hoanKhach: don.RentTotal + don.Deposit, denBuChuXe: 0);
                    await GhiNhanLoi(don.Car.OwnerId);
                    break;

                // Lỗi thuộc khách (không tới nhận xe giữa chừng bỏ ngang, gây sự cố…): huỷ đơn,
                // hoàn cọc, tiền thuê coi như đền bù cho chủ xe — cùng chính sách BookingsController
                // dùng khi khách chủ động huỷ sát giờ.
                case "CANCEL_COMPENSATE_OWNER":
                    ChanHuyKhiDaChayXongChuyen(don);
                    await HuyVaChiaTien(don, hoanKhach: don.Deposit, denBuChuXe: don.RentTotal);
                    await GhiNhanLoi(don.RenterId);
                    break;

                default:
                    throw new BizException("INVALID_INPUT",
                        "Outcome phải là RESUME, CANCEL_REFUND_RENTER hoặc CANCEL_COMPENSATE_OWNER");
            }

            don.DisputeReason = req.Note is null ? don.DisputeReason : $"{don.DisputeReason} — Kết luận: {req.Note}";
            don.DisputeResolvedBy = UserId;
            await _db.SaveChangesAsync();

            await _tb.Gui(don.RenterId, "Tranh chấp đã có kết luận", $"Đơn {don.Code} đã xử lý xong.", don.Code);
            await _tb.Gui(don.Car.OwnerId, "Tranh chấp đã có kết luận", $"Đơn {don.Code} đã xử lý xong.", don.Code);
            return don.ToDto();
        }

        /// Tranh chấp mở từ CHO_QUYET_TOAN nghĩa là chuyến đã chạy xong, chỉ đang cãi nhau về
        /// TIỀN PHÍ — không phải về việc có nên đi chuyến hay không. Huỷ cả đơn trong trường hợp
        /// này là hoàn oan cho khách hoặc mất trắng cho chủ xe dù chuyến đã hoàn thành bình
        /// thường; đường đúng là RESUME rồi chốt lại phí qua /settle với charges đã sửa.
        private void ChanHuyKhiDaChayXongChuyen(Booking don)
        {
            if (don.PreDisputeStatus == TrangThaiDon.ChoQuyetToan)
                throw new BizException("WRONG_STATE",
                    "Chuyến đã chạy xong, chỉ RESUME rồi chốt lại phí qua /settle, không huỷ cả đơn được");
        }

        private async Task HuyVaChiaTien(Booking don, long hoanKhach, long denBuChuXe)
        {
            don.Status = TrangThaiDon.DaHuy;
            don.PreDisputeStatus = null;
            var dong = await _db.CarAvailabilities.Where(a => a.BookingId == don.Id).ToListAsync();
            _db.CarAvailabilities.RemoveRange(dong);

            await _hoanTra.ChiTuViTreo(don.Id, Ben.Khach, don.RenterId, hoanKhach, LoaiChungTu.TranhChap);
            await _hoanTra.ChiTuViTreo(don.Id, Ben.ChuXe, don.Car.OwnerId, denBuChuXe, LoaiChungTu.TranhChap);
        }

        private async Task GhiNhanLoi(long userId)
        {
            var u = await _db.AppUsers.FindAsync(userId);
            if (u is not null) u.IncidentCount++;
        }

        // POST /admin/bookings/{id}/late-fine — phạt nguội hoặc chi phí phát sinh phát hiện
        // SAU khi đơn đã đóng (biên lai phạt nguội thường về muộn cả tuần). Cộng vào DebtAmount
        // — cùng cơ chế khoá đặt đơn mới đang dùng cho phí vượt cọc, khách phải trả mới đặt
        // được chuyến tiếp theo.
        [HttpPost("{khoa}/late-fine")]
        public async Task<ActionResult<BookingDto>> LateFine(string khoa, LateFineRequest req)
        {
            if (req.Amount <= 0)
                throw new BizException("INVALID_INPUT", "Số tiền phải lớn hơn 0");
            if (string.IsNullOrWhiteSpace(req.Note))
                throw new BizException("INVALID_INPUT", "Phải ghi rõ lý do");

            var don = await _db.Bookings.TheoKhoa(khoa).FirstOrDefaultAsync()
                ?? throw new BizException("NOT_FOUND", "Không tìm thấy đơn");
            if (don.Status != TrangThaiDon.HoanTat)
                throw new BizException("WRONG_STATE", "Chỉ ghi phạt nguội cho đơn đã hoàn tất");

            _db.Charges.Add(new Charge
            {
                BookingId = don.Id, Type = LoaiPhi.PhatNguoi, Amount = req.Amount, Note = req.Note
            });
            don.DebtAmount += req.Amount;
            don.DebtPaidAt = null;
            await _db.SaveChangesAsync();

            await _tb.Gui(don.RenterId, "Có khoản phạt nguội mới",
                $"Đơn {don.Code}: {req.Note}, số tiền {req.Amount:N0}đ. Thanh toán trước khi đặt chuyến mới.",
                don.Code);
            return don.ToDto();
        }

        // POST /admin/bookings/{id}/debt-paid — khách đã chuyển khoản trả nợ, ghi vào ví treo
        // và sinh lệnh chi bù cho chủ xe. Gọi lại lần hai không ghi sổ hai lần.
        [HttpPost("{khoa}/debt-paid")]
        public async Task<ActionResult<SettleResult>> ThuNo(string khoa)
        {
            var don = await _db.Bookings
                .Include(b => b.Car).ThenInclude(c => c.Photos)
                .Include(b => b.Renter)
                .TheoKhoa(khoa)
                .FirstOrDefaultAsync()
                ?? throw new BizException("NOT_FOUND", "Không tìm thấy đơn");

            if (don.DebtAmount <= 0)
                throw new BizException("WRONG_STATE", "Đơn này không có khoản nợ");
            if (don.DebtPaidAt is not null)
                throw new BizException("WRONG_STATE", "Khoản nợ đã được thu rồi");

            var chuXe = await _db.OwnerAgreements
                .Where(a => a.OwnerId == don.Car.OwnerId)
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();

            var chi = new Payout
            {
                BookingId = don.Id,
                PayeeType = Ben.ChuXe,
                PayeeId = don.Car.OwnerId,
                Amount = don.DebtAmount,
                Status = TrangThaiChiTra.Cho,
                BankAccount = chuXe?.BankAccount ?? ChiTra.ChuaCoSoTaiKhoan,
                BankName = chuXe?.BankName ?? ChiTra.ChuaCoSoTaiKhoan
            };
            _db.Payouts.Add(chi);
            don.DebtPaidAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();   // lấy id lệnh chi trước khi ghi sổ

            // Tiền khách trả nợ vào ví treo, rồi ra cho chủ xe: hai cặp bút toán kép.
            var soCai = new List<LedgerEntry>
            {
                Them(don.Id, TaiKhoanSoCai.Khach, Chieu.No, don.DebtAmount, LoaiChungTu.NoPhatSinh, don.Id),
                Them(don.Id, TaiKhoanSoCai.ViTreo, Chieu.Co, don.DebtAmount, LoaiChungTu.NoPhatSinh, don.Id),
                Them(don.Id, TaiKhoanSoCai.ViTreo, Chieu.No, don.DebtAmount, LoaiChungTu.ChiTra, chi.Id),
                Them(don.Id, TaiKhoanSoCai.ChuXe, Chieu.Co, don.DebtAmount, LoaiChungTu.ChiTra, chi.Id),
            };
            foreach (var e in soCai) _db.LedgerEntries.Add(e);
            await _db.SaveChangesAsync();

            return new SettleResult(don.ToDto(), new List<ChargeDto>(),
                new List<PayoutDto> { chi.ToDto(don.Code) },
                soCai.Select(e => e.ToDto()).ToList());
        }

        // POST /admin/bookings/{id}/settle — chốt phí, ghi sổ cái, sinh 2 lệnh chi
        [HttpPost("{khoa}/settle")]
        public async Task<ActionResult<SettleResult>> Settle(string khoa, SettleRequest req)
        {
            var don = await _db.Bookings
                .Include(b => b.Car).ThenInclude(c => c.Photos)
                .Include(b => b.Renter)
                .TheoKhoa(khoa)
                .FirstOrDefaultAsync()
                ?? throw new BizException("NOT_FOUND", "Không tìm thấy đơn");

            if (don.Status != TrangThaiDon.ChoQuyetToan)
                throw new BizException("WRONG_STATE", $"Đơn đang ở {don.Status}, chưa chốt được");

            var phi = req.Charges is { Count: > 0 }
                ? TuDanhSach(req.Charges)
                : await TuBienBan(don);

            // Phí do người vận hành nhập tay chưa mang BookingId; thiếu dòng này là vi phạm khoá ngoại.
            foreach (var c in phi) { c.BookingId = don.Id; _db.Charges.Add(c); }

            var tongPhi = phi.Sum(c => c.Amount);

            // Phí trừ vào cọc. Phí lớn hơn cọc thì trừ hết cọc, phần thiếu thành khoản nợ (F2):
            // sàn không tự ứng tiền cho chủ xe phần vượt.
            var phiTruVaoCoc = Math.Min(tongPhi, don.Deposit);

            // Phần vượt cọc ghi thành nợ của khách. Chưa vào sổ cái vì chưa có đồng nào vào ví treo;
            // sổ cái chỉ ghi khi thu được, xem ThuNo.
            don.DebtAmount = tongPhi - phiTruVaoCoc;
            don.DebtPaidAt = null;

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
            var khachThue = await _db.AppUsers.FindAsync(don.RenterId);
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
                BankAccount = khachThue?.BankAccount ?? ChiTra.ChuaCoSoTaiKhoan,
                BankName = khachThue?.BankName ?? ChiTra.ChuaCoSoTaiKhoan
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

            await _tb.Gui(don.RenterId, "Đơn đã quyết toán",
                khachHoan > 0 ? $"Đơn {don.Code}: sàn sẽ hoàn {khachHoan:N0}đ vào tài khoản của bạn."
                              : $"Đơn {don.Code} đã quyết toán.", don.Code);
            await _tb.Gui(don.Car.OwnerId, "Đơn đã quyết toán",
                $"Đơn {don.Code}: bạn sẽ nhận {chuXeNhan:N0}đ.", don.Code);

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
            var hopLe = new[]
            {
                LoaiPhi.QuaGio, LoaiPhi.QuaKm, LoaiPhi.NhienLieu, LoaiPhi.HuHong,
                LoaiPhi.VeSinh, LoaiPhi.MatPhuKien, LoaiPhi.PhiCauDuong, LoaiPhi.PhiDoXe,
                LoaiPhi.NgayNghiSua
                // LoaiPhi.PhatNguoi KHÔNG ở đây: phạt nguội đi qua /late-fine sau khi đơn đã
                // đóng, không phải phí chốt lúc quyết toán bình thường.
            };
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

            // "Ngày trả không tính tiền" (xem BookingsController.KiemNgay) nghĩa là khách được
            // trả xe BẤT KỲ LÚC NÀO trong ngày EndDate — hạn chót là nửa đêm bước sang ngày
            // hôm sau, không phải 00:00 của chính ngày EndDate. Trước đây tính từ 00:00 ngày
            // EndDate nên trả xe lúc 9 giờ sáng đúng hẹn vẫn bị tính trễ 9 giờ.
            var hanTra = GioVN.LucNuaDem(don.EndDate.AddDays(1));
            var treGio = (int)Math.Max(0, Math.Ceiling((tra.CreatedAt - hanTra).TotalHours));

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
