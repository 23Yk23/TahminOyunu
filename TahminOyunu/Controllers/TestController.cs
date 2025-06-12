using DataAccessLayer.Concrete;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Mvc;

namespace TahminOyunu.Controllers
{
    public class TestController : Controller
    {
        private readonly Context _context;

        public TestController(Context context)
        {
            _context = context;
        }

        public IActionResult AddAdmin()
        {
            var admin = new Admin
            {
                Username = "admin1",
                PasswordHash = "hashedpassword123"
            };

            _context.Admins.Add(admin);
            _context.SaveChanges();

            return Content("Admin eklendi!");

        }

        public IActionResult AddCategory()
        {
            var category = new Category
            {
                Name = "Yabancı Dizi",
                CreatedAt = DateTime.Now
            };

            _context.Categories.Add(category);
            _context.SaveChanges();

            return Content("Kategori eklendi!");
        }


        public IActionResult Index()
        {
            int adminCount = _context.Admins.Count();
            return Content($"Admin Sayısı: {adminCount}");
        }
    }
}
