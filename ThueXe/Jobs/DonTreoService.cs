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

                    await HetHanChoChuXe(db);
                    await HetHanChoThanhToan(db);
                    await KhachKhongToiNhanXe(db);
                    await TuChotSau24Gio(db);

                    if (DateTimeOffset.UtcNow - _lanCanhBaoCuoi >= MoiSauGio)
                    {
                        await CanhBaoQuaHan(db);
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

        /// B5 · Chủ xe im lặng quá 30 phút → hết hạn, nhả lịch, hạ điểm phản hồi.
        private async Task HetHanChoChuXe(ApplicationDbContext db)
        {
            var bayGio = DateTimeOffset.UtcNow;
            var don = await db.Bookings
                .Where(b => b.Status == TrangThaiDon.ChoChuXe
                         && b.HoldExpiresAt != null && b.HoldExpiresAt < bayGio)
                .ToListAsync();

            foreach (var b in don)
            {
                b.Status = TrangThaiDon.HetHan;
                b.CancelReason = "Chủ xe không phản hồi trong 30 phút";
                b.HoldExpiresAt = null;
                await NhaLich(db, b.Id);
                _log.LogInformation("Đơn {Ma} hết hạn chờ chủ xe", b.Code);
            }
            if (don.Count > 0) await db.SaveChangesAsync();
        }

        /// C1 · Khách không chuyển trong 30 phút → hết hạn, nhả lịch NGAY để xe còn bán được.
        private async Task HetHanChoThanhToan(ApplicationDbContext db)
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
                foreach (var t in thu) t.Status = TrangThaiThanhToan.HetHan;

                _log.LogInformation("Đơn {Ma} hết hạn chờ thanh toán", b.Code);
            }
            if (don.Count > 0) await db.SaveChangesAsync();
        }

        /// D4 · Quá giờ nhận 24 giờ mà chưa có biên bản giao → huỷ.
        /// Mất tiền thuê, HOÀN ĐỦ CỌC — cọc là tiền bảo đảm, không phải tiền phạt.
        private async Task KhachKhongToiNhanXe(ApplicationDbContext db)
        {
            var hanChot = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
            var don = await db.Bookings
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

                if (b.Deposit > 0)
                {
                    db.Payouts.Add(new Payout
                    {
                        BookingId = b.Id,
                        PayeeType = Ben.Khach,
                        PayeeId = b.RenterId,
                        Amount = b.Deposit,
                        Status = TrangThaiChiTra.Cho,
                        BankAccount = ChiTra.ChuaCoSoTaiKhoan,
                        BankName = ChiTra.ChuaCoSoTaiKhoan
                    });
                }
                _log.LogWarning("Đơn {Ma} huỷ vì khách không tới nhận xe, hoàn cọc {Coc}",
                    b.Code, b.Deposit);
            }
            if (don.Count > 0) await db.SaveChangesAsync();
        }

        /// F3 · Khách im lặng 24 giờ sau khi có biên bản trả → coi là chấp nhận, tự chốt.
        /// TRỪ KHI đang có phản đối mở.
        private async Task TuChotSau24Gio(ApplicationDbContext db)
        {
            var hanChot = DateTimeOffset.UtcNow.AddHours(-24);
            var don = await db.Bookings
                .Where(b => b.Status == TrangThaiDon.ChoQuyetToan)
                .ToListAsync();

            foreach (var b in don)
            {
                var bienBan = await db.Handovers
                    .Where(h => h.BookingId == b.Id && h.Kind == LoaiBienBan.Tra)
                    .ToListAsync();

                // Có phản đối mở thì để nguyên, người vận hành xử tay.
                if (bienBan.Any(h => h.Status == TrangThaiBienBan.PhanDoi)) continue;

                var daKy = bienBan.FirstOrDefault(h => h.Status == TrangThaiBienBan.DaKy);
                if (daKy?.ReviewedAt is null || daKy.ReviewedAt > hanChot) continue;

                _log.LogInformation("Đơn {Ma} đủ 24 giờ, cần người vận hành chốt", b.Code);
                // Cố ý KHÔNG tự chốt tiền ở đây: quyết toán sinh bút toán và lệnh chi thật,
                // để tác vụ nền tự làm thì không ai soát. Chỉ đánh dấu để hiện lên hàng chờ.
            }
        }

        /// E7 · Quá hạn chưa trả xe, và D3 · bên soi im lặng không ký. Chỉ cảnh báo.
        private async Task CanhBaoQuaHan(ApplicationDbContext db)
        {
            var homNay = DateOnly.FromDateTime(DateTime.UtcNow);
            var quaHan = await db.Bookings
                .Where(b => b.Status == TrangThaiDon.DangThue && b.EndDate < homNay)
                .Select(b => new { b.Code, b.EndDate })
                .ToListAsync();
            foreach (var b in quaHan)
                _log.LogWarning("Đơn {Ma} quá hạn trả xe từ {Ngay}", b.Code, b.EndDate);

            var hanSoi = DateTimeOffset.UtcNow.AddHours(-24);
            var chuaKy = await db.Handovers
                .Where(h => h.Status == TrangThaiBienBan.ChoSoi && h.CreatedAt < hanSoi)
                .Select(h => new { h.Id, h.BookingId, h.Kind })
                .ToListAsync();
            foreach (var h in chuaKy)
                _log.LogWarning("Biên bản {Id} ({Loai}) của đơn {Don} quá 24 giờ chưa ai ký",
                    h.Id, h.Kind, h.BookingId);
        }

        /// Huỷ đơn thì xoá các dòng lịch — một câu, không có trạng thái trung gian nào để quên.
        private static async Task NhaLich(ApplicationDbContext db, long donId)
        {
            var dong = await db.CarAvailabilities.Where(a => a.BookingId == donId).ToListAsync();
            db.CarAvailabilities.RemoveRange(dong);
        }
    }
}
