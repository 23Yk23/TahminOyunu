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
                          .OrderBy(g => g.CreatedAt) // Yüklenme sýrasýna göre
                          .ToList();

            var category = cm.TGetById(id);
            ViewBag.CategoryName = category != null ? category.Name : "Kategori bulunamadý";

            return View(games);
        }

        // Oyunu Baþlatma (GET)
        [HttpGet]
        public IActionResult PlayGame(int id) // mediaId'yi parametre olarak alýyoruz
        {
            var media = mm.TGetByIdWithImages(id); // Media'yý resimleriyle birlikte getiriyoruz

            var allInCategory = mm.GetMediaByCategoryId(media.CategoryId)
                      .OrderBy(m => m.CreatedAt)
                      .ToList();

            var currentIndex = allInCategory.FindIndex(m => m.Id == media.Id);
            int gameNumber = currentIndex + 1;

            var previousId = currentIndex > 0 ? allInCategory[currentIndex - 1].Id : (int?)null;
            var nextId = currentIndex < allInCategory.Count - 1 ? allInCategory[currentIndex + 1].Id : (int?)null;



            if (media == null || !media.MediaImages.Any())
            {
                TempData["ErrorMessage"] = "Oyun baþlatýlamadý veya bu içeriðe ait resim bulunamadý. Lütfen baþka bir oyun seçin.";
                return RedirectToAction("GameList", new { id = ViewBag.PreviousCategoryId ?? 0 }); // Eðer bir önceki kategori ID'si varsa oraya, yoksa Index'e
                                                                                                   // veya uygun bir hata sayfasýna yönlendir.
                                                                                                   // Ya da TempData["PreviousCategoryId"] = media.CategoryId; gibi bir deðer tutuluyorsa
            }

            // Eðer bir kategoriye geri dönülecekse CategoryId'yi ViewBag'e ata
            ViewBag.PreviousCategoryId = media.CategoryId;


            var sortedImages = media.MediaImages.OrderBy(img => img.OrderNo).ToList();

            if (!sortedImages.Any()) // Tekrar kontrol, TGetByIdWithImages sonrasý da resim olmayabilir.
            {
                TempData["ErrorMessage"] = "Bu içeriðe ait gösterilecek resim bulunamadý.";
                return RedirectToAction("GameList", new { id = media.CategoryId });
            }


            var viewModel = new PlayGameViewModel
            {
                MediaId = media.Id,
                MediaTitle = media.Title,
                AllImages = sortedImages,
                CurrentImagePath = sortedImages.First().ImagePath,
                CurrentImageIndex = 0,
                Attempts = 1, // Ýlk deneme
                IsCorrect = false,
                GameOver = false,
                Message = "",

                CreatedAt = media.CreatedAt,
                PreviousMediaId = previousId,
                NextMediaId = nextId,
                CurrentGameNumber = gameNumber,


            };

            // Döngüsel referanslarý görmezden gelmek için JsonSerializerSettings oluþturun
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            // Oyun durumunu session'da saklayýn, oluþturulan ayarlarý kullanarak
            HttpContext.Session.SetString($"PlayGameViewModel_{media.Id}", JsonConvert.SerializeObject(viewModel, settings));

            return View(viewModel);
        }

        // Tahmin Yapma veya Pas Geçme (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PlayGame(PlayGameViewModel submittedModel, string submitButton) // Gelen model ve submitButton
        {

            // Session'dan mevcut oyun durumunu al
            var sessionKey = $"PlayGameViewModel_{submittedModel.MediaId}"; // Doðru MediaId'yi kullan
            var sessionData = HttpContext.Session.GetString(sessionKey);

            if (string.IsNullOrEmpty(sessionData))
            {
                TempData["ErrorMessage"] = "Oyun oturumu bulunamadý. Lütfen oyunu tekrar baþlatýn.";
                // Hangi kategoriye döneceðini bilmek zor, bu yüzden ana sayfaya veya genel bir oyun listesine yönlendirme
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

            // Gelen tahmini ViewModel'e ata (eðer formdan direkt baðlanmýyorsa)
            viewModel.UserGuess = submittedModel.UserGuess;


            // ViewBag.PreviousCategoryId'yi POST iþleminden sonra da View'a taþýmak gerekebilir,
            // eðer View bu bilgiye ihtiyaç duyuyorsa (örneðin "Oyun Listesine Dön" linki için).
            // Session'dan okunan viewModel.AllImages.FirstOrDefault()?.Media?.CategoryId gibi bir yolla bulunabilir
            // veya GET sýrasýnda Session'a ayrýca kaydedilebilir.
            // Þimdilik en basit yol, media'nýn CategoryId'sini de viewModel içinde saklamak veya
            // GET'teki gibi tekrar veritabanýndan çekmemek için Session'daki viewModel'den almak.
            // Media'yý tekrar çekmemek için CategoryId'yi PlayGameViewModel'e ekleyebiliriz.
            // Ya da:
            if (viewModel.AllImages.Any() && viewModel.AllImages.First().Media != null) // Kontrol ekleyelim
            {
                ViewBag.PreviousCategoryId = viewModel.AllImages.First().Media.CategoryId;
            }
            else
            {
                // Alternatif olarak, mediaId üzerinden CategoryId'yi tekrar çekebiliriz
                var mediaForCategory = mm.TGetById(viewModel.MediaId);
                if (mediaForCategory != null) ViewBag.PreviousCategoryId = mediaForCategory.CategoryId;
            }


            if (viewModel.GameOver)
            {
                return View(viewModel); // Oyun zaten bitmiþse bir þey yapma, mevcut durumu göster
            }

            bool guessedCorrectly = false;

            if (submitButton == "guess") // Tahmin butonu týklandýysa
            {
                if (!string.IsNullOrWhiteSpace(viewModel.UserGuess)) // viewModel.UserGuess'i kullan
                {
                    if (viewModel.UserGuess.Trim().Equals(viewModel.MediaTitle.Trim(), System.StringComparison.OrdinalIgnoreCase))
                    {
                        guessedCorrectly = true;
                        viewModel.IsCorrect = true;
                        viewModel.GameOver = true;
                        viewModel.Message = "Bildiniz!";
                        // Doðru bilince son resmi göster (isteðe baðlý)
                        if (viewModel.AllImages.Any()) // Resim listesinin boþ olmadýðýndan emin ol
                        {
                            viewModel.CurrentImagePath = viewModel.AllImages.Last().ImagePath;
                            viewModel.CurrentImageIndex = viewModel.AllImages.Count - 1;
                        }
                    }
                    else
                    {
                        viewModel.Message = "Yanlýþ tahmin!"; // Yanlýþ tahminde mesaj
                    }
                }
                else
                {
                    viewModel.Message = "Lütfen bir tahmin girin."; // Boþ tahmin durumu
                }
            }
            // "pass" (Pas geç) veya yanlýþ tahmin durumunda (veya boþ tahmin) bir sonraki resme geç
            if (!guessedCorrectly) // Eðer doðru bilinmediyse (yanlýþ tahmin VEYA pas geçme ise)
            {
                if (submitButton == "pass" || (submitButton == "guess" && !guessedCorrectly)) // Sadece pas veya yanlýþ tahminde attempt artýr
                {
                    viewModel.Attempts++;
                }

                if (viewModel.Attempts <= viewModel.MaxAttempts && viewModel.CurrentImageIndex < viewModel.AllImages.Count - 1)
                {
                    // Eðer pas geçiliyorsa veya yanlýþ tahminse ve hala deneme hakký varsa bir sonraki resme geç
                    if (submitButton == "pass" || (submitButton == "guess" && !guessedCorrectly))
                    {
                        viewModel.CurrentImageIndex++;
                        viewModel.CurrentImagePath = viewModel.AllImages[viewModel.CurrentImageIndex].ImagePath;
                        if (submitButton == "pass") viewModel.Message = ""; // Pas geçince önceki mesajý temizle
                    }
                }
                else // Deneme hakký bitti veya son resme gelindi ve hala bilinmedi (veya pas geçildi)
                {
                    viewModel.GameOver = true;
                    viewModel.Message = $"Bilemediniz! Doðru Cevap: {viewModel.MediaTitle}";
                    if (viewModel.AllImages.Any())
                    {
                        viewModel.CurrentImagePath = viewModel.AllImages.Last().ImagePath; // Son resmi göster
                        viewModel.CurrentImageIndex = viewModel.AllImages.Count - 1;
                    }
                }
            }

            // Güncellenmiþ oyun durumunu session'a kaydet
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
