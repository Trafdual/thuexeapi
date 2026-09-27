using ThueXe.Common;
using ThueXe.Data;
using ThueXe.Services;

namespace ThueXe.Jobs
{
    /// <summary>
    /// Đồ án chưa nối cổng thanh toán thật và không phải lúc nào cũng có người vận hành ngồi
    /// bấm xác nhận tay. Tác vụ này đứng vào đúng chỗ một cổng thanh toán tự động (VNPay,
    /// MoMo…) sẽ đứng: sau khi phiếu thu được sinh ra ít lâu, tự coi như khách đã chuyển đủ
    /// tiền và gọi đúng <see cref="ThanhToanService.GhiTienVao"/> — vẫn sinh sổ cái, vẫn đi
    /// qua ví treo, chỉ khác webhook thật ở chỗ không có ngân hàng nào báo về thật.
    ///
    /// Xác nhận tay trên web quản trị vẫn dùng được bình thường, chạy trước tác vụ này thì
    /// tác vụ này thấy phiếu thu đã "DA_NHAN" và bỏ qua.
    /// </summary>
    public class TuDongXacNhanThanhToanService : BackgroundService
    {
        private static readonly TimeSpan ChuKy = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan DoTre = TimeSpan.FromSeconds(15);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TuDongXacNhanThanhToanService> _log;

        public TuDongXacNhanThanhToanService(IServiceScopeFactory scopeFactory,
                                             ILogger<TuDongXacNhanThanhToanService> log)
        {
            _scopeFactory = scopeFactory;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stop)
        {
            _log.LogInformation(
                "Tự động xác nhận thanh toán đã chạy — giả lập cổng thanh toán, độ trễ {Giay}s",
                DoTre.TotalSeconds);

            while (!stop.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var thanhToan = scope.ServiceProvider.GetRequiredService<ThanhToanService>();

                    var han = DateTimeOffset.UtcNow - DoTre;
                    var phieuThu = await db.Payments
                        .Include(p => p.Booking)
                        .Where(p => p.Status == TrangThaiThanhToan.Cho && p.CreatedAt <= han)
                        .ToListAsync();

                    foreach (var thu in phieuThu)
                    {
                        try
                        {
                            await thanhToan.GhiTienVao(thu, thu.Amount,
                                "Tự động xác nhận (giả lập cổng thanh toán)", nguoiXacNhan: null);
                            _log.LogInformation("Đã tự xác nhận phiếu thu cho đơn {Ma}", thu.Booking.Code);
                        }
                        catch (BizException ex)
                        {
                            // Đơn đã đổi trạng thái ở chỗ khác giữa lúc đọc và lúc ghi (ví dụ đã
                            // huỷ) — bỏ qua phiếu này, không phải lỗi hệ thống.
                            _log.LogWarning("Bỏ qua tự xác nhận đơn {Ma}: {Loi}", thu.Booking.Code, ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Vòng tự xác nhận thanh toán lỗi");
                }

                await Task.Delay(ChuKy, stop);
            }
        }
    }
}
