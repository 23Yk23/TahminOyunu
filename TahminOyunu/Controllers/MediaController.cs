using Microsoft.AspNetCore.Mvc;
using TahminOyunu.Services;
using EntityLayer.Concrete;
using BusinessLayer.Concrete;
using DataAccessLayer.EntityFramework;
using System.IO;

namespace TahminOyunu.Controllers
{
    public class MediaController : Controller
    {
        private readonly FirebaseService _firebaseService;
        private readonly MediaImageManager _mediaImageManager;

        public MediaController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
            _mediaImageManager = new MediaImageManager(new EfMediaImageRepository());
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, int mediaId)
        {
            if (file != null && file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                var contentType = file.ContentType;
                var fileName = file.FileName;

                string url = await _firebaseService.UploadFileAsync(stream, fileName, contentType);
                ViewBag.ImageUrl = url;

                var existingImages = _mediaImageManager.GetListByMediaId(mediaId);
                int nextOrder = existingImages.Any() ? existingImages.Max(i => i.OrderNo) + 1 : 1;

                MediaImage image = new MediaImage
                {
                    MediaId = mediaId,
                    ImagePath = url,
                    OrderNo = nextOrder
                };

                _mediaImageManager.TAdd(image);
            }

            return View();
        }

        [HttpGet]
        public IActionResult ImageList(int mediaId)
        {
            var images = _mediaImageManager.GetListByMediaId(mediaId);
            ViewBag.MediaId = mediaId;
            return View(images);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImage(int imageId, int mediaId)
        {
            var image = _mediaImageManager.TGetById(imageId);
            if (image != null)
            {
                var fileName = Path.GetFileName(image.ImagePath);
                await _firebaseService.DeleteFileAsync(fileName); // Firebase'den sil
                _mediaImageManager.TDelete(image); // Veritabanından sil
            }

            return RedirectToAction("ImageList", new { mediaId = mediaId });
        }

    }
}
