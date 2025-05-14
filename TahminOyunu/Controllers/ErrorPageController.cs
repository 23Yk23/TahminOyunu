using Microsoft.AspNetCore.Mvc;

namespace TahminOyunu.Controllers
{
    public class ErrorPageController : Controller
    {
        public IActionResult Error1(int code)
        {
            return View();
        }
    }
}
