using DataAccessLayer.Concrete;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TahminOyunu.Controllers
{
    [AllowAnonymous]
    public class LoginController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Admin"); // Kullanıcı zaten giriş yapmış
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(Admin p)
        {
            Context c = new Context();
            var datavalue = c.Admins.FirstOrDefault(x => x.Username == p.Username
            && x.PasswordHash == p.PasswordHash);
            if (datavalue != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name,p.Username)
                };
                var useridentity = new ClaimsIdentity(claims, "a");
                ClaimsPrincipal principal = new ClaimsPrincipal(useridentity);

                await HttpContext.SignInAsync(principal);

                return RedirectToAction("Index", "Admin");
            }
            else
            {
                return View();
            }
        }
    }
}
