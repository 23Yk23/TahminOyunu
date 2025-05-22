using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Session ve daðýtýk cache servislerini ekleyin <3-15 yeni ekledim>
builder.Services.AddDistributedMemoryCache(); // Session state'i bellekte saklamak için gereklidir.
                                              // Üretim ortamlarý için Redis veya SQL Server gibi daha ölçeklenebilir
                                              // bir daðýtýk cache düþünebilirsiniz.
builder.Services.AddHttpClient();//api yaparken ekledim
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Oturumun boþta kalma süresi
    options.Cookie.HttpOnly = true; // Cookie'nin istemci tarafý scriptler tarafýndan eriþilmesini engeller
    options.Cookie.IsEssential = true; // Cookie'nin uygulama iþlevselliði için gerekli olduðunu belirtir
                                       // Bu, özellikle GDPR gibi gizlilik düzenlemeleri için önemlidir.
                                       // options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Sadece HTTPS üzerinden gönderilmesini saðlar (üretim için önerilir)
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Index"; // Giriþ yapmayan kullanýcýlar bu sayfaya yönlendirilir.
        options.AccessDeniedPath = "/Login/Index"; // Eriþim yetkisi olmayan kullanýcýlar buraya yönlendirilir.  
    });

builder.Services.AddMvc(config =>
{
    var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
    config.Filters.Add(new AuthorizeFilter(policy));
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
