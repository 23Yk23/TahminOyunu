using DataAccessLayer.Concrete;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Concrete;
using DataAccessLayer.EntityFramework;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using TahminOyunu.Models;
using DataAccessLayer.Abstract;

namespace TahminOyunu.Controllers
{
    public class AdminController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment; //içerik ekleme kısmı için

        MediaManager mm = new MediaManager(new EfMediaRepository());
        MediaImageManager mim = new MediaImageManager(new EfMediaImageRepository());
        CategoryManager cm = new CategoryManager(new EfCategoryRepository());

        public AdminController(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            var _username = User.Identity.Name;
            Context c = new Context();
            //var _username = c.Users.Where(x => x.Email == _usermail).Select(y => y.Username).FirstOrDefault();
            var user = c.Admins.FirstOrDefault(x => x.Username == _username);

            return View();
        }

        // İçerik Ekleme - GET
        [HttpGet]
        public IActionResult Create()
        {
            // Dropdown için kategorileri getir
            ViewBag.Categories = new SelectList(cm.GetList(), "Id", "Name");
            return View(new ContentViewModel());
        }

        // İçerik Ekleme - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ContentViewModel model)
        {
            // ViewBag.Categories'i her durumda doldur (hata durumunda geri dönerken kullanılacak)
            ViewBag.Categories = new SelectList(cm.GetList(), "Id", "Name", model.CategoryId);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Görsel doğrulaması
            if (model.Images == null)
            {
                ModelState.AddModelError("Images", "Lütfen görsel yükleyin.");
                return View(model);
            }

            if (model.Images.Count != 6)
            {
                ModelState.AddModelError("Images", "Tam olarak 6 görsel yüklemelisiniz.");
                return View(model);
            }

            try
            {
                var media = new Media
                {
                    Title = model.Title,
                    Description = model.Description,
                    CategoryId = model.CategoryId.Value,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now,
                    MediaImages = new List<MediaImage>() // Koleksiyonu başlat
                };

                int orderNo = 1;
                foreach (var imageFile in model.Images)
                {
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "media");

                        // Klasörün varlığını kontrol et ve yoksa oluştur
                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);

                        string filePath = Path.Combine(uploadsFolder, fileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            imageFile.CopyTo(fileStream);
                        }

                        media.MediaImages.Add(new MediaImage
                        {
                            ImagePath = "/images/media/" + fileName,
                            OrderNo = orderNo++
                        });
                    }
                }

                // Media Manager ile ekleme
                mm.TAdd(media);

                TempData["SuccessMessage"] = "İçerik başarıyla eklendi.";
                return RedirectToAction("GameList", "Admin");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "İçerik eklenirken bir hata oluştu: " + ex.Message);
                // Hata durumunda log tutabilirsiniz
                // _logger.LogError(ex, "İçerik eklenirken hata oluştu");
                return View(model);
            }
        }


        public IActionResult GameList(string search)
        {
            var mediaList = mm.GetListWithCategoryM(); // Verileri kategori ile birlikte getiren metot

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower(); // Arama terimini küçük harfe çevir
                mediaList = mediaList
                    .Where(m =>
                        (m.Title != null && m.Title.ToLower().Contains(search)) ||
                        (m.Category != null && m.Category.Name != null && m.Category.Name.ToLower().Contains(search)) ||
                        (m.CreatedAt.ToString("dd.MM.yyyy").ToLower().Contains(search)) ||
                        (search == "evet" && m.IsActive) ||    // Alternatif olarak "evet" girilirse aktif
                        (search == "hayır" && !m.IsActive) ||  // Alternatif olarak "hayır" girilirse pasif
                        (search == "aktif" && m.IsActive) ||   // "aktif" girilirse aktif olanları filtrele
                        (search == "pasif" && !m.IsActive)  // "pasif" girilirse pasif olanları filtrele
                    )
                    .ToList();
            }

            return View(mediaList);
        }

        [HttpGet]
        public IActionResult Edit(int? id) // Yeni Edit Action'ı
        {
            if (id == null) return NotFound();

            var media = mm.TGetByIdWithImages(id.Value);// Sadece Media nesnesini getir
            if (media == null) return NotFound();

            var model = new ContentViewModel
            {
                Id = media.Id,
                Title = media.Title,
                Description = media.Description,
                CategoryId = media.CategoryId,
                IsActive = media.IsActive
                // ExistingImagePaths doldurulmayacak
            };

            ViewBag.Categories = new SelectList(cm.GetList(), "Id", "Name", media.CategoryId);
            ViewBag.ExistingImages = media.MediaImages.OrderBy(x => x.OrderNo).ToList();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ContentViewModel model, int id)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            // Model state geçerliyse ve yeni görsel yüklenmediyse, doğrudan güncelle
            if (ModelState.IsValid && (model.Images == null || !model.Images.Any()))
            {
                try
                {
                    var mediaInDb = mm.TGetByIdWithImages(id);

                    if (mediaInDb == null)
                    {
                        return NotFound();
                    }

                    mediaInDb.Title = model.Title;
                    mediaInDb.Description = model.Description;
                    mediaInDb.CategoryId = model.CategoryId.Value;
                    mediaInDb.IsActive = model.IsActive;
                    mediaInDb.CreatedAt = DateTime.Now;

                    mm.TUpdate(mediaInDb);

                    TempData["SuccessMessage"] = "İçerik başarıyla güncellendi.";
                    return RedirectToAction("GameList", "Admin");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "İçerik güncellenirken bir hata oluştu: " + ex.Message);
                }

                ViewBag.Categories = new SelectList(cm.GetList(), "Id", "Name", model.CategoryId);
                return View(model);
            }

            // Yeni görsel yüklenmişse 6 adet kontrolü
            if (model.Images != null && model.Images.Any() && model.Images.Count != 6)
            {
                ModelState.AddModelError("Images", "Yeni görseller 6 adet olmalıdır.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var mediaInDb = mm.TGetByIdWithImages(id);

                    if (mediaInDb == null)
                    {
                        return NotFound();
                    }

                    mediaInDb.Title = model.Title;
                    mediaInDb.Description = model.Description;
                    mediaInDb.CategoryId = model.CategoryId.Value;
                    mediaInDb.IsActive = model.IsActive;
                    //mediaInDb.CreatedAt = DateTime.Now;

                    mm.TUpdate(mediaInDb);

                    // Yeni fotoğraf yüklenmişse eskileri sil ve yenilerini ekle (tam olarak 6 adet)
                    if (model.Images != null && model.Images.Any() && model.Images.Count == 6)
                    {
                        // Önce eski görselleri sil
                        var oldImages = mediaInDb.MediaImages.ToList();
                        foreach (var oldImg in oldImages)
                        {
                            mim.TDelete(oldImg);
                        }

                        // Yeni görselleri kaydet
                        string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                        if (!Directory.Exists(uploadPath))
                        {
                            Directory.CreateDirectory(uploadPath);
                        }

                        int orderNo = 1;
                        foreach (var file in model.Images)
                        {
                            if (file.Length > 0)
                            {
                                string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                                string filePath = Path.Combine(uploadPath, uniqueFileName);

                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(fileStream);
                                }

                                mim.TAdd(new MediaImage
                                {
                                    MediaId = mediaInDb.Id,
                                    ImagePath = "/uploads/" + uniqueFileName,
                                    OrderNo = orderNo
                                });

                                orderNo++;
                            }
                        }
                    }
                    // Yeni görsel yüklenmemişse eski görseller olduğu gibi kalır.

                    TempData["SuccessMessage"] = "İçerik başarıyla güncellendi.";
                    return RedirectToAction("GameList", "Admin");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "İçerik güncellenirken bir hata oluştu: " + ex.Message);
                }
            }

            ViewBag.Categories = new SelectList(cm.GetList(), "Id", "Name", model.CategoryId);
            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var media = mm.TGetById(id);
            if (media == null)
            {
                return NotFound();
            }
            mm.TDelete(media);
            TempData["SuccessMessage"] = "İçerik başarıyla silindi.";
            return RedirectToAction("GameList");
        }

        // KATEGORİ YÖNETİMİ BAŞLANGICI

        public IActionResult Categories()
        {
            var categories = cm.GetList();
            return View(categories);
        }

        [HttpGet]
        public IActionResult AddCategory()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddCategory(Category category)
        {
            if (ModelState.IsValid) // Doğrulamayı burada yapın
            {
                cm.TAdd(category);
                TempData["CategorySuccessMessage"] = "Kategori başarıyla eklendi.";
                return RedirectToAction("Categories");
            }
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCategory(int id)
        {
            var category = cm.TGetById(id);
            if (category == null)
            {
                return NotFound();
            }

            // Kategoriye bağlı içerik var mı kontrol et
            var mediaCount = mm.GetList().Count(m => m.CategoryId == id); // MediaManager'ı kullanarak kontrol et

            if (mediaCount > 0)
            {
                TempData["CategoryErrorMessage"] = $"Bu kategoriye bağlı {mediaCount} içerik bulunmaktadır. Öncelikle bu içerikleri silmelisiniz.";
                return RedirectToAction("Categories");
            }

            try
            {
                cm.TDelete(category);
                TempData["CategorySuccessMessage"] = "Kategori başarıyla silindi.";
            }
            catch (Exception ex)
            {
                TempData["CategoryErrorMessage"] = "Kategori silinirken bir hata oluştu: " + ex.Message;
                // Loglama yapılabilir.
            }
            return RedirectToAction("Categories");
        }

        // KATEGORİ YÖNETİMİ SONU


        //çıkış
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home"); // Giriş sayfasına yönlendirme
        }
    }
}
