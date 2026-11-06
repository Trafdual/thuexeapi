namespace ThueXe.Services
{
    /// <summary>
    /// Sinh một lệnh chi từ ví treo cho một bên, kèm bút toán kép. Bốn nơi từng chép tay việc
    /// này (huỷ đơn, khách không tới nhận xe, biên bản giao bị phản đối, xử tranh chấp) và một
    /// trong số đó quên luôn phần lấy số tài khoản chủ xe từ bản cam kết — gom một chỗ để sửa
    /// một lần, đúng cho cả bốn.
    /// </summary>
    public class HoanTraService
    {
        private readonly ApplicationDbContext _db;

        public HoanTraService(ApplicationDbContext db) => _db = db;

        /// <param name="loaiChungTu">Một hằng số trong LoaiChungTu, để soi lại vì sao có lệnh chi này.</param>
        /// <returns>null nếu amount &lt;= 0 — không sinh lệnh chi rỗng.</returns>
        public async Task<Payout?> ChiTuViTreo(
            long donId, string payeeType, long payeeId, long amount, string loaiChungTu)
        {
            if (amount <= 0) return null;

            var (taiKhoan, tenNganHang) = await LayTaiKhoanNhan(payeeType, payeeId);

            var lenh = new Payout
            {
                BookingId = donId,
                PayeeType = payeeType,
                PayeeId = payeeId,
                Amount = amount,
                Status = TrangThaiChiTra.Cho,
                BankAccount = taiKhoan,
                BankName = tenNganHang
            };
            _db.Payouts.Add(lenh);
            await _db.SaveChangesAsync();   // lấy Id lệnh chi trước khi ghi sổ

            var taiKhoanBen = payeeType == Ben.ChuXe ? TaiKhoanSoCai.ChuXe : TaiKhoanSoCai.Khach;
            _db.LedgerEntries.Add(new LedgerEntry
            {
                BookingId = donId, Account = TaiKhoanSoCai.ViTreo, Direction = Chieu.No,
                Amount = amount, RefType = loaiChungTu, RefId = lenh.Id
            });
            _db.LedgerEntries.Add(new LedgerEntry
            {
                BookingId = donId, Account = taiKhoanBen, Direction = Chieu.Co,
                Amount = amount, RefType = loaiChungTu, RefId = lenh.Id
            });
            await _db.SaveChangesAsync();

            return lenh;
        }

        /// Chủ xe lấy số tài khoản từ bản cam kết đã ký, không phải bảng người dùng — trước đây
        /// một nhánh hoàn tiền quên mất điều này nên luôn luôn ghi "CHUA_CO" cho chủ xe dù họ đã
        /// ký cam kết đầy đủ.
        private async Task<(string TaiKhoan, string NganHang)> LayTaiKhoanNhan(string payeeType, long payeeId)
        {
            if (payeeType == Ben.ChuXe)
            {
                var camKet = await _db.OwnerAgreements
                    .Where(a => a.OwnerId == payeeId)
                    .OrderByDescending(a => a.Id)
                    .FirstOrDefaultAsync();
                return camKet is null
                    ? (ChiTra.ChuaCoSoTaiKhoan, ChiTra.ChuaCoSoTaiKhoan)
                    : (camKet.BankAccount, camKet.BankName);
            }

            var nguoiNhan = await _db.AppUsers.FindAsync(payeeId);
            return nguoiNhan?.BankAccount is null
                ? (ChiTra.ChuaCoSoTaiKhoan, ChiTra.ChuaCoSoTaiKhoan)
                : (nguoiNhan.BankAccount, nguoiNhan.BankName ?? ChiTra.ChuaCoSoTaiKhoan);
        }
    }
}
