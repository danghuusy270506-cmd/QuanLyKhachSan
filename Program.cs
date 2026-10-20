using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuanLyKhachSan.Models.Data;
using System.Security.Claims;
using QuanLyKhachSan.Models.Entities;

var builder = WebApplication.CreateBuilder(args);

// 1. CẤU HÌNH GLOBAL AUTHORIZATION POLICY (Khóa toàn bộ View & API mặc định)
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(policy));
});

// 2. KẾT NỐI DATABASE
builder.Services.AddDbContext<QLKhachSanContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("QuanLyKhachSan")));

// 3. CẤU HÌNH JWT BEARER ĐỌC TOKEN TỪ HTTPONLY COOKIE
var secretKey = "Chuoi_Secret_Key_Bao_Mat_Khach_San_123456789";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidIssuer = "QuanLyKhachSanServer",
            ValidAudience = "QuanLyKhachSanClient",
            ValidateIssuer = true,
            ValidateAudience = true,
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero
        };

        // Lấy Token từ Cookie "AccessToken" ở mỗi Request
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.ContainsKey("AccessToken"))
                {
                    context.Token = context.Request.Cookies["AccessToken"];
                }
                return Task.CompletedTask;
            },
            // Trả về JSON 401 nếu gọi API chưa đăng nhập
            OnChallenge = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync("{\"message\": \"Bạn chưa đăng nhập!\"}");
                }
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();


// TỰ ĐỘNG TẠO DATABASE VÀ KHỞI TẠO ADMIN NẾU CHƯA CÓ
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<QLKhachSanContext>();

    await context.Database.EnsureCreatedAsync();
    await SeedData.SeedCatalogAsync(context);
    // Kiểm tra đã có Admin chưa

    // Kiểm tra đã có Admin chưa
    bool adminExists = await context.TaiKhoans
        .AnyAsync(t => t.Role == "Admin" || t.Role == "ADMIN");

    if (!adminExists)
    {
        var username = app.Configuration[
            "InitialAdmin:Username"]?.Trim();

        var password = app.Configuration[
            "InitialAdmin:Password"];

        var hoTen = app.Configuration[
            "InitialAdmin:HoTen"]?.Trim();

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            // Không tạo Admin mặc định.
            // Cho phép website công khai khởi chạy để xem dữ liệu.
            app.Logger.LogWarning(
                "Chưa cấu hình InitialAdmin. " +
                "Website công khai vẫn khởi chạy, " +
                "nhưng chưa có tài khoản Admin.");
        }
        else
        {
            bool usernameExists = await context.TaiKhoans
                .AnyAsync(t => t.Username == username);

            if (usernameExists)
            {
                throw new InvalidOperationException(
                    "Username khởi tạo Admin đã được sử dụng. " +
                    "Hãy chọn Username khác trong InitialAdmin.");
            }

            await using var transaction =
                await context.Database.BeginTransactionAsync();

            try
            {
                var adminAccount = new TaiKhoan
                {
                    Username = username,
                    Password = BCrypt.Net.BCrypt.HashPassword(password, 10),
                    Role = "Admin",
                    TrangThai = "HoatDong"
                };

                context.TaiKhoans.Add(adminAccount);
                await context.SaveChangesAsync();

                var adminProfile = new NhanVien
                {
                    HoTen = string.IsNullOrWhiteSpace(hoTen)
                        ? "Quản trị viên"
                        : hoTen,
                    MaTaiKhoan = adminAccount.MaTaiKhoan
                };

                context.NhanViens.Add(adminProfile);
                await context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

}


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// BẬT XÁC THỰC VÀ PHÂN QUYỀN (Không còn UseSession)
app.UseAuthentication();
app.UseAuthorization();

// TỰ ĐỘNG REDIRECT VỀ DANGNHAP NẾU CHƯA XÁC THỰC (Dành cho View)
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    var request = context.HttpContext.Request;

    if (response.StatusCode == StatusCodes.Status401Unauthorized && !request.Path.StartsWithSegments("/api"))
    {
        response.Redirect("/DangNhap/Authentication");
    }
});

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=ClientIndex}/{id?}")
    .WithStaticAssets();

app.Run();