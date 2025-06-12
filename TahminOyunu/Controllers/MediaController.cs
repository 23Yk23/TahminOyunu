using Microsoft.AspNetCore.Mvc;
using TahminOyunu.Services;

namespace TahminOyunu.Controllers
{
    public class MediaController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public MediaController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                var contentType = file.ContentType;
                var fileName = file.FileName;

                string url = await _firebaseService.UploadFileAsync(stream, fileName, contentType);

                ViewBag.ImageUrl = url;
            }

            return View();
        }
    }
}
