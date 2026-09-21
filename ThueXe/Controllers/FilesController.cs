using ThueXe.Services;

namespace ThueXe.Controllers
{
    [ApiController]
    [Route("files")]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private readonly FileStorageService _storage;

        public FilesController(FileStorageService storage) => _storage = storage;

        // POST /files — tải một ảnh, trả về url. Dùng cho mọi loại ảnh.
        [HttpPost]
        [RequestSizeLimit(8 * 1024 * 1024)]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            var url = await _storage.SaveAsync(file);
            return Ok(new { url });
        }
    }
}
