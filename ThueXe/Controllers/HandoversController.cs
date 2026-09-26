using ThueXe.Helpers;

namespace ThueXe.Controllers
{
    [ApiController]
    [Route("handovers")]
    [Authorize]
    public class HandoversController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public HandoversController(ApplicationDbContext db) => _db = db;

        private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // POST /handovers/{id}/photos — bên soi bổ sung ảnh vết bên lập chưa ghi
        [HttpPost("{id:long}/photos")]
        public async Task<ActionResult<HandoverDto>> AddPhotos(long id, AddHandoverPhotosRequest req)
        {
            var (bienBan, laChuXe) = await LayBienBan(id);

            if (bienBan.Status != TrangThaiBienBan.ChoSoi)
                throw new BizException("WRONG_STATE", "Biên bản đã xử lý, không bổ sung ảnh được");

            // Chỉ bên SOI được bổ sung. Bên lập muốn thêm thì lập lại từ đầu.
            var ben = laChuXe ? Ben.ChuXe : Ben.Khach;
            if (bienBan.CreatedBy == ben)
                throw new BizException("FORBIDDEN", "Bên lập không bổ sung ảnh vào biên bản của mình");

            foreach (var a in req.Photos ?? new List<HandoverPhotoRequest>())
                bienBan.Photos.Add(new HandoverPhoto
                {
                    Slot = a.Slot,
                    Url = a.Url,
                    TakenBy = ben,
                    Note = a.Note
                });

            await _db.SaveChangesAsync();
            return bienBan.ToDto();
        }

        // POST /handovers/{id}/review — bên soi ký hoặc phản đối
        [HttpPost("{id:long}/review")]
        public async Task<ActionResult<HandoverDto>> Review(long id, ReviewHandoverRequest req)
        {
            var (bienBan, laChuXe) = await LayBienBan(id);

            if (bienBan.Status != TrangThaiBienBan.ChoSoi)
                throw new BizException("WRONG_STATE", "Biên bản này đã xử lý rồi");

            var ben = laChuXe ? Ben.ChuXe : Ben.Khach;
            if (bienBan.CreatedBy == ben)
                throw new BizException("FORBIDDEN", "Bên lập không tự soi biên bản của mình");

            var don = bienBan.Booking;
            bienBan.ReviewedAt = DateTimeOffset.UtcNow;

            if (req.Agreed)
            {
                if (string.IsNullOrWhiteSpace(req.Signature))
                    throw new BizException("HANDOVER_INCOMPLETE", "Còn thiếu: chữ ký");

                bienBan.Status = TrangThaiBienBan.DaKy;
                bienBan.SignReviewer = req.Signature;

                // Chỉ khi bên soi ký thì đơn mới đổi trạng thái.
                don.Status = bienBan.Kind == LoaiBienBan.Giao
                    ? TrangThaiDon.DangThue
                    : TrangThaiDon.ChoQuyetToan;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(req.Objection))
                    throw new BizException("INVALID_INPUT", "Phản đối thì phải ghi rõ lý do");

                bienBan.Status = TrangThaiBienBan.PhanDoi;
                bienBan.Objection = req.Objection;

                // D1: khách soi biên bản giao mà không đồng ý thì không nhận xe, đơn huỷ.
                // E9: chủ xe phản đối biên bản trả thì đơn đứng lại chờ người vận hành xử.
                if (bienBan.Kind == LoaiBienBan.Giao)
                {
                    don.Status = TrangThaiDon.DaHuy;
                    don.CancelReason = req.Objection;

                    var dong = await _db.CarAvailabilities
                        .Where(a => a.BookingId == don.Id).ToListAsync();
                    _db.CarAvailabilities.RemoveRange(dong);
                }
            }

            await _db.SaveChangesAsync();
            return bienBan.ToDto();
        }

        private async Task<(Handover BienBan, bool LaChuXe)> LayBienBan(long id)
        {
            var bienBan = await _db.Handovers
                .Include(h => h.Photos)
                .Include(h => h.Booking).ThenInclude(b => b.Car)
                .FirstOrDefaultAsync(h => h.Id == id)
                ?? throw new BizException("WRONG_STATE", "Không tìm thấy biên bản");

            var laChuXe = bienBan.Booking.Car.OwnerId == UserId;
            if (!laChuXe && bienBan.Booking.RenterId != UserId)
                throw new BizException("NOT_FOUND", "Không tìm thấy biên bản");

            return (bienBan, laChuXe);
        }
    }
}
