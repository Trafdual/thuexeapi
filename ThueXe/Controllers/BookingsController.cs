using ThueXe.Helpers;

namespace ThueXe.Controllers
{
    [ApiController]
    [Route("bookings")]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private const int HoaHongPhanTram = 15;

        private readonly ApplicationDbContext _db;

        private readonly ThongBaoService _tb;

        public BookingsController(ApplicationDbContext db, ThongBaoService tb)
        {
            _db = db;
            _tb = tb;
        }

        private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET /bookings — đơn của tôi, phía khách
        [HttpGet]
        public async Task<ActionResult<List<BookingDto>>> MyBookings([FromQuery] string? status)
        {
            var q = _db.Bookings
                .Include(b => b.Car).ThenInclude(c => c.Photos)
                .Include(b => b.Renter)
                .Where(b => b.RenterId == UserId);

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(b => b.Status == status);

            var don = await q.OrderByDescending(b => b.CreatedAt).ToListAsync();
            return don.Select(b => b.ToDto()).ToList();
        }

        // POST /bookings/quote — tính giá, KHÔNG tạo đơn
        [HttpPost("quote")]
        public async Task<ActionResult<QuoteResult>> Quote(QuoteRequest req)
        {
            var xe = await LayXeDatDuoc(req.CarId);
            var soNgay = KiemNgay(req.StartDate, req.EndDate);

            var ban = await NgayDaBan(req.CarId, req.StartDate, req.EndDate);

            var tienThue = xe.PricePerDay * soNgay;
            return new QuoteResult(
                xe.Id, req.StartDate, req.EndDate, soNgay,
                xe.PricePerDay, tienThue, xe.Deposit,
                tienThue * HoaHongPhanTram / 100,
                tienThue + xe.Deposit,
                ban.Count == 0, ban);
        }

        // POST /bookings — tạo đơn, giữ lịch, chống hai người đặt trùng ngày
        [HttpPost]
        public async Task<ActionResult<BookingDto>> Create(CreateBookingRequest req)
        {
            var xe = await LayXeDatDuoc(req.CarId);
            var soNgay = KiemNgay(req.StartDate, req.EndDate);

            // B4: không thuê xe của chính mình.
            if (xe.OwnerId == UserId)
                throw new BizException("CAR_UNAVAILABLE", "Không thể thuê xe của chính bạn");

            // Còn nợ từ chuyến trước thì chưa đặt chuyến mới được.
            if (await _db.Bookings.AnyAsync(b => b.RenterId == UserId && b.DebtAmount > 0 && b.DebtPaidAt == null))
                throw new BizException("DEBT_OUTSTANDING", "Bạn còn khoản nợ từ chuyến trước, cần thanh toán trước khi đặt chuyến mới");

            // A1 (đã nới): cho đặt trước, giấy tờ được duyệt sau. Chủ xe chỉ nhận được đơn khi
            // khách đã có giấy tờ được duyệt, xem OwnerBookingsController.Confirm. Nhờ vậy người
            // mới đăng ký không bị chặn ngay cửa vào trong lúc chờ người vận hành duyệt.

            // A3: GPLX hết hạn thì chặn đặt đơn mới.
            var hanGplx = await _db.IdDocuments
                .Where(d => d.UserId == UserId && d.Status == TrangThaiGiayTo.Dat)
                .OrderByDescending(d => d.Id)
                .Select(d => d.GplxExpiry)
                .FirstOrDefaultAsync();
            if (hanGplx is not null && hanGplx < req.EndDate)
                throw new BizException("KYC_REQUIRED", "GPLX hết hạn trước ngày trả xe");

            var tienThue = xe.PricePerDay * soNgay;
            var don = new Booking
            {
                Code = MaDonService.Sinh(),
                CarId = xe.Id,
                RenterId = UserId,
                StartDate = req.StartDate,
                EndDate = req.EndDate,
                Days = soNgay,
                PricePerDay = xe.PricePerDay,
                RentTotal = tienThue,
                Deposit = xe.Deposit,
                Commission = tienThue * HoaHongPhanTram / 100,
                Status = TrangThaiDon.ChoChuXe,
                // Hẹn chủ xe 30 phút. CHƯA THU TIỀN ở bước này.
                HoldExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            await using var giaoDich = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.Bookings.Add(don);
                await _db.SaveChangesAsync();   // lấy id trước khi giữ lịch

                // Đừng kiểm tra rồi mới ghi — giữa hai bước đó là chỗ đơn thứ hai chen vào.
                // Dùng thẳng khoá chính (car_id, day) làm trọng tài: ai chèn được thì thắng.
                // KHOÁ CẢ NGÀY TRẢ: thuê 12→15 là khoá bốn ngày 12, 13, 14, 15. Chỉ khoá tới
                // 14 thì người khác đặt được ngày 15 trong khi xe còn chưa về.
                for (var d = req.StartDate; d <= req.EndDate; d = d.AddDays(1))
                    _db.CarAvailabilities.Add(new CarAvailability
                    {
                        CarId = xe.Id,
                        Day = d,
                        BookingId = don.Id
                    });

                await _db.SaveChangesAsync();
                await giaoDich.CommitAsync();
            }
            catch (DbUpdateException)
            {
                // Đụng khoá trùng dù chỉ một ngày là cả lệnh đổ. Giao dịch cuộn lại,
                // đơn cũng biến mất — không để lại dòng mồ côi.
                await giaoDich.RollbackAsync();
                throw new BizException("SLOT_TAKEN",
                    "Những ngày này vừa có người khác đặt mất rồi");
            }

            await _db.Entry(don).Reference(b => b.Car).LoadAsync();
            await _db.Entry(don.Car).Collection(c => c.Photos).LoadAsync();
            await _db.Entry(don).Reference(b => b.Renter).LoadAsync();

            await _tb.Gui(xe.OwnerId, "Có đơn thuê mới",
                $"{don.Renter.FullName} muốn thuê {xe.Brand} {xe.Model} từ {req.StartDate:dd/MM} đến {req.EndDate:dd/MM}. Xác nhận trong 30 phút.",
                don.Code);
            return don.ToDto();
        }

        private async Task<Car> LayXeDatDuoc(long carId)
        {
            var xe = await _db.Cars.FirstOrDefaultAsync(c => c.Id == carId)
                     ?? throw new BizException("CAR_UNAVAILABLE", "Không tìm thấy xe");

            // B7: xe bị gỡ hoặc ẩn giữa lúc khách đang đặt.
            if (xe.Status != TrangThaiXe.DangBan)
                throw new BizException("CAR_UNAVAILABLE", "Xe này đang không cho thuê");

            return xe;
        }

        /// B3: ngày quá khứ, hoặc ngày trả không sau ngày nhận → chặn ngay ở tầng dữ liệu vào.
        private static int KiemNgay(DateOnly batDau, DateOnly ketThuc)
        {
            var homNay = DateOnly.FromDateTime(DateTime.UtcNow);
            if (batDau < homNay)
                throw new BizException("INVALID_INPUT", "Không đặt được ngày trong quá khứ");
            if (ketThuc <= batDau)
                throw new BizException("INVALID_INPUT", "Ngày trả phải sau ngày nhận");

            // Ngày trả KHÔNG tính tiền: thuê 12→15 là ba ngày.
            return ketThuc.DayNumber - batDau.DayNumber;
        }

        private async Task<List<DateOnly>> NgayDaBan(long carId, DateOnly tu, DateOnly den) =>
            await _db.CarAvailabilities
                .Where(a => a.CarId == carId && a.Day >= tu && a.Day <= den)
                .Select(a => a.Day)
                .OrderBy(d => d)
                .ToListAsync();

        // GET /bookings/{id} — chi tiết đơn + thanh toán + biên bản
        [HttpGet("{khoa}")]
        public async Task<ActionResult<BookingDetailDto>> Get(string khoa)
        {
            var don = await LayDonLienQuan(khoa);

            var thanhToan = await _db.Payments
                .Where(p => p.BookingId == don.Id)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            var bienBan = await _db.Handovers
                .Include(h => h.Photos)
                .Where(h => h.BookingId == don.Id)
                .ToListAsync();

            var phi = await _db.Charges.Where(c => c.BookingId == don.Id).ToListAsync();

            var giayTo = await _db.IdDocuments
                .Where(d => d.UserId == don.RenterId)
                .OrderByDescending(d => d.Id)
                .Select(d => d.Status)
                .FirstOrDefaultAsync();

            return new BookingDetailDto(
                don.ToDto(giayTo ?? "CHUA_NOP"),
                thanhToan is null ? null : new PaymentDto(
                    thanhToan.Id, thanhToan.BookingId, thanhToan.Amount, thanhToan.TransferCode,
                    thanhToan.QrUrl, thanhToan.Status, thanhToan.ReceivedAmount,
                    thanhToan.ConfirmedAt, thanhToan.BankNote),
                bienBan.Select(h => h.ToDto()).ToList(),
                phi.Select(c => new ChargeDto(c.Id, c.Type, c.Amount, c.Note)).ToList());
        }

        // POST /bookings/{id}/cancel — nhả lịch, ghi lý do. Đơn đã thanh toán (DA_XAC_NHAN) thì
        // chia tiền thuê theo chính sách huỷ; cọc luôn hoàn đủ vì cọc là tiền bảo đảm, không phải
        // tiền phạt (cùng nguyên tắc DonTreoService dùng khi khách không tới nhận xe).
        [HttpPost("{khoa}/cancel")]
        public async Task<ActionResult<BookingDto>> Cancel(string khoa, CancelBookingRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Reason))
                throw new BizException("INVALID_INPUT", "Phải ghi lý do huỷ");

            var don = await LayDonLienQuan(khoa);

            if (TrangThaiDon.DaDong.Contains(don.Status))
                throw new BizException("WRONG_STATE", "Đơn đã đóng");

            // G4: xe đã ra khỏi tay chủ thì phải đi qua đường trả xe, không huỷ được nữa.
            var coBienBanGiao = await _db.Handovers
                .AnyAsync(h => h.BookingId == don.Id && h.Kind == LoaiBienBan.Giao);
            if (coBienBanGiao)
                throw new BizException("WRONG_STATE", "Đã có biên bản giao, phải đi qua đường trả xe");

            var laKhachHuy = UserId == don.RenterId;
            var daThanhToan = don.Status == TrangThaiDon.DaXacNhan;

            don.Status = TrangThaiDon.DaHuy;
            don.CancelReason = req.Reason;
            don.HoldExpiresAt = null;

            var dong = await _db.CarAvailabilities.Where(a => a.BookingId == don.Id).ToListAsync();
            _db.CarAvailabilities.RemoveRange(dong);

            long hoanKhach = 0, denBuChuXe = 0;
            if (daThanhToan)
                (hoanKhach, denBuChuXe) = await HoanTienHuyDon(don, laKhachHuy);

            await _db.SaveChangesAsync();

            var benKia = UserId == don.RenterId ? don.Car.OwnerId : don.RenterId;
            var noiDungHuy = $"Đơn {don.Code} đã huỷ: {req.Reason}";
            if (daThanhToan)
                noiDungHuy += hoanKhach > 0 ? $" Sàn sẽ hoàn {hoanKhach:N0}đ." : " Theo chính sách huỷ, không có khoản hoàn.";
            await _tb.Gui(benKia, "Đơn đã bị huỷ", noiDungHuy, don.Code);
            return don.ToDto();
        }

        /// Chia RentTotal + Deposit (đang nằm nguyên trong ví treo) giữa khách và chủ xe.
        /// Cọc hoàn đủ cho khách trong mọi trường hợp. Tiền thuê:
        ///  - Chủ xe huỷ: khách không có lỗi, hoàn đủ 100% tiền thuê.
        ///  - Khách huỷ: hoàn theo mốc còn lại tới ngày nhận xe — trước 7 ngày 100%, còn 3–7 ngày
        ///    70%, còn 24–72 giờ 50%, dưới 24 giờ 0%. Phần không hoàn coi là bồi thường cho chủ xe
        ///    vì xe đã bị giữ chỗ, không trừ hoa hồng sàn (hoa hồng chỉ tính khi đơn chạy trọn).
        /// Trả về (số hoàn khách, số đền bù chủ xe) để ghi vào thông báo.
        private async Task<(long HoanKhach, long DenBuChuXe)> HoanTienHuyDon(Booking don, bool laKhachHuy)
        {
            long hoanTienThue;
            if (!laKhachHuy)
            {
                hoanTienThue = don.RentTotal;
            }
            else
            {
                var conLai = don.StartDate.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow;
                var tyLe = conLai switch
                {
                    var t when t >= TimeSpan.FromDays(7) => 1.00,
                    var t when t >= TimeSpan.FromHours(72) => 0.70,
                    var t when t >= TimeSpan.FromHours(24) => 0.50,
                    _ => 0.00
                };
                hoanTienThue = (long)(don.RentTotal * tyLe);
            }

            var hoanKhach = don.Deposit + hoanTienThue;
            var denBuChuXe = don.RentTotal - hoanTienThue;

            if (hoanKhach > 0)
            {
                var khach = await _db.AppUsers.FindAsync(don.RenterId);
                var lenhHoan = new Payout
                {
                    BookingId = don.Id,
                    PayeeType = Ben.Khach,
                    PayeeId = don.RenterId,
                    BankAccount = khach?.BankAccount ?? ChiTra.ChuaCoSoTaiKhoan,
                    BankName = khach?.BankName ?? ChiTra.ChuaCoSoTaiKhoan,
                    Amount = hoanKhach,
                    Status = TrangThaiChiTra.Cho
                };
                _db.Payouts.Add(lenhHoan);
                await _db.SaveChangesAsync();
                GhiSoCancel(don.Id, TaiKhoanSoCai.Khach, hoanKhach, lenhHoan.Id);
            }
            if (denBuChuXe > 0)
            {
                var chuXe = await _db.AppUsers.FindAsync(don.Car.OwnerId);
                var lenhDenBu = new Payout
                {
                    BookingId = don.Id,
                    PayeeType = Ben.ChuXe,
                    PayeeId = don.Car.OwnerId,
                    BankAccount = chuXe?.BankAccount ?? ChiTra.ChuaCoSoTaiKhoan,
                    BankName = chuXe?.BankName ?? ChiTra.ChuaCoSoTaiKhoan,
                    Amount = denBuChuXe,
                    Status = TrangThaiChiTra.Cho
                };
                _db.Payouts.Add(lenhDenBu);
                await _db.SaveChangesAsync();
                GhiSoCancel(don.Id, TaiKhoanSoCai.ChuXe, denBuChuXe, lenhDenBu.Id);
            }
            return (hoanKhach, denBuChuXe);
        }

        /// Tiền đang nằm nguyên trong ví treo (No), trả về cho người nhận (Co) — cùng cách ghi
        /// sổ ThanhToanService dùng khi hoàn phần chuyển thừa.
        private void GhiSoCancel(long donId, string taiKhoanNhan, long soTien, long chungTuId)
        {
            _db.LedgerEntries.Add(new LedgerEntry
            {
                BookingId = donId, Account = TaiKhoanSoCai.ViTreo, Direction = Chieu.No,
                Amount = soTien, RefType = LoaiChungTu.HuyDon, RefId = chungTuId
            });
            _db.LedgerEntries.Add(new LedgerEntry
            {
                BookingId = donId, Account = taiKhoanNhan, Direction = Chieu.Co,
                Amount = soTien, RefType = LoaiChungTu.HuyDon, RefId = chungTuId
            });
        }

        // POST /bookings/{id}/handovers — bên lập nộp biên bản, vào trạng thái CHO_SOI
        [HttpPost("{khoa}/handovers")]
        public async Task<ActionResult<HandoverDto>> CreateHandover(string khoa, CreateHandoverRequest req)
        {
            var don = await LayDonLienQuan(khoa);
            var laChuXe = don.Car.OwnerId == UserId;

            if (req.Kind is not (LoaiBienBan.Giao or LoaiBienBan.Tra))
                throw new BizException("INVALID_INPUT", "Loại biên bản phải là GIAO hoặc TRA");

            // Chủ xe lập biên bản giao, khách lập biên bản trả — không ai lập thay ai.
            if (req.Kind == LoaiBienBan.Giao && !laChuXe)
                throw new BizException("FORBIDDEN", "Biên bản giao do chủ xe lập");
            if (req.Kind == LoaiBienBan.Tra && laChuXe)
                throw new BizException("FORBIDDEN", "Biên bản trả do khách lập");

            var canTrangThai = req.Kind == LoaiBienBan.Giao
                ? TrangThaiDon.DaXacNhan
                : TrangThaiDon.DangThue;
            if (don.Status != canTrangThai)
                throw new BizException("WRONG_STATE", $"Đơn đang ở {don.Status}");

            if (await _db.Handovers.AnyAsync(h => h.BookingId == don.Id && h.Kind == req.Kind))
                throw new BizException("WRONG_STATE", $"Biên bản {req.Kind} của đơn này đã lập rồi");

            // D7: thiếu ảnh hay thiếu chữ ký thì chỉ đúng ô còn thiếu, không xoá cái đã nhập.
            var anh = req.Photos ?? new List<HandoverPhotoRequest>();
            var thieu = KhungAnh.BatBuoc.Where(k => anh.All(a => a.Slot != k)).ToList();
            if (thieu.Count > 0 || string.IsNullOrWhiteSpace(req.Signature))
                throw new BizException("HANDOVER_INCOMPLETE",
                    "Còn thiếu: " + (thieu.Count > 0 ? string.Join(", ", thieu) : "chữ ký"));

            if (req.FuelLevel is < 0 or > 8)
                throw new BizException("INVALID_INPUT", "Mức nhiên liệu nằm ngoài thang 0..8");

            var ben = laChuXe ? Ben.ChuXe : Ben.Khach;
            var bienBan = new Handover
            {
                BookingId = don.Id,
                Kind = req.Kind,
                CreatedBy = ben,
                Odo = req.Odo,
                FuelLevel = req.FuelLevel,
                Note = req.Note,
                Status = TrangThaiBienBan.ChoSoi,
                SignCreator = req.Signature,
                CreatedAt = DateTimeOffset.UtcNow
            };
            foreach (var a in anh)
                bienBan.Photos.Add(new HandoverPhoto
                {
                    Slot = a.Slot,
                    Url = a.Url,
                    TakenBy = ben,
                    Note = a.Note
                });

            _db.Handovers.Add(bienBan);
            await _db.SaveChangesAsync();

            var loai = req.Kind == LoaiBienBan.Giao ? "giao xe" : "trả xe";
            await _tb.Gui(laChuXe ? don.RenterId : don.Car.OwnerId, "Cần soi biên bản " + loai,
                $"Đơn {don.Code}: kiểm tra ảnh và ký biên bản {loai}.", don.Code);
            return bienBan.ToDto();
        }

        private async Task<Booking> LayDonLienQuan(string khoa)
        {
            var don = await _db.Bookings
                .Include(b => b.Car).ThenInclude(c => c.Photos)
                .Include(b => b.Renter)
                .TheoKhoa(khoa)
                .FirstOrDefaultAsync();

            // Đơn không tồn tại và đơn của người khác phải trả về CÙNG MỘT phản hồi.
            // Phân biệt hai ca là biến API thành máy dò: gọi tuần tự /bookings/1,2,3…
            // rồi đếm xem ca nào trả FORBIDDEN là biết sàn có bao nhiêu đơn.
            if (don is null || (don.Car.OwnerId != UserId && don.RenterId != UserId))
                throw new BizException("NOT_FOUND", "Không tìm thấy đơn");

            return don;
        }
    }
}
