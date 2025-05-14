var builder = WebApplication.CreateBuilder(args);

// Session ve daðýtýk cache servislerini ekleyin <3-15 yeni ekledim>
builder.Services.AddDistributedMemoryCache(); // Session state'i bellekte saklamak için gereklidir.
                                              // Üretim ortamlarý için Redis veya SQL Server gibi daha ölçeklenebilir
                                              // bir daðýtýk cache düþünebilirsiniz.

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Oturumun boþta kalma süresi
    options.Cookie.HttpOnly = true; // Cookie'nin istemci tarafý scriptler tarafýndan eriþilmesini engeller
    options.Cookie.IsEssential = true; // Cookie'nin uygulama iþlevselliði için gerekli olduðunu belirtir
                                       // Bu, özellikle GDPR gibi gizlilik düzenlemeleri için önemlidir.
                                       // options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Sadece HTTPS üzerinden gönderilmesini saðlar (üretim için önerilir)
});

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/ErrorPage/Error1", "?code={0}");//hata sayfasý 404

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();//yeniekledim <3-15> . 2

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
