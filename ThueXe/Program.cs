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
    });

builder.Services.AddAuthorization();

// ── Dịch vụ nghiệp vụ ────────────────────────────────────────────────
builder.Services.AddMemoryCache();

builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<FileStorageService>();

// ── MVC / API ─────────────────────────────────────────────────────────
builder.Services.AddControllers();
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