namespace ThueXe.Dtos
{
    // ── Duyệt giấy tờ ─────────────────────────────────────────────────
    public record NguoiDungTomTat(long Id, string FullName, string Phone, string? Email);

    public record IdDocumentAdminDto(
        long Id, NguoiDungTomTat User,
        string? CccdNo, string? GplxNo, string? GplxClass, DateOnly? GplxExpiry,
        string FrontUrl, string BackUrl, string? SelfieUrl,
        string Status, string? RejectReason, DateTimeOffset? ReviewedAt);

    public record ReviewRequest(bool Approved, string? Reason);

    public record ReviewResult(long Id, string Status, string? RejectReason, DateTimeOffset? ReviewedAt);

    // ── Duyệt xe ──────────────────────────────────────────────────────
    /// Kèm CCCD chủ xe để người vận hành đối chiếu với tên trên đăng ký xe (case A5).
    public record ChuXeTomTat(long Id, string FullName, string Phone, string? CccdNo);

    public record CarAdminDto(
        long Id, ChuXeTomTat Owner, string Plate, string Brand, string Model,
        int Year, int Seats, string Transmission, string Fuel, int Odo,
        string District, string PickupAddress, long PricePerDay, int MaxKmDay,
        long Deposit, string? Description, string Status, string? RejectReason,
        DateTimeOffset CreatedAt,
        List<CarPhotoDto> Photos, List<CarDocumentDto> Documents);

    // ── Tiền vào ──────────────────────────────────────────────────────
    public record ConfirmPaymentRequest(long ReceivedAmount, string? BankNote);

    public record ConfirmPaymentResult(
        PaymentDto Payment, BookingDto Booking,
        /// Khớp đủ tiền hay không. Thiếu thì Booking đứng nguyên, web phải tô cảnh báo.
        bool KhopSoTien, long LechSoTien,
        /// Chuyển thừa thì sinh thêm lệnh hoàn phần thừa.
        PayoutDto? RefundPayout,
        /// false = người vận hành không dán nội dung chuyển khoản, máy không đối chiếu được.
        bool DaDoiChieuNoiDung = false);

    // ── Quyết toán ────────────────────────────────────────────────────
    public record ChargeRequest(string Type, long Amount, string? Note);

    public record SettleRequest(List<ChargeRequest>? Charges);

    public record LedgerEntryDto(
        long Id, string Account, string Direction, long Amount,
        string RefType, long RefId, DateTimeOffset OccurredAt);

    public record SettleResult(
        BookingDto Booking, List<ChargeDto> Charges,
        List<PayoutDto> Payouts, List<LedgerEntryDto> Ledger);

    // ── Chi trả ───────────────────────────────────────────────────────
    public record PayoutAdminDto(
        long Id, long BookingId, string? BookingCode, string PayeeType,
        NguoiDungTomTat Payee, string BankAccount, string BankName,
        long Amount, string Status, string? TransferRef,
        DateTimeOffset? PaidAt, DateTimeOffset CreatedAt);

    public record MarkPaidRequest(string TransferRef);

    // ── Sổ cái ────────────────────────────────────────────────────────
    public record LedgerResult(
        long BookingId, List<LedgerEntryDto> Entries,
        long TongNo, long TongCo, bool CanBang);

    /// Thân thông báo tiền về. Đặt tên trường theo cái mà SePay và Casso gửi ra,
    /// để sau này cắm dịch vụ thật vào là chạy, không phải sửa.
    public record BankWebhookRequest(
        long Amount,
        string? Content,
        string TransferRef,
        DateTimeOffset? OccurredAt);

    public record BankWebhookResult(
        string TinhHuong,
        long? PaymentId,
        long? BookingId,
        string? BookingStatus,
        string? BookingCode,
        string Loi);

    /// Kết quả đối soát cuối ngày. Lệch dương là có tiền vào chưa ghi; lệch âm nguy hiểm
    /// hơn: sổ nói còn tiền mà tài khoản không có.
    public record DoiSoatResult(
        DateTimeOffset TinhDenLuc,
        long TongDaThu,
        long TongDaChi,
        long SoDuKyVong,
        long? SoDuNganHang,
        long? Lech,
        long ViTreoTheoSoCai,
        long ConPhaiChi,
        bool Dat,
        string Loi);
}
