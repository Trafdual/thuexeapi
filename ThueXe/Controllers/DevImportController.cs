using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using ThueXe.Data;
using ThueXe.Models;

namespace ThueXe.Controllers
{
    /// Nhập người dùng và xe từ file Excel, để dựng dữ liệu demo theo ý mình thay vì dữ liệu cố định của /dev/seed.
    /// Chỉ chạy ở môi trường Development. Sai một dòng là không ghi gì cả và báo rõ dòng nào sai.
    [ApiController]
    [Route("dev/import-excel")]
    [Authorize]
    public class DevImportController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly DuLieuMauService _mau;

        public DevImportController(ApplicationDbContext db, IWebHostEnvironment env, DuLieuMauService mau)
        {
            _db = db;
            _env = env;
            _mau = mau;
        }

        private static readonly string[] CotNguoiDung =
        {
            "SDT", "HoTen", "MatKhau", "Email", "LaChuXe", "NganHang", "SoTaiKhoan",
            "CCCD", "GPLX", "HangGPLX", "HanGPLX", "DuyetGiayTo"
        };

        private static readonly string[] CotXe =
        {
            "ChuXeSDT", "BienSo", "Hang", "Dong", "Nam", "SoCho", "HopSo", "NhienLieu", "Odo", "Quan",
            "DiaChiNhan", "GiaNgay", "Coc", "GioiHanKmNgay", "MoTa", "TrangThai", "Anh", "DangKy", "DangKiem", "HanDangKiem"
        };

        // GET /dev/import-excel/mau — tải file mẫu, điền sẵn dữ liệu để sửa
        [HttpGet("mau")]
        public IActionResult TaiFileMau()
        {
            if (!_env.IsDevelopment()) throw new BizException("FORBIDDEN", "Chỉ chạy được ở môi trường dev");

            using var wb = new XLWorkbook();

            var nd = wb.AddWorksheet("NguoiDung");
            Dau(nd, CotNguoiDung);
            // Demo dùng đúng hai tài khoản: chủ xe giữ tất cả xe, người thuê đặt xe.
            var chu = new[] { ("0364184928", "Phạm Quốc Bảo", "0071000123456", "Vietcombank", "079201000123", "790123456789") };
            var hang = 2;
            foreach (var (sdt, ten, tk, nh, cccd, gplx) in chu)
            {
                nd.Cell(hang, 1).SetValue(sdt); nd.Cell(hang, 2).SetValue(ten); nd.Cell(hang, 3).SetValue("test");
                nd.Cell(hang, 5).SetValue("Có"); nd.Cell(hang, 6).SetValue(nh); nd.Cell(hang, 7).SetValue(tk);
                nd.Cell(hang, 8).SetValue(cccd); nd.Cell(hang, 9).SetValue(gplx); nd.Cell(hang, 10).SetValue("B2");
                nd.Cell(hang, 11).SetValue(new DateTime(2031, 4, 18)); nd.Cell(hang, 12).SetValue("Có");
                hang++;
            }
            var khach = new[] { ("0901234567", "Nguyễn Văn An", "079201000456", "790123456456") };
            foreach (var (sdt, ten, cccd, gplx) in khach)
            {
                nd.Cell(hang, 1).SetValue(sdt); nd.Cell(hang, 2).SetValue(ten); nd.Cell(hang, 3).SetValue("test");
                nd.Cell(hang, 5).SetValue("Không"); nd.Cell(hang, 6).SetValue("Techcombank"); nd.Cell(hang, 7).SetValue("0071000654321");
                nd.Cell(hang, 8).SetValue(cccd); nd.Cell(hang, 9).SetValue(gplx); nd.Cell(hang, 10).SetValue("B2");
                nd.Cell(hang, 11).SetValue(new DateTime(2031, 4, 18)); nd.Cell(hang, 12).SetValue("Có");
                hang++;
            }
            nd.Column(1).Style.NumberFormat.Format = "@";
            nd.Column(7).Style.NumberFormat.Format = "@";
            nd.Column(8).Style.NumberFormat.Format = "@";
            nd.Column(9).Style.NumberFormat.Format = "@";
            nd.Column(11).Style.DateFormat.Format = "dd/MM/yyyy";

            var xe = wb.AddWorksheet("Xe");
            Dau(xe, CotXe);
            var mau = new (string Bien, string Hang, string Dong, int Nam, int Cho, string Hs, string Nl, int Odo, string Quan, long Gia, string Tt)[]
            {
                ("51A-123.45", "Toyota", "Vios", 2021, 5, "Số tự động", "Xăng", 42000, "Quận 1", 620000, "Đang bán"),
                ("51F-678.90", "Hyundai", "Accent", 2022, 5, "Số tự động", "Xăng", 31000, "Quận 3", 650000, "Đang bán"),
                ("51G-222.11", "Kia", "Morning", 2020, 5, "Số sàn", "Xăng", 68000, "Quận 7", 480000, "Đang bán"),
                ("51H-333.22", "Mazda", "CX-5", 2023, 5, "Số tự động", "Xăng", 18000, "Quận 2", 980000, "Đang bán"),
                ("51K-444.33", "Ford", "Ranger", 2021, 5, "Số sàn", "Dầu", 75000, "Thủ Đức", 1050000, "Đang bán"),
                ("51L-555.44", "Toyota", "Innova", 2019, 7, "Số sàn", "Xăng", 96000, "Bình Thạnh", 780000, "Đang bán"),
                ("51M-666.55", "Mitsubishi", "Xpander", 2022, 7, "Số tự động", "Xăng", 27000, "Gò Vấp", 820000, "Đang bán"),
                ("51N-777.66", "VinFast", "VF e34", 2023, 5, "Số tự động", "Điện", 12000, "Quận 1", 900000, "Đang bán"),
                ("51P-888.77", "Honda", "City", 2021, 5, "Số tự động", "Xăng", 39000, "Tân Bình", 640000, "Chờ duyệt"),
                ("51R-999.88", "Suzuki", "XL7", 2022, 7, "Số tự động", "Xăng", 33000, "Quận 10", 750000, "Chờ duyệt"),
                ("51S-101.12", "Toyota", "Fortuner", 2020, 7, "Số sàn", "Dầu", 88000, "Quận 12", 1200000, "Nháp"),
                ("51T-121.31", "Hyundai", "Santa Fe", 2023, 7, "Số tự động", "Dầu", 15000, "Phú Nhuận", 1350000, "Ẩn"),
            };
            for (var i = 0; i < mau.Length; i++)
            {
                var r = i + 2; var m = mau[i];
                xe.Cell(r, 1).SetValue("0364184928");
                xe.Cell(r, 2).SetValue(m.Bien); xe.Cell(r, 3).SetValue(m.Hang); xe.Cell(r, 4).SetValue(m.Dong);
                xe.Cell(r, 5).SetValue(m.Nam); xe.Cell(r, 6).SetValue(m.Cho); xe.Cell(r, 7).SetValue(m.Hs);
                xe.Cell(r, 8).SetValue(m.Nl); xe.Cell(r, 9).SetValue(m.Odo); xe.Cell(r, 10).SetValue(m.Quan);
                xe.Cell(r, 11).SetValue($"12 Nguyễn Huệ, {m.Quan}"); xe.Cell(r, 12).SetValue(m.Gia);
                xe.Cell(r, 13).SetValue(15000000); xe.Cell(r, 14).SetValue(300);
                xe.Cell(r, 15).SetValue($"{m.Hang} {m.Dong} {m.Nam}, {m.Cho} chỗ. Xe gia đình, mới bảo dưỡng.");
                xe.Cell(r, 16).SetValue(m.Tt);
                xe.Cell(r, 17).SetValue($"xe-{i + 1}.jpg");
                xe.Cell(r, 18).SetValue($"dang-ky-{i + 1}.jpg");
                xe.Cell(r, 19).SetValue($"dang-kiem-{i + 1}.jpg");
                xe.Cell(r, 20).SetValue(new DateTime(2027, 3, 1));
            }
            xe.Column(1).Style.NumberFormat.Format = "@";
            xe.Column(20).Style.DateFormat.Format = "dd/MM/yyyy";

            var hd = wb.AddWorksheet("HuongDan");
            var dong = new[]
            {
                "HƯỚNG DẪN NHẬP",
                "Dòng 1 của mỗi sheet là tên cột, không đổi. Dữ liệu bắt đầu từ dòng 2. Dòng trống bỏ qua.",
                "Sai một dòng là không nhập gì cả; kết quả báo rõ sheet và dòng sai.",
                "",
                "Sheet NguoiDung",
                "SDT: số điện thoại 10 số, bắt đầu bằng 0. Đã có thì cập nhật, chưa có thì tạo mới.",
                "MatKhau, HoTen: chỉ dùng khi tạo mới; tài khoản đã có thì giữ nguyên tên và mật khẩu. MatKhau để trống thì dùng 'test'.",
                "LaChuXe: Có / Không. Chủ xe được tạo sẵn bản cam kết chủ xe.",
                "NganHang, SoTaiKhoan: nơi nhận tiền. Chủ xe nên điền.",
                "CCCD, GPLX, HangGPLX, HanGPLX: điền thì tạo hồ sơ giấy tờ. DuyetGiayTo = Có thì hồ sơ ở trạng thái đã duyệt, không thì chờ duyệt.",
                "",
                "Sheet Xe",
                "ChuXeSDT: SDT của chủ xe (demo: 0364184928), phải có trong sheet NguoiDung hoặc đã có trong hệ thống.",
                "Demo dùng hai tài khoản, mật khẩu test: chủ xe 0364184928 và người thuê 0901234567.",
                "BienSo: khoá để cập nhật. Trùng biển số thì sửa xe đó.",
                "HopSo: Số tự động / Số sàn. NhienLieu: Xăng / Dầu / Điện.",
                "TrangThai: Nháp / Chờ duyệt / Đang bán / Ẩn. Chỉ xe Đang bán mới hiện cho người thuê.",
                "Coc: tiền cọc (đồng). Để trống thì 15.000.000. GioiHanKmNgay để trống thì 300.",
                "Anh, DangKy, DangKiem: tên file, nhiều file cách nhau bằng dấu ; hoặc dùng đường dẫn http.",
                "Tên file phải nằm trong thư mục SeedAssets/mau của backend (hoặc uploads). Ví dụ xe-1.jpg.",
                "Muốn dùng ảnh của bạn: bỏ ảnh vào SeedAssets/mau rồi ghi tên file vào cột Anh.",
            };
            for (var i = 0; i < dong.Length; i++) hd.Cell(i + 1, 1).SetValue(dong[i]);
            hd.Cell(1, 1).Style.Font.Bold = true; hd.Cell(5, 1).Style.Font.Bold = true; hd.Cell(12, 1).Style.Font.Bold = true;
            hd.Column(1).Width = 120;

            foreach (var ws in new[] { nd, xe }) ws.Columns().AdjustToContents();
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "du-lieu-mau.xlsx");
        }

        private static void Dau(IXLWorksheet ws, string[] cot)
        {
            for (var i = 0; i < cot.Length; i++)
            {
                var c = ws.Cell(1, i + 1);
                c.SetValue(cot[i]);
                c.Style.Font.Bold = true;
                c.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8E9F5");
            }
            ws.SheetView.FreezeRows(1);
        }

        // POST /dev/import-excel?xoaCu=true — form-data: file=.xlsx
        [HttpPost]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> Nhap(IFormFile file, [FromQuery] bool xoaCu = false)
        {
            if (!_env.IsDevelopment()) throw new BizException("FORBIDDEN", "Chỉ chạy được ở môi trường dev");
            if (file is null || file.Length == 0) throw new BizException("INVALID_INPUT", "Chưa chọn file Excel");

            XLWorkbook wb;
            try
            {
                await using var luong = file.OpenReadStream();
                wb = new XLWorkbook(luong);
            }
            catch (Exception)
            {
                throw new BizException("INVALID_INPUT", "Không đọc được file. Cần file .xlsx (Excel), không phải .xls hay .csv");
            }

            using (wb)
            {
                var loi = new List<string>();
                _mau.ChepAnhMau();

                await using var giaoDich = await _db.Database.BeginTransactionAsync();
                if (xoaCu) await _mau.XoaXeVaDon();

                var soNguoi = 0;
                var soXe = 0;
                var nguoi = new Dictionary<string, AppUser>();

                if (wb.TryGetWorksheet("NguoiDung", out var wsN))
                    foreach (var row in HangDuLieu(wsN, CotNguoiDung, loi, "NguoiDung", out var chiSoN))
                    {
                        var d = row;
                        var sdt = Chuoi(d, "SDT");
                        var ten = Chuoi(d, "HoTen");
                        if (!System.Text.RegularExpressions.Regex.IsMatch(sdt, @"^0\d{9}$"))
                        { loi.Add($"NguoiDung dòng {d["#"]}: SDT '{sdt}' phải gồm 10 số và bắt đầu bằng 0"); continue; }
                        if (ten == "") { loi.Add($"NguoiDung dòng {d["#"]}: thiếu HoTen"); continue; }

                        var u = await _db.AppUsers.FirstOrDefaultAsync(x => x.Phone == sdt);
                        if (u is null)
                        {
                            u = new AppUser
                            {
                                Phone = sdt,
                                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Chuoi(d, "MatKhau") is { Length: > 0 } mk ? mk : "test"),
                                FullName = ten,
                                Status = "HOAT_DONG",
                                CreatedAt = DateTimeOffset.UtcNow
                            };
                            _db.AppUsers.Add(u);
                        }
                        // Tài khoản đã có thì giữ nguyên tên hiện tại, không đổi tên người đang dùng.
                        if (Chuoi(d, "Email") != "") u.Email = Chuoi(d, "Email");
                        if (Chuoi(d, "NganHang") != "") u.BankName = Chuoi(d, "NganHang");
                        if (Chuoi(d, "SoTaiKhoan") != "") u.BankAccount = Chuoi(d, "SoTaiKhoan");
                        u.IsOwner = Co(Chuoi(d, "LaChuXe"));
                        await _db.SaveChangesAsync();
                        nguoi[sdt] = u;
                        soNguoi++;

                        if (u.IsOwner && !await _db.OwnerAgreements.AnyAsync(a => a.OwnerId == u.Id && a.Version == "1.0"))
                            _db.OwnerAgreements.Add(new OwnerAgreement
                            {
                                OwnerId = u.Id, Version = "1.0",
                                CccdNo = Chuoi(d, "CCCD") is { Length: > 0 } c ? c : "079200000000",
                                BankAccount = u.BankAccount ?? "0000000000",
                                BankName = u.BankName ?? "Vietcombank",
                                SignatureUrl = "/files/mau/chu-ky.jpg",
                                AcceptedAt = DateTimeOffset.UtcNow
                            });

                        if ((Chuoi(d, "CCCD") != "" || Chuoi(d, "GPLX") != "") &&
                            !await _db.IdDocuments.AnyAsync(x => x.UserId == u.Id))
                        {
                            var duyet = Co(Chuoi(d, "DuyetGiayTo"));
                            _db.IdDocuments.Add(new IdDocument
                            {
                                UserId = u.Id,
                                CccdNo = NullNeuRong(Chuoi(d, "CCCD")),
                                GplxNo = NullNeuRong(Chuoi(d, "GPLX")),
                                GplxClass = NullNeuRong(Chuoi(d, "HangGPLX")),
                                GplxExpiry = Ngay(d, "HanGPLX"),
                                FrontUrl = "/files/mau/cccd-truoc.jpg",
                                BackUrl = "/files/mau/cccd-sau.jpg",
                                Status = duyet ? "DAT" : "CHO_DUYET",
                                ReviewedAt = duyet ? DateTimeOffset.UtcNow : null
                            });
                        }
                        await _db.SaveChangesAsync();
                    }

                if (wb.TryGetWorksheet("Xe", out var wsX))
                    foreach (var d in HangDuLieu(wsX, CotXe, loi, "Xe", out _))
                    {
                        var dong = d["#"];
                        var sdt = Chuoi(d, "ChuXeSDT");
                        AppUser? chu = nguoi.GetValueOrDefault(sdt)
                                       ?? await _db.AppUsers.FirstOrDefaultAsync(x => x.Phone == sdt);
                        if (chu is null) { loi.Add($"Xe dòng {dong}: không có chủ xe SDT '{sdt}' (thêm vào sheet NguoiDung)"); continue; }

                        var bien = Chuoi(d, "BienSo").ToUpperInvariant();
                        if (bien == "") { loi.Add($"Xe dòng {dong}: thiếu BienSo"); continue; }
                        var hang = Chuoi(d, "Hang"); var dm = Chuoi(d, "Dong");
                        if (hang == "" || dm == "") { loi.Add($"Xe dòng {dong}: thiếu Hang hoặc Dong"); continue; }

                        var nam = So(d, "Nam"); var cho = So(d, "SoCho"); var gia = So(d, "GiaNgay");
                        if (nam is null or < 1990 or > 2100) { loi.Add($"Xe dòng {dong}: Nam '{Chuoi(d, "Nam")}' không hợp lệ"); continue; }
                        if (cho is null or < 2 or > 16) { loi.Add($"Xe dòng {dong}: SoCho '{Chuoi(d, "SoCho")}' không hợp lệ"); continue; }
                        if (gia is null or <= 0) { loi.Add($"Xe dòng {dong}: GiaNgay phải là số dương"); continue; }

                        var hs = ChuanHopSo(Chuoi(d, "HopSo"));
                        if (hs is null) { loi.Add($"Xe dòng {dong}: HopSo '{Chuoi(d, "HopSo")}' phải là Số tự động hoặc Số sàn"); continue; }
                        var nl = ChuanNhienLieu(Chuoi(d, "NhienLieu"));
                        if (nl is null) { loi.Add($"Xe dòng {dong}: NhienLieu '{Chuoi(d, "NhienLieu")}' phải là Xăng, Dầu hoặc Điện"); continue; }
                        var tt = ChuanTrangThai(Chuoi(d, "TrangThai"));
                        if (tt is null) { loi.Add($"Xe dòng {dong}: TrangThai '{Chuoi(d, "TrangThai")}' phải là Nháp, Chờ duyệt, Đang bán hoặc Ẩn"); continue; }

                        var anh = DuongDanAnh(d, "Anh", dong, loi);
                        var dangKy = DuongDanAnh(d, "DangKy", dong, loi);
                        var dangKiem = DuongDanAnh(d, "DangKiem", dong, loi);

                        var c = await _db.Cars.Include(x => x.Photos).Include(x => x.Documents)
                            .FirstOrDefaultAsync(x => x.Plate == bien);
                        if (c is null)
                        {
                            c = new Car { Plate = bien, CreatedAt = DateTimeOffset.UtcNow };
                            _db.Cars.Add(c);
                        }
                        c.OwnerId = chu.Id;
                        c.Brand = hang; c.Model = dm; c.Year = (int)nam.Value; c.Seats = (int)cho.Value;
                        c.Transmission = hs; c.Fuel = nl; c.Odo = (int)(So(d, "Odo") ?? 0);
                        c.District = Chuoi(d, "Quan") is { Length: > 0 } q ? q : "Quận 1";
                        c.PickupAddress = Chuoi(d, "DiaChiNhan") is { Length: > 0 } dc ? dc : $"12 Nguyễn Huệ, {c.District}";
                        c.PricePerDay = gia.Value;
                        c.Deposit = So(d, "Coc") ?? 15_000_000;
                        c.MaxKmDay = (int)(So(d, "GioiHanKmNgay") ?? 300);
                        c.Description = NullNeuRong(Chuoi(d, "MoTa"));
                        c.Status = tt;
                        c.RejectReason = tt == "AN" ? "Ẩn bởi người vận hành" : null;

                        if (anh.Count > 0)
                        {
                            _db.CarPhotos.RemoveRange(c.Photos); c.Photos.Clear();
                            for (var i = 0; i < anh.Count; i++) c.Photos.Add(new CarPhoto { Url = anh[i], SortOrder = i });
                        }
                        if (dangKy.Count > 0 || dangKiem.Count > 0)
                        {
                            _db.CarDocuments.RemoveRange(c.Documents); c.Documents.Clear();
                            foreach (var u in dangKy) c.Documents.Add(new CarDocument { Type = "DANG_KY", Url = u });
                            var han = Ngay(d, "HanDangKiem");
                            foreach (var u in dangKiem) c.Documents.Add(new CarDocument { Type = "DANG_KIEM", Url = u, ExpiryDate = han });
                        }
                        soXe++;
                    }

                if (wb.Worksheets.All(w => w.Name != "NguoiDung" && w.Name != "Xe"))
                    loi.Add("File không có sheet NguoiDung hoặc Xe. Tải file mẫu về để có đúng tên sheet.");

                if (loi.Count > 0)
                {
                    await giaoDich.RollbackAsync();
                    throw new BizException("IMPORT_FAILED",
                        $"Có {loi.Count} lỗi, chưa nhập gì cả:\n" + string.Join("\n", loi.Take(30)) +
                        (loi.Count > 30 ? $"\n… và {loi.Count - 30} lỗi nữa" : ""));
                }

                await _db.SaveChangesAsync();
                await giaoDich.CommitAsync();
                return Ok(new { nguoiDung = soNguoi, xe = soXe, daXoaDuLieuCu = xoaCu });
            }
        }

        // ---- đọc sheet ----

        /// Trả từng dòng dữ liệu thành từ điển cột→giá trị, kèm khoá "#" là số dòng trong Excel.
        private static List<Dictionary<string, string>> HangDuLieu(IXLWorksheet ws, string[] cot, List<string> loi,
                                                                    string ten, out int soDong)
        {
            var vi = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var dauDong = ws.Row(1);
            foreach (var c in dauDong.CellsUsed()) vi[c.GetString().Trim()] = c.Address.ColumnNumber;

            foreach (var bat in cot.Take(cot.Contains("SDT") ? 2 : 4))
                if (!vi.ContainsKey(bat)) loi.Add($"Sheet {ten}: thiếu cột '{bat}' ở dòng 1");

            var kq = new List<Dictionary<string, string>>();
            var cuoi = ws.LastRowUsed()?.RowNumber() ?? 1;
            for (var r = 2; r <= cuoi; r++)
            {
                var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["#"] = r.ToString() };
                var coGiaTri = false;
                foreach (var k in cot)
                {
                    var v = vi.TryGetValue(k, out var col) ? Text(ws.Cell(r, col)) : "";
                    d[k] = v;
                    if (v != "") coGiaTri = true;
                }
                if (coGiaTri) kq.Add(d);
            }
            soDong = kq.Count;
            return kq;
        }

        private static string Text(IXLCell c)
        {
            if (c.IsEmpty()) return "";
            if (c.DataType == XLDataType.DateTime) return c.GetDateTime().ToString("yyyy-MM-dd");
            if (c.DataType == XLDataType.Number)
            {
                var n = c.GetDouble();
                return n == Math.Floor(n) ? ((long)n).ToString(CultureInfo.InvariantCulture)
                                          : n.ToString(CultureInfo.InvariantCulture);
            }
            return c.GetString().Trim();
        }

        private static string Chuoi(Dictionary<string, string> d, string k) => d.GetValueOrDefault(k)?.Trim() ?? "";
        private static string? NullNeuRong(string s) => s == "" ? null : s;

        private static long? So(Dictionary<string, string> d, string k)
        {
            var s = Chuoi(d, k);
            if (s == "") return null;
            var so = new string(s.Where(char.IsDigit).ToArray());   // chấp nhận "620.000", "620,000"
            return long.TryParse(so, out var v) ? v : null;
        }

        private static DateOnly? Ngay(Dictionary<string, string> d, string k)
        {
            var s = Chuoi(d, k);
            if (s == "") return null;
            foreach (var f in new[] { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy" })
                if (DateOnly.TryParseExact(s, f, CultureInfo.InvariantCulture, DateTimeStyles.None, out var v)) return v;
            return null;
        }

        private static bool Co(string s) =>
            new[] { "1", "x", "co", "có", "yes", "true", "y" }.Contains(BoDau(s));

        private static string BoDau(string s)
        {
            var n = s.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
            return new string(n.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray())
                .Replace("_", " ");
        }

        private static string? ChuanHopSo(string s) => BoDau(s) switch
        {
            "so tu dong" or "tu dong" or "auto" or "at" => "SO_TU_DONG",
            "so san" or "san" or "manual" or "mt" => "SO_SAN",
            _ => null
        };

        private static string? ChuanNhienLieu(string s) => BoDau(s) switch
        {
            "xang" => "XANG",
            "dau" or "diesel" => "DAU",
            "dien" => "DIEN",
            _ => null
        };

        private static string? ChuanTrangThai(string s) => BoDau(s) switch
        {
            "" or "nhap" => "NHAP",
            "cho duyet" => "CHO_DUYET",
            "dang ban" or "dang cho thue" => "DANG_BAN",
            "an" => "AN",
            _ => null
        };

        private List<string> DuongDanAnh(Dictionary<string, string> d, string cot, string dong, List<string> loi)
        {
            var kq = new List<string>();
            foreach (var t in Chuoi(d, cot).Split(new[] { ';', '\n', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (t.StartsWith("http://") || t.StartsWith("https://") || t.StartsWith("/")) { kq.Add(t); continue; }
                if (t.Contains('/') || t.Contains("..")) { loi.Add($"Xe dòng {dong}: cột {cot} '{t}' chỉ ghi tên file"); continue; }
                if (_mau.CoAnhMau(t)) kq.Add("/files/mau/" + t);
                else if (System.IO.File.Exists(Path.Combine(_env.ContentRootPath, "uploads", t))) kq.Add("/files/" + t);
                else loi.Add($"Xe dòng {dong}: cột {cot} không thấy file '{t}' (bỏ vào SeedAssets/mau của backend)");
            }
            return kq;
        }
    }
}
