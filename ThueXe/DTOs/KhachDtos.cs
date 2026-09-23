namespace ThueXe.Dtos
{
    public record Paged<T>(List<T> Items, int Page, int Size, int Total, int TotalPages);

    /// Bản rút gọn cho màn tìm xe. KHÔNG có pickupAddress — địa chỉ giao xe chỉ mở
    /// sau khi đơn đã xác nhận.
    public record CarSearchItemDto(
        long Id, string Plate, string Brand, string Model, int Year, int Seats,
        string Transmission, string Fuel, string District,
        long PricePerDay, int MaxKmDay, long Deposit, string? PhotoUrl);

    public record QuoteRequest(long CarId, DateOnly StartDate, DateOnly EndDate);

    public record QuoteResult(
        long CarId, DateOnly StartDate, DateOnly EndDate, int Days,
        long PricePerDay, long RentTotal, long Deposit, long Commission,
        /// Số khách phải chuyển một lần: tiền thuê cộng tiền cọc.
        long TongPhaiTra,
        bool ConTrong, List<DateOnly> NgayBanTrongKhoang);

    public record CreateBookingRequest(long CarId, DateOnly StartDate, DateOnly EndDate);
}
