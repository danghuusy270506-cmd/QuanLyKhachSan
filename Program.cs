using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models.Data;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();


//cấu hình kết nối với SQL Server
builder.Services.AddDbContext<QLKhachSanContext>(Options =>
Options.UseSqlServer(builder.Configuration.GetConnectionString("QuanLyKhachSan")));


var app = builder.Build();


// Tự động tạo cơ sở dữ liệu và bảng khi ứng dụng khởi động
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<QLKhachSanContext>();
    context.Database.EnsureCreated(); // Dòng này tự động tạo DB & Bảng nếu chưa có
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
