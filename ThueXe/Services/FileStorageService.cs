using ThueXe.Common;

namespace ThueXe.Services
{
    public class FileStorageService
    {
        private readonly string _root;
        private readonly string _rieng;

        public FileStorageService(IWebHostEnvironment env)
        {
            _root = Path.Combine(env.ContentRootPath, "uploads");
            Directory.CreateDirectory(_root);
            // Ảnh giấy tờ tuỳ thân nằm NGOÀI thư mục uploads: uploads được phát công khai ở /files,
            // ai có đường dẫn là xem được, không cần đăng nhập.
            _rieng = Path.Combine(env.ContentRootPath, "private-uploads");
            Directory.CreateDirectory(_rieng);
        }

        /// Chuyển ảnh vừa tải lên (/files/xxx) sang kho riêng, trả về url mới dạng /private/xxx.
        /// Ảnh mẫu (/files/mau/...) và url lạ được giữ nguyên.
        public string ChuyenSangRiengTu(string? url)
        {
            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("/files/")) return url ?? "";
            var ten = url["/files/".Length..];
            if (ten.Contains('/') || ten.Contains('\\') || ten.Contains("..")) return url;
            var nguon = Path.Combine(_root, ten);
            if (!File.Exists(nguon)) return url;
            File.Move(nguon, Path.Combine(_rieng, ten), overwrite: true);
            return "/private/" + ten;
        }

        /// Đường dẫn thật của ảnh giấy tờ, kể cả ảnh cũ còn ở /files. Null nếu không có hoặc url không an toàn.
        public string? DuongDanGiayTo(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            string goc, ten;
            if (url.StartsWith("/private/")) { goc = _rieng; ten = url["/private/".Length..]; }
            else if (url.StartsWith("/files/")) { goc = _root; ten = url["/files/".Length..]; }
            else return null;
            if (ten.Contains("..") || ten.Contains('\\') || Path.IsPathRooted(ten)) return null;
            var p = Path.GetFullPath(Path.Combine(goc, ten));
            return p.StartsWith(Path.GetFullPath(goc)) && File.Exists(p) ? p : null;
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
