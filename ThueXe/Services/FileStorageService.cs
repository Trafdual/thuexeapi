using ThueXe.Common;

namespace ThueXe.Services
{
    public class FileStorageService
    {
        private readonly string _root;

        public FileStorageService(IWebHostEnvironment env)
        {
            _root = Path.Combine(env.ContentRootPath, "uploads");
            Directory.CreateDirectory(_root);
        }

        public async Task<string> SaveAsync(IFormFile file)
        {
            if (file.Length == 0)
                throw new BizException("EMPTY_FILE");
            if (file.Length > 8 * 1024 * 1024)
                throw new BizException("FILE_TOO_LARGE", "Ảnh vượt quá 8MB");

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                throw new BizException("UNSUPPORTED_FILE_TYPE");

            var name = $"{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(_root, name);

            await using var stream = File.Create(path);
            await file.CopyToAsync(stream);

            return $"/files/{name}";
        }
    }
}
