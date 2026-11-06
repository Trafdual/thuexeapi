namespace ThueXe.Jobs
{
    /// <summary>
    /// Năm tác vụ nền, gom một chỗ. Không có chúng thì đơn treo vĩnh viễn và lịch xe bị
    /// khoá chết — lỗi kín, không ai thấy cho tới lúc chủ xe hỏi vì sao xe mình không ai
    /// đặt nữa.
    ///
    /// Bốn tác vụ chạy mỗi phút, đều là một câu truy vấn theo mốc thời gian.
    /// Một tác vụ chạy mỗi 6 giờ chỉ để cảnh báo, không đổi dữ liệu.
    /// </summary>
    public class DonTreoService : BackgroundService
    {
        private static readonly TimeSpan MoiPhut = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan MoiSauGio = TimeSpan.FromHours(6);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DonTreoService> _log;
        private DateTimeOffset _lanCanhBaoCuoi = DateTimeOffset.MinValue;

        public DonTreoService(IServiceScopeFactory scopeFactory, ILogger<DonTreoService> log)
        {
            _scopeFactory = scopeFactory;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stop)
        {
            _log.LogInformation("Tác vụ nền dọn đơn treo đã chạy, chu kỳ {Phut} phút", MoiPhut.TotalMinutes);

            while (!stop.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var tb = scope.ServiceProvider.GetRequiredService<ThongBaoService>();
                    var hoanTra = scope.ServiceProvider.GetRequiredService<HoanTraService>();

                    await HetHanChoChuXe(db, tb);
                    await HetHanChoThanhToan(db, tb, hoanTra);
                    await KhachKhongToiNhanXe(db, tb, hoanTra);
                    await TuChotSau24Gio(db, tb);
                    await NhacGiaoXe(db, tb);

                    if (DateTimeOffset.UtcNow - _lanCanhBaoCuoi >= MoiSauGio)
                    {
                        await CanhBaoQuaHan(db, tb);
                        _lanCanhBaoCuoi = DateTimeOffset.UtcNow;
                    }
                }
                catch (Exception ex)
                {
                    // Một vòng lỗi không được làm chết tác vụ — vòng sau chạy lại.
                    _log.LogError(ex, "Vòng dọn đơn treo lỗi");
                }

                await Task.Delay(MoiPhut, stop);
            }
        }

        /// B5 · Chủ xe im lặng quá 30 phút → hết hạn, nhả lịch, ghi nhận một lần lỡ hẹn.
        private async Task HetHanChoChuXe(ApplicationDbContext db, ThongBaoService tb)
        {
            var bayGio = DateTimeOffset.UtcNow;
            var don = await db.Bookings
                .Include(b => b.Car)
                .Where(b => b.Status == TrangThaiDon.ChoChuXe
                         && b.HoldExpiresAt != null && b.HoldExpiresAt < bayGio)
                .ToListAsync();

            foreach (var b in don)
            {
                b.Status = TrangThaiDon.HetHan;
                b.CancelReason = "Chủ xe không phản hồi trong 30 phút";
                b.HoldExpiresAt = null;
                await NhaLich(db, b.Id);

                var chuXe = await db.AppUsers.FindAsync(b.Car.OwnerId);
                if (chuXe is not null) chuXe.IncidentCount++;

                await tb.Gui(b.RenterId, "Đơn đã hết hạn",
                    $"Đơn {b.Code}: chủ xe không phản hồi trong 30 phút, đơn tự huỷ.", b.Code);
                _log.LogInformation("Đơn {Ma} hết hạn chờ chủ xe", b.Code);
            }
            if (don.Count > 0) await db.SaveChangesAsync();
        }

        /// C1 · Khách không chuyển trong 30 phút → hết hạn, nhả lịch NGAY để xe còn bán được.
        private async Task HetHanChoThanhToan(ApplicationDbContext db, ThongBaoService tb, HoanTraService hoanTra)
        {
            var bayGio = DateTimeOffset.UtcNow;
            var don = await db.Bookings
                .Where(b => b.Status == TrangThaiDon.ChoThanhToan
                         && b.HoldExpiresAt != null && b.HoldExpiresAt < bayGio)
                .ToListAsync();

            foreach (var b in don)
            {
                b.Status = TrangThaiDon.HetHan;
                b.CancelReason = "Khách không chuyển tiền trong 30 phút";
                b.HoldExpiresAt = null;
                await NhaLich(db, b.Id);

                // Đóng phiếu thu lại, không để nó treo mãi ở trạng thái chờ.
                var thu = await db.Payments
                    .Where(p => p.BookingId == b.Id && p.Status == TrangThaiThanhToan.Cho)
                    .ToListAsync();
                foreach (var t in thu)
                {
                    t.Status = TrangThaiThanhToan.HetHan;

                    // C2: khách chuyển THIẾU rồi bỏ ngang, không bù nốt — tiền đó có thật trong
                    // tài khoản ngân hàng của sàn nhưng chưa từng vào sổ cái (GhiTienVao cố ý
                    // không ghi khi còn thiếu). Ghi nhận nó vào ví treo rồi hoàn ngay, không thì
                    // tiền nằm im vô hình, không ai biết đường trả lại.
                    var daNhan = t.ReceivedAmount ?? 0;
                    if (daNhan <= 0) continue;

                    db.LedgerEntries.Add(new LedgerEntry
                    {
                        BookingId = b.Id, Account = TaiKhoanSoCai.Khach, Direction = Chieu.No,
                        Amount = daNhan, RefType = LoaiChungTu.ThanhToan, RefId = t.Id
                    });
                    db.LedgerEntries.Add(new LedgerEntry
                    {
                        BookingId = b.Id, Account = TaiKhoanSoCai.ViTreo, Direction = Chieu.Co,
                        Amount = daNhan, RefType = LoaiChungTu.ThanhToan, RefId = t.Id
                    });
                    await db.SaveChangesAsync();

                    await hoanTra.ChiTuViTreo(b.Id, Ben.Khach, b.RenterId, daNhan, LoaiChungTu.HoanThieuTien);
                    await tb.Gui(b.RenterId, "Đã hoàn tiền chuyển thiếu",
                        $"Đơn {b.Code} hết hạn vì chuyển thiếu và không bù kịp. Sàn sẽ hoàn lại {daNhan:N0}đ.",
                        b.Code);
                }

                _log.LogInformation("Đơn {Ma} hết hạn chờ thanh toán", b.Code);
            }
            if (don.Count > 0) await db.SaveChangesAsync();
        }

        /// D4 · Quá giờ nhận 24 giờ mà chưa có biên bản giao → huỷ.
        /// Mất tiền thuê (đền bù cho chủ xe vì xe đã bị giữ chỗ), HOÀN ĐỦ CỌC — cọc là tiền bảo
        /// đảm, không phải tiền phạt. Cả hai lệnh chi đều phải ghi sổ, không thì ví treo ôm luôn
        /// tiền thuê mà không ai biết đi đâu.
        private async Task KhachKhongToiNhanXe(ApplicationDbContext db, ThongBaoService tb, HoanTraService hoanTra)
        {
            var hanChot = GioVN.HomNay.AddDays(-1);
            var don = await db.Bookings
                .Include(b => b.Car)
                .Where(b => b.Status == TrangThaiDon.DaXacNhan && b.StartDate <= hanChot)
                .ToListAsync();

            foreach (var b in don)
            {
                var coBienBanGiao = await db.Handovers
                    .AnyAsync(h => h.BookingId == b.Id && h.Kind == LoaiBienBan.Giao);
                if (coBienBanGiao) continue;

                b.Status = TrangThaiDon.DaHuy;
                b.CancelReason = "Khách không tới nhận xe trong 24 giờ";
                await NhaLich(db, b.Id);

                await hoanTra.ChiTuViTreo(b.Id, Ben.Khach, b.RenterId, b.Deposit, LoaiChungTu.KhachKhongNhanXe);
                await hoanTra.ChiTuViTreo(b.Id, Ben.ChuXe, b.Car.OwnerId, b.RentTotal, LoaiChungTu.KhachKhongNhanXe);

                var khach = await db.AppUsers.FindAsync(b.RenterId);
                if (khach is not null) khach.IncidentCount++;

                await tb.Gui(b.RenterId, "Đơn đã huỷ vì không tới nhận xe",
                    $"Đơn {b.Code}: quá 24 giờ không tới nhận xe. Sàn hoàn {b.Deposit:N0}đ tiền cọc, " +
                    $"tiền thuê {b.RentTotal:N0}đ đền bù cho chủ xe.", b.Code);
                await tb.Gui(b.Car.OwnerId, "Khách không tới nhận xe, đơn đã huỷ",
                    $"Đơn {b.Code}: sàn sẽ chuyển {b.RentTotal:N0}đ đền bù cho bạn.", b.Code);

                _log.LogWarning("Đơn {Ma} huỷ vì khách không tới nhận xe, hoàn cọc {Coc}, đền bù {DenBu}",
                    b.Code, b.Deposit, b.RentTotal);
            }
            if (don.Count > 0) await db.SaveChangesAsync();
        }

        /// Nhắc cả hai bên khi ngày giao xe đã tới gần (còn hôm nay hoặc mai) mà đơn vẫn chưa
        /// có biên bản giao — cùng ngưỡng "sắp phải giao xe" ở màn Tổng quan của app chủ xe.
        /// Gửi đúng một lần mỗi đơn (DaNhacGiaoXe), không nhắc lại mỗi phút.
        private async Task NhacGiaoXe(ApplicationDbContext db, ThongBaoService tb)
        {
            var ngayMai = GioVN.HomNay.AddDays(1);
            var don = await db.Bookings
                .Include(b => b.Car)
                .Where(b => b.Status == TrangThaiDon.DaXacNhan && !b.DaNhacGiaoXe && b.StartDate <= ngayMai)
                .ToListAsync();

            foreach (var b in don)
            {
                var coBienBanGiao = await db.Handovers
                    .AnyAsync(h => h.BookingId == b.Id && h.Kind == LoaiBienBan.Giao);
                if (coBienBanGiao) { b.DaNhacGiaoXe = true; continue; }

                var ngay = b.StartDate == GioVN.HomNay ? "hôm nay" : "ngày mai";
                await tb.Gui(b.Car.OwnerId, "Sắp tới giờ giao xe",
                    $"Đơn {b.Code}: hẹn giao xe {ngay} ({b.StartDate:dd/MM}). Lập biên bản giao khi khách tới.", b.Code);
                await tb.Gui(b.RenterId, "Sắp tới ngày nhận xe",
                    $"Đơn {b.Code}: hẹn nhận xe {ngay} ({b.StartDate:dd/MM}). Nhớ mang CCCD và GPLX gốc.", b.Code);

                b.DaNhacGiaoXe = true;
                _log.LogInformation("Đã nhắc giao xe cho đơn {Ma}", b.Code);
            }
            if (don.Count > 0) await db.SaveChangesAsync();
        }

        /// F3 · Khách im lặng 24 giờ sau khi có biên bản trả → coi là chấp nhận, tự chốt.
        /// TRỪ KHI đang có phản đối mở (phản đối giờ tự chuyển đơn sang TRANH_CHAP, nên nhánh
        /// PhanDoi ở đây gần như không còn gặp — giữ lại cho chắc, phòng đơn cũ từ trước bản vá).
        private async Task TuChotSau24Gio(ApplicationDbContext db, ThongBaoService tb)
        {
            var hanChot = DateTimeOffset.UtcNow.AddHours(-24);
            var don = await db.Bookings
                .Where(b => b.Status == TrangThaiDon.ChoQuyetToan && b.ReadyToSettleAt == null)
                .ToListAsync();

            foreach (var b in don)
            {
                var bienBan = await db.Handovers
                    .Where(h => h.BookingId == b.Id && h.Kind == LoaiBienBan.Tra)
                    .ToListAsync();

                if (bienBan.Any(h => h.Status == TrangThaiBienBan.PhanDoi)) continue;

                var daKy = bienBan.FirstOrDefault(h => h.Status == TrangThaiBienBan.DaKy);
                if (daKy?.ReviewedAt is null || daKy.ReviewedAt > hanChot) continue;

                // Cố ý KHÔNG tự chốt tiền ở đây: quyết toán sinh bút toán và lệnh chi thật,
                // để tác vụ nền tự làm thì không ai soát. Đánh dấu MỘT LẦN để đơn nổi lên đầu
                // hàng chờ /admin/bookings?status=CHO_QUYET_TOAN, và báo người vận hành một lần
                // duy nhất — không nhắc lại mỗi phút.
                b.ReadyToSettleAt = DateTimeOffset.UtcNow;
                await tb.GuiChoVanHanh("Đơn sẵn sàng chốt quyết toán",
                    $"Đơn {b.Code}: khách đã im lặng 24 giờ, coi như đồng ý biên bản trả.", b.Code);
                _log.LogInformation("Đơn {Ma} đủ 24 giờ, cần người vận hành chốt", b.Code);
            }
            if (don.Count > 0) await db.SaveChangesAsync();
        }

        /// E7 · Quá hạn chưa trả xe, và D3 · bên soi im lặng không ký. Chạy mỗi 6 giờ nên đủ
        /// chắc chắn là bất thường thật (không phải chỉ chậm vài tiếng) — quá 2 ngày thì không
        /// còn lý do gì để đơn đứng yên mãi, chuyển hẳn sang TRANH_CHAP cho người vận hành gọi
        /// điện, thay vì chỉ ghi log rồi không ai đọc.
        private async Task CanhBaoQuaHan(ApplicationDbContext db, ThongBaoService tb)
        {
            var homNay = GioVN.HomNay;
            var quaHan = await db.Bookings
                .Where(b => b.Status == TrangThaiDon.DangThue && b.EndDate.AddDays(2) < homNay)
                .ToListAsync();
            foreach (var b in quaHan)
            {
                b.PreDisputeStatus = b.Status;
                b.Status = TrangThaiDon.TranhChap;
                b.DisputeReason = $"Quá hạn trả xe từ {b.EndDate:dd/MM} mà không có biên bản trả";
                b.DisputeOpenedAt = DateTimeOffset.UtcNow;
                b.DisputeResolvedBy = null;
                await tb.GuiChoVanHanh("Đơn quá hạn trả xe", $"Đơn {b.Code}: {b.DisputeReason}", b.Code);
                _log.LogWarning("Đơn {Ma} quá hạn trả xe từ {Ngay}, chuyển sang TRANH_CHAP",
                    b.Code, b.EndDate);
            }

            var hanSoi = DateTimeOffset.UtcNow.AddHours(-48);
            var chuaKy = await db.Handovers
                .Include(h => h.Booking)
                .Where(h => h.Status == TrangThaiBienBan.ChoSoi && h.CreatedAt < hanSoi
                         && h.Booking.Status != TrangThaiDon.TranhChap
                         && !TrangThaiDon.DaDong.Contains(h.Booking.Status))
                .ToListAsync();
            foreach (var h in chuaKy)
            {
                var b = h.Booking;
                // Bộ nhớ đệm nhận diện của EF trả về CÙNG một đối tượng đã bị vòng lặp trên sửa
                // trong bộ nhớ (dù câu SQL của vòng này lọc theo giá trị cũ chưa lưu xuống CSDL)
                // — nếu không chặn lại thì PreDisputeStatus sẽ bị ghi đè thành chính "TRANH_CHAP".
                if (b.Status == TrangThaiDon.TranhChap) continue;
                b.PreDisputeStatus = b.Status;
                b.Status = TrangThaiDon.TranhChap;
                b.DisputeReason = $"Biên bản {h.Kind} quá 48 giờ chưa ai soi";
                b.DisputeOpenedAt = DateTimeOffset.UtcNow;
                b.DisputeResolvedBy = null;
                await tb.GuiChoVanHanh("Biên bản treo quá lâu", $"Đơn {b.Code}: {b.DisputeReason}", b.Code);
                _log.LogWarning("Biên bản {Id} ({Loai}) của đơn {Don} quá 48 giờ chưa ai ký, chuyển sang TRANH_CHAP",
                    h.Id, h.Kind, h.BookingId);
            }

            if (quaHan.Count > 0 || chuaKy.Count > 0) await db.SaveChangesAsync();
        }

        /// Huỷ đơn thì xoá các dòng lịch — một câu, không có trạng thái trung gian nào để quên.
        private static async Task NhaLich(ApplicationDbContext db, long donId)
        {
            var dong = await db.CarAvailabilities.Where(a => a.BookingId == donId).ToListAsync();
            db.CarAvailabilities.RemoveRange(dong);
        }
    }
}
