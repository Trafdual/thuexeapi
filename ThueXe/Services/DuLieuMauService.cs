using ThueXe.Data;

namespace ThueXe.Services
{
    /// Việc dùng chung cho các đường nạp dữ liệu mẫu ở môi trường dev.
    public class DuLieuMauService
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public DuLieuMauService(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        /// Chép ảnh mẫu (SeedAssets/mau, nằm trong git) vào uploads/mau để URL /files/mau/... mở được.
        public void ChepAnhMau()
        {
            var nguon = Path.Combine(_env.ContentRootPath, "SeedAssets", "mau");
            var dich = Path.Combine(_env.ContentRootPath, "uploads", "mau");
            if (!Directory.Exists(nguon)) return;
            Directory.CreateDirectory(dich);
            foreach (var f in Directory.GetFiles(nguon))
                System.IO.File.Copy(f, Path.Combine(dich, Path.GetFileName(f)), overwrite: true);
        }

        /// Xoá mọi tài khoản ngoài các số điện thoại được giữ, cùng giấy tờ, cam kết chủ xe, mã thiết bị
        /// và file ảnh đã tải lên (uploads, private-uploads; ảnh mẫu trong uploads/mau được giữ).
        /// Gọi sau XoaXeVaDon vì các bảng xe và đơn trỏ tới người dùng.
        public async Task<int> XoaNguoiKhac(string[] giuSdt)
        {
            var bo = await _db.AppUsers.Where(u => !giuSdt.Contains(u.Phone)).Select(u => u.Id).ToListAsync();

            _db.IdDocuments.RemoveRange(await _db.IdDocuments.Where(d => bo.Contains(d.UserId)).ToListAsync());
            _db.OwnerAgreements.RemoveRange(await _db.OwnerAgreements.Where(a => bo.Contains(a.OwnerId)).ToListAsync());
            _db.DeviceTokens.RemoveRange(await _db.DeviceTokens.Where(t => bo.Contains(t.UserId)).ToListAsync());
            _db.AppUsers.RemoveRange(await _db.AppUsers.Where(u => bo.Contains(u.Id)).ToListAsync());
            await _db.SaveChangesAsync();

            var uploads = Path.Combine(_env.ContentRootPath, "uploads");
            if (Directory.Exists(uploads))
                foreach (var f in Directory.GetFiles(uploads)) System.IO.File.Delete(f);
            var rieng = Path.Combine(_env.ContentRootPath, "private-uploads");
            if (Directory.Exists(rieng))
                foreach (var f in Directory.GetFiles(rieng)) System.IO.File.Delete(f);

            return bo.Count;
        }

        public bool CoAnhMau(string ten) =>
            System.IO.File.Exists(Path.Combine(_env.ContentRootPath, "SeedAssets", "mau", ten));

        public async Task XoaXeVaDon()
        {
            // Xoá toàn bộ xe và đơn trong hệ thống (không chỉ của người gọi), để không sót dữ liệu
            // tay/dữ liệu cũ khiến danh sách lẫn xe không có ảnh. Tài khoản người dùng được giữ.
            var xeCu = await _db.Cars.Select(c => c.Id).ToListAsync();
            var donCu = await _db.Bookings.Select(b => b.Id).ToListAsync();

            _db.Payouts.RemoveRange(await _db.Payouts.Where(p => donCu.Contains(p.BookingId)).ToListAsync());
            _db.Charges.RemoveRange(await _db.Charges.Where(c => donCu.Contains(c.BookingId)).ToListAsync());
            _db.LedgerEntries.RemoveRange(await _db.LedgerEntries.Where(l => donCu.Contains(l.BookingId)).ToListAsync());
            _db.Payments.RemoveRange(await _db.Payments.Where(p => donCu.Contains(p.BookingId)).ToListAsync());

            var bienBanCu = await _db.Handovers.Where(h => donCu.Contains(h.BookingId)).ToListAsync();
            _db.HandoverPhotos.RemoveRange(
                await _db.HandoverPhotos.Where(p => bienBanCu.Select(h => h.Id).Contains(p.HandoverId)).ToListAsync());
            _db.Handovers.RemoveRange(bienBanCu);

            _db.CarAvailabilities.RemoveRange(await _db.CarAvailabilities.Where(a => xeCu.Contains(a.CarId)).ToListAsync());
            _db.Bookings.RemoveRange(await _db.Bookings.Where(b => donCu.Contains(b.Id)).ToListAsync());
            _db.CarPhotos.RemoveRange(await _db.CarPhotos.Where(p => xeCu.Contains(p.CarId)).ToListAsync());
            _db.CarDocuments.RemoveRange(await _db.CarDocuments.Where(d => xeCu.Contains(d.CarId)).ToListAsync());
            _db.Cars.RemoveRange(await _db.Cars.Where(c => xeCu.Contains(c.Id)).ToListAsync());

            await _db.SaveChangesAsync();
        }

    }
}
