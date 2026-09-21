namespace ThueXe.Dtos
{
    // ── Xe ────────────────────────────────────────────────────────────
    public record CarPhotoDto(long Id, string Url, int SortOrder);

    public record CarDocumentDto(long Id, string Type, string Url, DateOnly? ExpiryDate);

    public record CarDto(
        long Id, long OwnerId, string Plate, string Brand, string Model,
        int Year, int Seats, string Transmission, string Fuel, int Odo,
        string District, string? PickupAddress, long PricePerDay, int MaxKmDay,
        long Deposit, string? Description, string Status, string? RejectReason,
        DateTimeOffset CreatedAt,
        List<CarPhotoDto> Photos, List<CarDocumentDto> Documents);

    public record CarDocumentRequest(string Type, string Url, DateOnly? ExpiryDate);

    public record CarUpsertRequest(
        string Plate, string Brand, string Model, int Year, int Seats,
        string Transmission, string Fuel, int Odo, string District,
        string PickupAddress, long PricePerDay, int MaxKmDay, long Deposit,
        string? Description, List<string>? PhotoUrls, List<CarDocumentRequest>? Documents);

    public record CalendarUpdateRequest(List<DateOnly> Days, bool Blocked);

    public record AvailabilityDto(
        long CarId, DateOnly From, DateOnly To,
        List<DateOnly> BusyDays, List<DateOnly> BlockedDays);

    // ── Cam kết chủ xe ────────────────────────────────────────────────
    public record AgreementInfoDto(
        string Version, string Content, bool Signed, DateTimeOffset? SignedAt);

    public record AcceptAgreementRequest(
        string Version, string CccdNo, string BankAccount, string BankName, string SignatureUrl);

    // ── Đơn ───────────────────────────────────────────────────────────
    public record CarBriefDto(long Id, string Plate, string Brand, string Model, string? PhotoUrl);

    public record RenterBriefDto(long Id, string FullName, string? Phone, string? IdDocumentStatus);

    public record BookingDto(
        long Id, string Code, long CarId, CarBriefDto? Car, RenterBriefDto? Renter,
        DateOnly StartDate, DateOnly EndDate, int Days,
        long PricePerDay, long RentTotal, long Deposit, long Commission,
        string Status, DateTimeOffset? HoldExpiresAt, string? CancelReason,
        DateTimeOffset CreatedAt);

    public record PaymentDto(
        long Id, long BookingId, long Amount, string TransferCode, string QrUrl,
        string Status, long? ReceivedAmount, DateTimeOffset? ConfirmedAt, string? BankNote);

    public record ChargeDto(long Id, string Type, long Amount, string? Note);

    public record BookingDetailDto(
        BookingDto Booking, PaymentDto? Payment,
        List<HandoverDto> Handovers, List<ChargeDto> Charges);

    public record RejectBookingRequest(string Reason);

    public record CancelBookingRequest(string Reason);

    // ── Biên bản ──────────────────────────────────────────────────────
    public record HandoverPhotoDto(long Id, string Slot, string Url, string? TakenBy, string? Note);

    public record HandoverDto(
        long Id, long BookingId, string Kind, string CreatedBy, int Odo, int FuelLevel,
        string? Note, string Status, string? SignCreator, string? SignReviewer,
        string? Objection, DateTimeOffset? ReviewedAt, DateTimeOffset CreatedAt,
        List<HandoverPhotoDto> Photos);

    public record HandoverPhotoRequest(string Slot, string Url, string? Note);

    public record CreateHandoverRequest(
        string Kind, int Odo, int FuelLevel, string? Note,
        string Signature, List<HandoverPhotoRequest> Photos);

    public record AddHandoverPhotosRequest(List<HandoverPhotoRequest> Photos);

    public record ReviewHandoverRequest(bool Agreed, string? Objection, string? Signature);

    // ── Chi trả ───────────────────────────────────────────────────────
    public record PayoutDto(
        long Id, long BookingId, string? BookingCode, string PayeeType,
        string BankAccount, string BankName, long Amount, string Status,
        string? TransferRef, DateTimeOffset? PaidAt, DateTimeOffset CreatedAt);

    // ── Tin tức ───────────────────────────────────────────────────────
    // NGOÀI §04: bảng API của kế hoạch không có GET /news. App chủ xe cần màn tin tức
    // nên tạm để ở đây; nhóm chốt lại rồi hoặc đưa vào docs/api.md hoặc bỏ màn đó.
    public record NewsItemDto(
        long Id, string Tag, string Title, string Summary, string? ImageUrl, DateOnly? PublishedAt);
}
