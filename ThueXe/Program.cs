using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using ThueXe.Data;
using ThueXe.Middleware;
using ThueXe.Services;

var builder = WebApplication.CreateBuilder(args);

// ── CSDL ──────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Xác thực JWT ─────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        var jwt = builder.Configuration.GetSection("Jwt");

        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],

            ValidateAudience = true,
            ValidAudience = jwt["Audience"],

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt["Key"]!)
            ),

            ValidateLifetime = true
        };

        // Thiếu/hết hạn token và sai vai cũng trả { status, code, message }. HTTP luôn 200, lỗi nằm trong JSON.
        opt.Events = new JwtBearerEvents
        {
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsJsonAsync(ThueXe.Middleware.ApiResponse.Fail(401, "UNAUTHORIZED", "Chưa đăng nhập hoặc token hết hạn"));
            },
            OnForbidden = async ctx =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsJsonAsync(ThueXe.Middleware.ApiResponse.Fail(403, "FORBIDDEN", "Không có quyền thực hiện thao tác này"));
            }
        };
    });

builder.Services.AddAuthorization();

// ── Dịch vụ nghiệp vụ ────────────────────────────────────────────────
builder.Services.AddMemoryCache();

builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<QrService>();
builder.Services.AddScoped<ThanhToanService>();

// Nam tac vu nen don don treo — thieu chung thi lich xe bi khoa chet.
builder.Services.AddHostedService<ThueXe.Jobs.DonTreoService>();

// ── MVC / API ─────────────────────────────────────────────────────────
builder.Services.AddControllers(o => o.Filters.Add<ThueXe.Middleware.BocKetQuaFilter>())
    .ConfigureApiBehaviorOptions(o =>
    {
        // Thiếu trường, sai kiểu, JSON hỏng: cùng một dạng INVALID_INPUT/422 như lỗi nghiệp vụ.
        o.InvalidModelStateResponseFactory = ctx =>
        {
            var loi = ctx.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
            return new Microsoft.AspNetCore.Mvc.ObjectResult(ThueXe.Middleware.ApiResponse.Fail(
                422, "INVALID_INPUT", loi ?? "Dữ liệu gửi lên không hợp lệ"))
            { StatusCode = 200 };
        };
    });
builder.Services.AddEndpointsApiExplorer();

// ── Swagger ──────────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(opt =>
{
    opt.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Nhập JWT token"
    });

    opt.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("bearer", document)] = []
        });
});

// ── Build app ─────────────────────────────────────────────────────────
var app = builder.Build();

// ── Swagger UI ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ── Middleware ────────────────────────────────────────────────────────
app.UseMiddleware<BizExceptionMiddleware>();

// 404, 405, 415... mà thân còn rỗng thì điền { code, message } cho đủ bộ.
app.UseStatusCodePages(async ctx =>
{
    var res = ctx.HttpContext.Response;
    var (code, message) = res.StatusCode switch
    {
        404 => ("NOT_FOUND", "Không tìm thấy đường dẫn"),
        405 => ("METHOD_NOT_ALLOWED", "Sai phương thức HTTP"),
        415 => ("UNSUPPORTED_MEDIA_TYPE", "Kiểu nội dung không được hỗ trợ"),
        401 => ("UNAUTHORIZED", "Chưa đăng nhập hoặc token hết hạn"),
        403 => ("FORBIDDEN", "Không có quyền thực hiện thao tác này"),
        _ => ("HTTP_" + res.StatusCode, "Yêu cầu không thành công")
    };
    var status = res.StatusCode;
    res.StatusCode = 200;
    res.ContentType = "application/json";
    await res.WriteAsJsonAsync(ThueXe.Middleware.ApiResponse.Fail(status, code, message));
});

// ── Static files ─────────────────────────────────────────────────────
app.UseStaticFiles();

var uploadsPath = Path.Combine(
    builder.Environment.ContentRootPath,
    "uploads");

if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider =
        new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
            uploadsPath),

    RequestPath = "/files"
});

// ── HTTPS ────────────────────────────────────────────────────────────
// Ở Development thì KHÔNG chuyển hướng: app Android gọi cleartext, bị đá sang HTTPS là
// gặp chứng chỉ dev mà máy không tin, và lỗi hiện ra y hệt lỗi mất mạng.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// ── Authentication / Authorization ───────────────────────────────────
app.UseAuthentication();
app.UseAuthorization();

// ── Controllers ─────────────────────────────────────────────────────
app.MapControllers();

app.Run();