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

        public ThanhToanService(ApplicationDbContext db) => _db = db;

        public static class TinhHuong
        {
            public const string DaGhiNhan = "DA_GHI_NHAN";
            public const string ThieuTien = "THIEU_TIEN";
            public const string DaXuLyTruocDo = "DA_XU_LY_TRUOC_DO";
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
                throw new BizException("WRONG_STATE", $"Đơn đang ở {don.Status}");

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
