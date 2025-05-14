var builder = WebApplication.CreateBuilder(args);

// Session ve da��t�k cache servislerini ekleyin <3-15 yeni ekledim>
builder.Services.AddDistributedMemoryCache(); // Session state'i bellekte saklamak i�in gereklidir.
                                              // �retim ortamlar� i�in Redis veya SQL Server gibi daha �l�eklenebilir
                                              // bir da��t�k cache d���nebilirsiniz.

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Oturumun bo�ta kalma s�resi
    options.Cookie.HttpOnly = true; // Cookie'nin istemci taraf� scriptler taraf�ndan eri�ilmesini engeller
    options.Cookie.IsEssential = true; // Cookie'nin uygulama i�levselli�i i�in gerekli oldu�unu belirtir
                                       // Bu, �zellikle GDPR gibi gizlilik d�zenlemeleri i�in �nemlidir.
                                       // options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Sadece HTTPS �zerinden g�nderilmesini sa�lar (�retim i�in �nerilir)
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
app.UseStatusCodePagesWithReExecute("/ErrorPage/Error1", "?code={0}");//hata sayfas� 404

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();//yeniekledim <3-15> . 2

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
