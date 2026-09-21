namespace ThueXe.Helpers
{
    /// Đổi entity sang DTO. Giữ một chỗ để tên trường trên dây không trôi mỗi nơi một kiểu.
    public static class Mapping
    {
        public static CarDto ToDto(this Car c) => new(
            c.Id, c.OwnerId, c.Plate, c.Brand, c.Model, c.Year, c.Seats,
            c.Transmission, c.Fuel, c.Odo, c.District, c.PickupAddress,
            c.PricePerDay, c.MaxKmDay, c.Deposit, c.Description, c.Status,
            c.RejectReason, c.CreatedAt,
            c.Photos.OrderBy(p => p.SortOrder)
                .Select(p => new CarPhotoDto(p.Id, p.Url, p.SortOrder)).ToList(),
            c.Documents.Select(d => new CarDocumentDto(d.Id, d.Type, d.Url, d.ExpiryDate)).ToList());

        public static CarBriefDto ToBrief(this Car c) => new(
            c.Id, c.Plate, c.Brand, c.Model,
            c.Photos.OrderBy(p => p.SortOrder).Select(p => p.Url).FirstOrDefault());

        /// Số điện thoại khách chỉ mở sau khi đơn đã xác nhận — trước đó chủ xe chưa cần gọi ai.
        public static RenterBriefDto ToBrief(this AppUser u, string bookingStatus, string? docStatus)
        {
            var moSoDienThoai = bookingStatus is not ("CHO_CHU_XE" or "CHO_THANH_TOAN");
            return new RenterBriefDto(u.Id, u.FullName, moSoDienThoai ? u.Phone : null, docStatus);
        }

        public static BookingDto ToDto(this Booking b, string? renterDocStatus = null) => new(
            b.Id, b.Code, b.CarId,
            b.Car is null ? null : b.Car.ToBrief(),
            b.Renter is null ? null : b.Renter.ToBrief(b.Status, renterDocStatus),
            b.StartDate, b.EndDate, b.Days, b.PricePerDay, b.RentTotal,
            b.Deposit, b.Commission, b.Status, b.HoldExpiresAt, b.CancelReason, b.CreatedAt);

        public static HandoverDto ToDto(this Handover h) => new(
            h.Id, h.BookingId, h.Kind, h.CreatedBy, h.Odo, h.FuelLevel, h.Note,
            h.Status, h.SignCreator, h.SignReviewer, h.Objection, h.ReviewedAt, h.CreatedAt,
            h.Photos.Select(p => new HandoverPhotoDto(p.Id, p.Slot, p.Url, p.TakenBy, p.Note)).ToList());

        public static PayoutDto ToDto(this Payout p, string? bookingCode) => new(
            p.Id, p.BookingId, bookingCode, p.PayeeType, p.BankAccount, p.BankName,
            p.Amount, p.Status, p.TransferRef, p.PaidAt, p.CreatedAt);
    }
}
