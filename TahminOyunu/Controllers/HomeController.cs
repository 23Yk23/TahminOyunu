using BusinessLayer.Concrete;
using DataAccessLayer.EntityFramework;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using TahminOyunu.Models;

namespace TahminOyunu.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        CategoryManager cm = new CategoryManager(new EfCategoryRepository());
        MediaManager mm = new MediaManager(new EfMediaRepository());
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var values = cm.GetList();
            return View(values);
        }

        public IActionResult GameList(int id)
        {
            var games = mm.GetMediaByCategoryId(id)
                          .OrderBy(g => g.CreatedAt) // Y�klenme s�ras�na g�re
                          .ToList();

            var category = cm.TGetById(id);
            ViewBag.CategoryName = category != null ? category.Name : "Kategori bulunamad�";

            return View(games);
        }

        // Oyunu Ba�latma (GET)
        [HttpGet]
        public IActionResult PlayGame(int id) // mediaId'yi parametre olarak al�yoruz
        {
            var media = mm.TGetByIdWithImages(id); // Media'y� resimleriyle birlikte getiriyoruz

            var allInCategory = mm.GetMediaByCategoryId(media.CategoryId)
                      .OrderBy(m => m.CreatedAt)
                      .ToList();

            var currentIndex = allInCategory.FindIndex(m => m.Id == media.Id);
            int gameNumber = currentIndex + 1;

            var previousId = currentIndex > 0 ? allInCategory[currentIndex - 1].Id : (int?)null;
            var nextId = currentIndex < allInCategory.Count - 1 ? allInCategory[currentIndex + 1].Id : (int?)null;



            if (media == null || !media.MediaImages.Any())
            {
                TempData["ErrorMessage"] = "Oyun ba�lat�lamad� veya bu i�eri�e ait resim bulunamad�. L�tfen ba�ka bir oyun se�in.";
                return RedirectToAction("GameList", new { id = ViewBag.PreviousCategoryId ?? 0 }); // E�er bir �nceki kategori ID'si varsa oraya, yoksa Index'e
                                                                                                   // veya uygun bir hata sayfas�na y�nlendir.
                                                                                                   // Ya da TempData["PreviousCategoryId"] = media.CategoryId; gibi bir de�er tutuluyorsa
            }

            // E�er bir kategoriye geri d�n�lecekse CategoryId'yi ViewBag'e ata
            ViewBag.PreviousCategoryId = media.CategoryId;


            var sortedImages = media.MediaImages.OrderBy(img => img.OrderNo).ToList();

            if (!sortedImages.Any()) // Tekrar kontrol, TGetByIdWithImages sonras� da resim olmayabilir.
            {
                TempData["ErrorMessage"] = "Bu i�eri�e ait g�sterilecek resim bulunamad�.";
                return RedirectToAction("GameList", new { id = media.CategoryId });
            }


            var viewModel = new PlayGameViewModel
            {
                MediaId = media.Id,
                MediaTitle = media.Title,
                AllImages = sortedImages,
                CurrentImagePath = sortedImages.First().ImagePath,
                CurrentImageIndex = 0,
                Attempts = 1, // �lk deneme
                IsCorrect = false,
                GameOver = false,
                Message = "",

                CreatedAt = media.CreatedAt,
                PreviousMediaId = previousId,
                NextMediaId = nextId,
                CurrentGameNumber = gameNumber,


            };

            // D�ng�sel referanslar� g�rmezden gelmek i�in JsonSerializerSettings olu�turun
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            // Oyun durumunu session'da saklay�n, olu�turulan ayarlar� kullanarak
            HttpContext.Session.SetString($"PlayGameViewModel_{media.Id}", JsonConvert.SerializeObject(viewModel, settings));

            return View(viewModel);
        }

        // Tahmin Yapma veya Pas Ge�me (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PlayGame(PlayGameViewModel submittedModel, string submitButton) // Gelen model ve submitButton
        {

            // Session'dan mevcut oyun durumunu al
            var sessionKey = $"PlayGameViewModel_{submittedModel.MediaId}"; // Do�ru MediaId'yi kullan
            var sessionData = HttpContext.Session.GetString(sessionKey);

            if (string.IsNullOrEmpty(sessionData))
            {
                TempData["ErrorMessage"] = "Oyun oturumu bulunamad�. L�tfen oyunu tekrar ba�lat�n.";
                // Hangi kategoriye d�nece�ini bilmek zor, bu y�zden ana sayfaya veya genel bir oyun listesine y�nlendirme
                return RedirectToAction("Index");
            }

            var viewModel = JsonConvert.DeserializeObject<PlayGameViewModel>(sessionData);

            if (submitButton == "select")
            {
                viewModel.CurrentImageIndex = submittedModel.SelectedIndex;
                viewModel.CurrentImagePath = viewModel.AllImages[submittedModel.SelectedIndex].ImagePath;

                HttpContext.Session.SetString(sessionKey, JsonConvert.SerializeObject(viewModel));
                return View(viewModel);
            }

            // Gelen tahmini ViewModel'e ata (e�er formdan direkt ba�lanm�yorsa)
            viewModel.UserGuess = submittedModel.UserGuess;


            // ViewBag.PreviousCategoryId'yi POST i�leminden sonra da View'a ta��mak gerekebilir,
            // e�er View bu bilgiye ihtiya� duyuyorsa (�rne�in "Oyun Listesine D�n" linki i�in).
            // Session'dan okunan viewModel.AllImages.FirstOrDefault()?.Media?.CategoryId gibi bir yolla bulunabilir
            // veya GET s�ras�nda Session'a ayr�ca kaydedilebilir.
            // �imdilik en basit yol, media'n�n CategoryId'sini de viewModel i�inde saklamak veya
            // GET'teki gibi tekrar veritaban�ndan �ekmemek i�in Session'daki viewModel'den almak.
            // Media'y� tekrar �ekmemek i�in CategoryId'yi PlayGameViewModel'e ekleyebiliriz.
            // Ya da:
            if (viewModel.AllImages.Any() && viewModel.AllImages.First().Media != null) // Kontrol ekleyelim
            {
                ViewBag.PreviousCategoryId = viewModel.AllImages.First().Media.CategoryId;
            }
            else
            {
                // Alternatif olarak, mediaId �zerinden CategoryId'yi tekrar �ekebiliriz
                var mediaForCategory = mm.TGetById(viewModel.MediaId);
                if (mediaForCategory != null) ViewBag.PreviousCategoryId = mediaForCategory.CategoryId;
            }


            if (viewModel.GameOver)
            {
                return View(viewModel); // Oyun zaten bitmi�se bir �ey yapma, mevcut durumu g�ster
            }

            bool guessedCorrectly = false;

            if (submitButton == "guess") // Tahmin butonu t�kland�ysa
            {
                if (!string.IsNullOrWhiteSpace(viewModel.UserGuess)) // viewModel.UserGuess'i kullan
                {
                    if (viewModel.UserGuess.Trim().Equals(viewModel.MediaTitle.Trim(), System.StringComparison.OrdinalIgnoreCase))
                    {
                        guessedCorrectly = true;
                        viewModel.IsCorrect = true;
                        viewModel.GameOver = true;
                        viewModel.Message = "Bildiniz!";
                        // Do�ru bilince son resmi g�ster (iste�e ba�l�)
                        if (viewModel.AllImages.Any()) // Resim listesinin bo� olmad���ndan emin ol
                        {
                            viewModel.CurrentImagePath = viewModel.AllImages.Last().ImagePath;
                            viewModel.CurrentImageIndex = viewModel.AllImages.Count - 1;
                        }
                    }
                    else
                    {
                        viewModel.Message = "Yanl�� tahmin!"; // Yanl�� tahminde mesaj
                    }
                }
                else
                {
                    viewModel.Message = "L�tfen bir tahmin girin."; // Bo� tahmin durumu
                }
            }
            // "pass" (Pas ge�) veya yanl�� tahmin durumunda (veya bo� tahmin) bir sonraki resme ge�
            if (!guessedCorrectly) // E�er do�ru bilinmediyse (yanl�� tahmin VEYA pas ge�me ise)
            {
                if (submitButton == "pass" || (submitButton == "guess" && !guessedCorrectly)) // Sadece pas veya yanl�� tahminde attempt art�r
                {
                    viewModel.Attempts++;
                }

                if (viewModel.Attempts <= viewModel.MaxAttempts && viewModel.CurrentImageIndex < viewModel.AllImages.Count - 1)
                {
                    // E�er pas ge�iliyorsa veya yanl�� tahminse ve hala deneme hakk� varsa bir sonraki resme ge�
                    if (submitButton == "pass" || (submitButton == "guess" && !guessedCorrectly))
                    {
                        viewModel.CurrentImageIndex++;
                        viewModel.CurrentImagePath = viewModel.AllImages[viewModel.CurrentImageIndex].ImagePath;
                        if (submitButton == "pass") viewModel.Message = ""; // Pas ge�ince �nceki mesaj� temizle
                    }
                }
                else // Deneme hakk� bitti veya son resme gelindi ve hala bilinmedi (veya pas ge�ildi)
                {
                    viewModel.GameOver = true;
                    viewModel.Message = $"Bilemediniz! Do�ru Cevap: {viewModel.MediaTitle}";
                    if (viewModel.AllImages.Any())
                    {
                        viewModel.CurrentImagePath = viewModel.AllImages.Last().ImagePath; // Son resmi g�ster
                        viewModel.CurrentImageIndex = viewModel.AllImages.Count - 1;
                    }
                }
            }

            // G�ncellenmi� oyun durumunu session'a kaydet
            HttpContext.Session.SetString(sessionKey, JsonConvert.SerializeObject(viewModel));

            return View(viewModel);
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
