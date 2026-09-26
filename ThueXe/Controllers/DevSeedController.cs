using ThueXe.Helpers;

namespace ThueXe.Controllers
{
    /// Dữ liệu mẫu cho môi trường dev: 12 xe, 6 đơn ở các trạng thái khác nhau, 2 lệnh chi.
    /// Chỉ chạy khi ASPNETCORE_ENVIRONMENT=Development. Gọi lại thì xoá sạch rồi dựng lại.
    [ApiController]
    [Route("dev/seed")]
    [Authorize]
    public class DevSeedController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly QrService _qr;

        public DevSeedController(ApplicationDbContext db, IWebHostEnvironment env, QrService qr)
        {
            _db = db;
            _env = env;
            _qr = qr;
        }

        private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private record MauXe(
            string Bien, string Hang, string Dong, int Nam, int Cho,
            string HopSo, string NhienLieu, int Odo, string Quan, long Gia, string TrangThai);

        private static readonly MauXe[] DanhSachXe =
        {
            new("51A-123.45", "Toyota", "Vios", 2021, 5, "SO_TU_DONG", "XANG", 42000, "Quận 1", 620_000, "DANG_BAN"),
            new("51F-678.90", "Hyundai", "Accent", 2022, 5, "SO_TU_DONG", "XANG", 31000, "Quận 3", 650_000, "DANG_BAN"),
            new("51G-222.11", "Kia", "Morning", 2020, 5, "SO_SAN", "XANG", 68000, "Quận 7", 480_000, "DANG_BAN"),
            new("51H-333.22", "Mazda", "CX-5", 2023, 5, "SO_TU_DONG", "XANG", 18000, "Quận 2", 980_000, "DANG_BAN"),
            new("51K-444.33", "Ford", "Ranger", 2021, 5, "SO_SAN", "DAU", 75000, "Thủ Đức", 1_050_000, "DANG_BAN"),
            new("51L-555.44", "Toyota", "Innova", 2019, 7, "SO_SAN", "XANG", 96000, "Bình Thạnh", 780_000, "DANG_BAN"),
            new("51M-666.55", "Mitsubishi", "Xpander", 2022, 7, "SO_TU_DONG", "XANG", 27000, "Gò Vấp", 820_000, "DANG_BAN"),
            new("51N-777.66", "VinFast", "VF e34", 2023, 5, "SO_TU_DONG", "DIEN", 12000, "Quận 1", 900_000, "DANG_BAN"),
            new("51P-888.77", "Honda", "City", 2021, 5, "SO_TU_DONG", "XANG", 39000, "Tân Bình", 640_000, "CHO_DUYET"),
            new("51R-999.88", "Suzuki", "XL7", 2022, 7, "SO_TU_DONG", "XANG", 33000, "Quận 10", 750_000, "CHO_DUYET"),
            new("51S-101.12", "Toyota", "Fortuner", 2020, 7, "SO_SAN", "DAU", 88000, "Quận 12", 1_200_000, "NHAP"),
            new("51T-121.31", "Hyundai", "Santa Fe", 2023, 7, "SO_TU_DONG", "DAU", 15000, "Phú Nhuận", 1_350_000, "AN"),
        };

        private record MauDon(string Ma, int XeThu, string TenKhach, string Sdt, int CachHomNay, int SoNgay, string TrangThai);

        private static readonly MauDon[] DanhSachDon =
        {
            new("KNM4F2C9", 1, "Nguyễn Văn An", "0901234567", 2, 3, "CHO_CHU_XE"),
            new("KNM7H3K2", 4, "Trần Thị Bình", "0912345678", 5, 2, "CHO_CHU_XE"),
            new("KNM9P4M5", 2, "Lê Hoàng Cường", "0923456789", 1, 4, "CHO_THANH_TOAN"),
            new("KNM2R5N8", 6, "Phạm Minh Dũng", "0934567890", 0, 3, "DA_XAC_NHAN"),
            new("KNM5T6Q3", 3, "Vũ Thu Hà", "0945678901", -2, 5, "DANG_THUE"),
            new("KNM8W7Z4", 5, "Đỗ Quang Huy", "0956789012", -9, 4, "HOAN_TAT"),
        };

        /// Tra số tiền của một phiếu thu theo mã đơn. Chỉ để script giả lập ngân hàng
        /// biết phải bắn bao nhiêu — coi như khách quét mã QR, không sửa được số tiền.
        [HttpGet("/dev/so-tien-phieu-thu/{ma}")]
        [AllowAnonymous]
        public async Task<ActionResult<long>> SoTienPhieuThu(string ma)
        {
            if (!_env.IsDevelopment())
                throw new BizException("FORBIDDEN", "Chỉ chạy được ở môi trường dev");

            var thu = await _db.Payments.FirstOrDefaultAsync(p => p.TransferCode == ma)
                      ?? throw new BizException("NOT_FOUND", $"Không có phiếu thu mang mã {ma}");
            return thu.Amount;
        }

        [HttpPost]
        public async Task<IActionResult> Seed()
        {
            if (!_env.IsDevelopment())
                throw new BizException("FORBIDDEN", "Chỉ chạy được ở môi trường dev");

            var chuXe = await _db.AppUsers.FindAsync(UserId)
                        ?? throw new BizException("USER_NOT_FOUND");

            await XoaDuLieuCu();

            chuXe.IsOwner = true;
            // Ở môi trường dev, người chạy seed kiêm luôn vai vận hành để thử các đường /admin/**.
            chuXe.Role = Vai.VanHanh;
            if (!await _db.OwnerAgreements.AnyAsync(a => a.OwnerId == UserId && a.Version == "1.0"))
            {
                _db.OwnerAgreements.Add(new OwnerAgreement
                {
                    OwnerId = UserId,
                    Version = "1.0",
                    CccdNo = "079201000123",
                    BankAccount = "0071000123456",
                    BankName = "Vietcombank",
                    SignatureUrl = "/files/mau/chu-ky.jpg"
                });
            }

            if (!await _db.IdDocuments.AnyAsync(d => d.UserId == UserId))
            {
                _db.IdDocuments.Add(new IdDocument
                {
                    UserId = UserId,
                    CccdNo = "079201000123",
                    GplxNo = "790123456789",
                    GplxClass = "B2",
                    GplxExpiry = new DateOnly(2031, 4, 18),
                    FrontUrl = "/files/mau/cccd-truoc.jpg",
                    BackUrl = "/files/mau/cccd-sau.jpg",
                    Status = "DAT",
                    ReviewedAt = DateTimeOffset.UtcNow
                });
            }

            var xe = new List<Car>();
            for (var i = 0; i < DanhSachXe.Length; i++)
            {
                var m = DanhSachXe[i];
                var c = new Car
                {
                    OwnerId = UserId,
                    Plate = BienTheoNguoi(m.Bien, i),
                    Brand = m.Hang,
                    Model = m.Dong,
                    Year = m.Nam,
                    Seats = m.Cho,
                    Transmission = m.HopSo,
                    Fuel = m.NhienLieu,
                    Odo = m.Odo,
                    District = m.Quan,
                    PickupAddress = $"12 Nguyễn Huệ, {m.Quan}",
                    PricePerDay = m.Gia,
                    Description = $"{m.Hang} {m.Dong} {m.Nam}, {m.Cho} chỗ. Xe gia đình, mới bảo dưỡng.",
                    Status = m.TrangThai,
                    RejectReason = m.TrangThai == "AN" ? "Ảnh đăng kiểm mờ, chụp lại giúp sàn" : null
                };
                c.Photos.Add(new CarPhoto { Url = $"/files/mau/xe-{i + 1}.jpg", SortOrder = 0 });
                c.Documents.Add(new CarDocument { Type = "DANG_KY", Url = $"/files/mau/dang-ky-{i + 1}.jpg" });
                c.Documents.Add(new CarDocument
                {
                    Type = "DANG_KIEM",
                    Url = $"/files/mau/dang-kiem-{i + 1}.jpg",
                    ExpiryDate = new DateOnly(2027, 3, 1)
                });
                xe.Add(c);
                _db.Cars.Add(c);
            }
            await _db.SaveChangesAsync();

            var homNay = DateOnly.FromDateTime(DateTime.UtcNow);
            var don = new List<Booking>();
            foreach (var m in DanhSachDon)
            {
                var khach = await LayHoacTaoKhach(m.TenKhach, m.Sdt);
                var c = xe[m.XeThu - 1];
                var tienThue = c.PricePerDay * m.SoNgay;

                var b = new Booking
                {
                    Code = MaDonTheoNguoi(Array.IndexOf(DanhSachDon, m)),
                    CarId = c.Id,
                    RenterId = khach.Id,
                    StartDate = homNay.AddDays(m.CachHomNay),
                    EndDate = homNay.AddDays(m.CachHomNay + m.SoNgay),
                    Days = m.SoNgay,
                    PricePerDay = c.PricePerDay,
                    RentTotal = tienThue,
                    Deposit = c.Deposit,
                    Commission = tienThue * 15 / 100,
                    Status = m.TrangThai,
                    HoldExpiresAt = m.TrangThai is "CHO_CHU_XE" or "CHO_THANH_TOAN"
                        ? DateTimeOffset.UtcNow.AddMinutes(30)
                        : null
                };
                _db.Bookings.Add(b);
                don.Add(b);
            }
            await _db.SaveChangesAsync();

            // Đơn còn sống thì giữ lịch — khoá CẢ ngày trả, không phải nửa khoảng.
            foreach (var b in don.Where(b => !TrangThaiDon.DaDong.Contains(b.Status)))
            {
                for (var d = b.StartDate; d <= b.EndDate; d = d.AddDays(1))
                    _db.CarAvailabilities.Add(new CarAvailability
                    {
                        CarId = b.CarId,
                        Day = d,
                        BookingId = b.Id
                    });
            }

            // Đơn nào đã qua bước nhận đơn thì phải có phiếu thu, không thì màn xác nhận
            // tiền của quản trị không có gì để bấm.
            foreach (var b in don.Where(b => b.Status is "CHO_THANH_TOAN" or "DA_XAC_NHAN"
                                                      or "DANG_THUE" or "CHO_QUYET_TOAN"))
            {
                var soTien = b.RentTotal + b.Deposit;
                _db.Payments.Add(new Payment
                {
                    BookingId = b.Id,
                    Amount = soTien,
                    TransferCode = b.Code,
                    QrUrl = _qr.TaoUrl(b.Code, soTien),
                    Status = b.Status == "CHO_THANH_TOAN"
                        ? TrangThaiThanhToan.Cho
                        : TrangThaiThanhToan.DaNhan,
                    ReceivedAmount = b.Status == "CHO_THANH_TOAN" ? null : soTien,
                    ConfirmedAt = b.Status == "CHO_THANH_TOAN" ? null : DateTimeOffset.UtcNow
                });
            }

            var daXong = don.Single(b => b.Status == "HOAN_TAT");
            var dangChay = don.Single(b => b.Status == "DANG_THUE");
            _db.Payouts.Add(new Payout
            {
                BookingId = daXong.Id,
                PayeeType = Ben.ChuXe,
                PayeeId = UserId,
                BankAccount = "0071000123456",
                BankName = "Vietcombank",
                Amount = daXong.RentTotal - daXong.Commission,
                Status = "DA_CHI",
                TransferRef = "FT2609210012345",
                PaidAt = DateTimeOffset.UtcNow
            });
            _db.Payouts.Add(new Payout
            {
                BookingId = dangChay.Id,
                PayeeType = Ben.ChuXe,
                PayeeId = UserId,
                BankAccount = "0071000123456",
                BankName = "Vietcombank",
                Amount = dangChay.RentTotal - dangChay.Commission,
                Status = "CHO"
            });

            // Biên bản. Không có thì màn soi ảnh của app chủ xe không có gì để mở,
            // mà đơn DANG_THUE hay HOAN_TAT lại vô lý vì đã qua bước giao xe.
            var soBienBan = 0;

            // Đơn đang thuê: xe đã giao xong, khách vừa nộp biên bản TRA và đang chờ chủ xe soi.
            // Đây đúng là trạng thái màn "Soi biên bản trả xe" cần để thử.
            soBienBan += ThemBienBan(dangChay, LoaiBienBan.Giao, Ben.ChuXe, TrangThaiBienBan.DaKy,
                odo: 52_000, nhienLieu: 8, ghiChu: "Xe sạch, có vết xước nhẹ cửa sau bên trái");
            soBienBan += ThemBienBan(dangChay, LoaiBienBan.Tra, Ben.Khach, TrangThaiBienBan.ChoSoi,
                odo: 52_340, nhienLieu: 5, ghiChu: "Trả đúng giờ, xăng còn 5 vạch");

            // Đơn đã xong: đủ hai biên bản đã ký, kèm phí phát sinh để màn quyết toán có số.
            soBienBan += ThemBienBan(daXong, LoaiBienBan.Giao, Ben.ChuXe, TrangThaiBienBan.DaKy,
                odo: 88_000, nhienLieu: 8, ghiChu: "Giao đủ đồ nghề, lốp dự phòng còn mới");
            soBienBan += ThemBienBan(daXong, LoaiBienBan.Tra, Ben.Khach, TrangThaiBienBan.DaKy,
                odo: 89_620, nhienLieu: 6, ghiChu: "Trả muộn 2 giờ, đã báo trước");

            _db.Charges.Add(new Charge
            {
                BookingId = daXong.Id,
                Type = LoaiPhi.QuaKm,
                Amount = 420_000,
                Note = "Vượt 84 km so với hạn mức"
            });
            _db.Charges.Add(new Charge
            {
                BookingId = daXong.Id,
                Type = LoaiPhi.NhienLieu,
                Amount = 240_000,
                Note = "Thiếu 2 vạch xăng"
            });

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Đã dựng dữ liệu mẫu",
                xe = xe.Count,
                don = don.Count,
                bienBan = soBienBan,
                lenhChi = 2
            });
        }

        /// Mã đơn cũng có ràng buộc duy nhất toàn bảng như biển số. Sinh theo người gọi
        /// và thứ tự đơn để mỗi người một dải riêng, mà seed lại vẫn ra mã cũ.
        /// Dùng đúng bảng chữ của MaDonService: bỏ 0 O 1 I L cho khỏi nhầm khi gõ chuyển khoản.
        private string MaDonTheoNguoi(int thuTu)
        {
            const string bangChu = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
            var so = UserId * 100 + thuTu;
            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < 6; i++)
            {
                sb.Insert(0, bangChu[(int)(so % bangChu.Length)]);
                so /= bangChu.Length;
            }
            return "KNM" + sb;
        }

        /// Dựng một biên bản đủ sáu khung ảnh. Bên lập ký sẵn; bên soi chỉ ký khi đã DA_KY,
        /// còn CHO_SOI thì để trống chữ ký người soi đúng như lúc chờ duyệt thật.
        private int ThemBienBan(Booking b, string loai, string benLap, string trangThai,
            int odo, int nhienLieu, string ghiChu)
        {
            var bb = new Handover
            {
                BookingId = b.Id,
                Kind = loai,
                CreatedBy = benLap,
                Odo = odo,
                FuelLevel = nhienLieu,
                Note = ghiChu,
                Status = trangThai,
                SignCreator = "/files/mau/chu-ky.jpg",
                SignReviewer = trangThai == TrangThaiBienBan.DaKy ? "/files/mau/chu-ky.jpg" : null,
                ReviewedAt = trangThai == TrangThaiBienBan.DaKy ? DateTimeOffset.UtcNow : null
            };
            foreach (var khung in KhungAnh.BatBuoc)
                bb.Photos.Add(new HandoverPhoto
                {
                    Slot = khung,
                    Url = $"/files/mau/bien-ban-{khung.ToLowerInvariant()}.jpg",
                    TakenBy = benLap
                });
            _db.Handovers.Add(bb);
            return 1;
        }

        /// Biển số có ràng buộc duy nhất toàn bảng, trong khi seed chỉ xoá xe của người gọi.
        /// Nhiều người cùng seed trên một backend dùng chung thì biển mẫu sẽ đụng nhau,
        /// nên gắn mã người dùng vào biển để mỗi người một dải riêng.
        private string BienTheoNguoi(string bienMau, int thuTu)
        {
            var dauSo = bienMau.Split('-')[0];           // giữ "51A", "51F"...
            return $"{dauSo}-{UserId % 1000:000}.{thuTu + 1:00}";
        }

        private async Task XoaDuLieuCu()
        {
            var xeCu = await _db.Cars.Where(c => c.OwnerId == UserId).Select(c => c.Id).ToListAsync();
            var donCu = await _db.Bookings.Where(b => xeCu.Contains(b.CarId)).Select(b => b.Id).ToListAsync();

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

        private async Task<AppUser> LayHoacTaoKhach(string ten, string sdt)
        {
            var khach = await _db.AppUsers.FirstOrDefaultAsync(u => u.Phone == sdt);
            if (khach is not null) return khach;

            khach = new AppUser
            {
                Phone = sdt,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("test"),
                FullName = ten,
                Status = "HOAT_DONG",
                // Có sẵn nơi nhận tiền hoàn, để luồng quyết toán không vướng CHUA_CO.
                BankAccount = "0" + sdt[1..],
                BankName = "Vietcombank"
            };
            _db.AppUsers.Add(khach);
            await _db.SaveChangesAsync();

            _db.IdDocuments.Add(new IdDocument
            {
                UserId = khach.Id,
                FrontUrl = "/files/mau/cccd-truoc.jpg",
                BackUrl = "/files/mau/cccd-sau.jpg",
                Status = "DAT"
            });
            await _db.SaveChangesAsync();
            return khach;
        }
    }
}
