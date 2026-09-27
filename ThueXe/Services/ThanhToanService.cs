namespace ThueXe.Services
{
    /// <summary>
    /// Luật ghi tiền vào ví treo. Tách khỏi controller vì có hai đường cùng gọi:
    /// người vận hành bấm tay, và webhook ngân hàng bắn về. Hai bản sao của luật xử tiền
    /// là thứ chắc chắn lệch nhau sau vài lần sửa.
    /// </summary>
    public class ThanhToanService
    {
        private readonly ApplicationDbContext _db;

        private readonly ThongBaoService _tb;

        public ThanhToanService(ApplicationDbContext db, ThongBaoService tb)
        {
            _db = db;
            _tb = tb;
        }

        public static class TinhHuong
        {
            public const string DaGhiNhan = "DA_GHI_NHAN";
            public const string ThieuTien = "THIEU_TIEN";
            public const string DaXuLyTruocDo = "DA_XU_LY_TRUOC_DO";
            /// C6: tiền về sau khi đơn đã đóng (hết hạn giữ chỗ, đã huỷ…) — không còn xe nào để
            /// giao, sàn không giữ đồng nào của khách nên hoàn ngay toàn bộ.
            public const string DonDaDongHoanLai = "DON_DA_DONG_HOAN_LAI";
        }

        public record KetQua(
            Payment Thu,
            Booking Don,
            bool KhopSoTien,
            long LechSoTien,
            Payout? HoanThua,
            string TinhHuongXuLy);

        /// <param name="nguoiXacNhan">Id người vận hành, hoặc null khi webhook tự ghi.</param>
        public async Task<KetQua> GhiTienVao(Payment thu, long soThucNhan, string? ghiChuNganHang,
                                             long? nguoiXacNhan)
        {
            var don = thu.Booking;

            // soThucNhan là số tiền của LẦN CHUYỂN NÀY, không phải tổng. Khách chuyển
            // thiếu rồi bù thêm thì cộng dồn, nếu không luồng "gọi khách chuyển bù"
            // không bao giờ tới đích: lần bù nhỏ hơn sẽ ghi đè lần đầu.
            var daNhanTruocDo = thu.ReceivedAmount ?? 0;
            var tongDaNhan = daNhanTruocDo + soThucNhan;
            var lech = tongDaNhan - thu.Amount;

            // Bắn lại cùng một giao dịch thì không ghi đè, cũng không ném lỗi — trả lại
            // nguyên trạng để bên gọi biết là đã xử rồi.
            if (thu.Status == TrangThaiThanhToan.DaNhan)
                return new KetQua(thu, don, KhopSoTien: thu.ReceivedAmount == thu.Amount,
                    LechSoTien: (thu.ReceivedAmount ?? 0) - thu.Amount,
                    HoanThua: null, TinhHuongXuLy: TinhHuong.DaXuLyTruocDo);

            thu.ReceivedAmount = tongDaNhan;
            // Nối thêm chứ không ghi đè: giữ dấu vết mọi lần chuyển để đối soát sao kê.
            thu.BankNote = string.IsNullOrWhiteSpace(thu.BankNote)
                ? ghiChuNganHang
                : $"{thu.BankNote} | {ghiChuNganHang}";

            // C2: chuyển THIẾU thì KHÔNG tự xác nhận. Ghi lại số thực nhận để người vận hành
            // gọi khách chuyển bù; đơn đứng nguyên ở CHO_THANH_TOAN.
            if (lech < 0)
            {
                await _db.SaveChangesAsync();
                return new KetQua(thu, don, KhopSoTien: false, LechSoTien: lech,
                    HoanThua: null, TinhHuongXuLy: TinhHuong.ThieuTien);
            }

            if (don.Status != TrangThaiDon.ChoThanhToan)
            {
                if (!TrangThaiDon.DaDong.Contains(don.Status))
                    throw new BizException("WRONG_STATE", $"Đơn đang ở {don.Status}");

                // C6: đơn đã đóng trước khi tiền kịp về — ghi nhận tiền vào ví treo rồi hoàn lại
                // NGAY toàn bộ, không chờ người vận hành nhớ ra. Việc của người vận hành chỉ còn
                // là gọi khách xin lỗi.
                thu.Status = TrangThaiThanhToan.DaNhan;
                thu.ConfirmedBy = nguoiXacNhan;
                thu.ConfirmedAt = DateTimeOffset.UtcNow;
                GhiSo(don.Id, TaiKhoanSoCai.Khach, Chieu.No, tongDaNhan, LoaiChungTu.ThanhToan, thu.Id);
                GhiSo(don.Id, TaiKhoanSoCai.ViTreo, Chieu.Co, tongDaNhan, LoaiChungTu.ThanhToan, thu.Id);

                var khachTre = await _db.AppUsers.FindAsync(don.RenterId);
                var hoanTraTre = new Payout
                {
                    BookingId = don.Id,
                    PayeeType = Ben.Khach,
                    PayeeId = don.RenterId,
                    BankAccount = khachTre?.BankAccount ?? ChiTra.ChuaCoSoTaiKhoan,
                    BankName = khachTre?.BankName ?? ChiTra.ChuaCoSoTaiKhoan,
                    Amount = tongDaNhan,
                    Status = TrangThaiChiTra.Cho
                };
                _db.Payouts.Add(hoanTraTre);
                await _db.SaveChangesAsync();   // lấy Id lệnh hoàn trước khi ghi sổ

                GhiSo(don.Id, TaiKhoanSoCai.ViTreo, Chieu.No, tongDaNhan, LoaiChungTu.ChiTra, hoanTraTre.Id);
                GhiSo(don.Id, TaiKhoanSoCai.Khach, Chieu.Co, tongDaNhan, LoaiChungTu.ChiTra, hoanTraTre.Id);
                await _db.SaveChangesAsync();

                await _tb.Gui(don.RenterId, "Đã nhận tiền trễ, sẽ hoàn lại",
                    $"Đơn {don.Code} đã đóng trước khi tiền về. Sàn sẽ hoàn {tongDaNhan:N0}đ vào tài khoản của bạn.",
                    don.Code);

                return new KetQua(thu, don, KhopSoTien: false, LechSoTien: lech,
                    HoanThua: hoanTraTre, TinhHuongXuLy: TinhHuong.DonDaDongHoanLai);
            }

            thu.Status = TrangThaiThanhToan.DaNhan;
            thu.ConfirmedBy = nguoiXacNhan;
            thu.ConfirmedAt = DateTimeOffset.UtcNow;

            // Tiền vào ví treo: bút toán kép, chỉ ghi thêm. Ghi TỔNG THỰC NHẬN chứ không
            // ghi số phải thu — sổ cái phải khớp tiền thật nằm trong tài khoản ngân hàng.
            GhiSo(don.Id, TaiKhoanSoCai.Khach, Chieu.No, tongDaNhan, LoaiChungTu.ThanhToan, thu.Id);
            GhiSo(don.Id, TaiKhoanSoCai.ViTreo, Chieu.Co, tongDaNhan, LoaiChungTu.ThanhToan, thu.Id);

            // Đơn sang đã xác nhận: từ đây địa chỉ giao xe và số điện thoại mở cho hai bên.
            don.Status = TrangThaiDon.DaXacNhan;
            don.HoldExpiresAt = null;

            // C3: chuyển THỪA thì vẫn xác nhận đơn, sinh thêm lệnh hoàn phần thừa cho khách.
            Payout? hoanThua = null;
            if (lech > 0)
            {
                var khach = await _db.AppUsers.FindAsync(don.RenterId);
                hoanThua = new Payout
                {
                    BookingId = don.Id,
                    PayeeType = Ben.Khach,
                    PayeeId = don.RenterId,
                    BankAccount = khach?.BankAccount ?? ChiTra.ChuaCoSoTaiKhoan,
                    BankName = khach?.BankName ?? ChiTra.ChuaCoSoTaiKhoan,
                    Amount = lech,
                    Status = TrangThaiChiTra.Cho
                };
                _db.Payouts.Add(hoanThua);
                await _db.SaveChangesAsync();   // lấy Id lệnh hoàn trước khi ghi sổ

                // Phần thừa là nghĩa vụ phải trả lại, không phải tiền của ví treo.
                // Thiếu cặp bút toán này thì sổ cái vẫn báo cân trong khi sàn giữ dư.
                GhiSo(don.Id, TaiKhoanSoCai.ViTreo, Chieu.No, lech, LoaiChungTu.ChiTra, hoanThua.Id);
                GhiSo(don.Id, TaiKhoanSoCai.Khach, Chieu.Co, lech, LoaiChungTu.ChiTra, hoanThua.Id);
            }

            await _db.SaveChangesAsync();

            var chuXeId = await _db.Cars.Where(c => c.Id == don.CarId).Select(c => c.OwnerId).FirstAsync();
            await _tb.Gui(don.RenterId, "Đã nhận tiền", $"Đơn {don.Code} đã được xác nhận thanh toán.", don.Code);
            await _tb.Gui(chuXeId, "Khách đã thanh toán", $"Đơn {don.Code}: khách đã chuyển tiền, chuẩn bị giao xe.", don.Code);
            return new KetQua(thu, don, KhopSoTien: lech == 0, LechSoTien: lech,
                HoanThua: hoanThua, TinhHuongXuLy: TinhHuong.DaGhiNhan);
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
