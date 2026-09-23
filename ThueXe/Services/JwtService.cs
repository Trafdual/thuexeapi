using ThueXe.Models;

namespace ThueXe.Services
{
    public class JwtService
    {
        private readonly IConfiguration _cfg;

        public JwtService(IConfiguration cfg) => _cfg = cfg;

        public string Issue(AppUser user)
        {
            var jwt = _cfg.GetSection("Jwt");

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new("is_owner", user.IsOwner.ToString()),
                // Đặt đúng ClaimTypes.Role để [Authorize(Roles = "VAN_HANH")] chạy được ngay.
                new(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwt["ExpireMinutes"]!)),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}