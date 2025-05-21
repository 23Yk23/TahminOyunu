using BusinessLayer.Concrete;
using DataAccessLayer.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using TahminOyunu.Models;

namespace TahminOyunu.Controllers
{
    [AllowAnonymous]
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
                          .OrderBy(g => g.CreatedAt) // Yüklenme sırasına göre
                          .ToList();

            var category = cm.TGetById(id);
            ViewBag.CategoryName = category != null ? category.Name : "Kategori bulunamadı";

            return View(games);
        }
        //Random
        public IActionResult RandomGame(int id)
        {
            Console.WriteLine("RandomGame çağrıldı. Kategori ID: " + id);

            var medyalar = mm.GetMediaByCategoryId(id);
            Console.WriteLine("Kategoriye ait medya sayısı: " + medyalar.Count);

            var randomMedia = medyalar
                                .OrderBy(m => Guid.NewGuid())
                                .FirstOrDefault();

            if (randomMedia == null)
            {
                Console.WriteLine("Rastgele medya bulunamadı.");
                TempData["ErrorMessage"] = "Bu kategoriye ait rastgele bir oyun bulunamadı.";
                return RedirectToAction("GameList", new { id = id });
            }

            Console.WriteLine("Seçilen rastgele medya ID: " + randomMedia.Id);
            return RedirectToAction("PlayGame", new { id = randomMedia.Id });
        }


        // Oyunu Başlatma (GET)
        [HttpGet]
        public IActionResult PlayGame(int id) // mediaId'yi parametre olarak alıyoruz
        {
            var media = mm.TGetByIdWithImages(id); // Media'yı resimleriyle birlikte getiriyoruz

            var allInCategory = mm.GetMediaByCategoryId(media.CategoryId)
                      .OrderBy(m => m.CreatedAt)
                      .ToList();

            var currentIndex = allInCategory.FindIndex(m => m.Id == media.Id);
            int gameNumber = currentIndex + 1;

            var previousId = currentIndex > 0 ? allInCategory[currentIndex - 1].Id : (int?)null;
            var nextId = currentIndex < allInCategory.Count - 1 ? allInCategory[currentIndex + 1].Id : (int?)null;



            if (media == null || !media.MediaImages.Any())
            {
                TempData["ErrorMessage"] = "Oyun başlatılamadı veya bu içeriğe ait resim bulunamadı. Lütfen başka bir oyun seçin.";
                return RedirectToAction("GameList", new { id = ViewBag.PreviousCategoryId ?? 0 }); // Eğer bir önceki kategori ID'si varsa oraya, yoksa Index'e
                                                                                                   // veya uygun bir hata sayfasına yönlendir.
                                                                                                   // Ya da TempData["PreviousCategoryId"] = media.CategoryId; gibi bir değer tutuluyorsa
            }

            // Eğer bir kategoriye geri dönülecekse CategoryId'yi ViewBag'e ata
            ViewBag.PreviousCategoryId = media.CategoryId;


            var sortedImages = media.MediaImages.OrderBy(img => img.OrderNo).ToList();

            if (!sortedImages.Any()) // Tekrar kontrol, TGetByIdWithImages sonrası da resim olmayabilir.
            {
                TempData["ErrorMessage"] = "Bu içeriğe ait gösterilecek resim bulunamadı.";
                return RedirectToAction("GameList", new { id = media.CategoryId });
            }


            var viewModel = new PlayGameViewModel
            {
                MediaId = media.Id,
                MediaTitle = media.Title,
                AllImages = sortedImages,
                CurrentImagePath = sortedImages.First().ImagePath,
                CurrentImageIndex = 0,
                Attempts = 1, // İlk deneme
                IsCorrect = false,
                GameOver = false,
                Message = "",

                CreatedAt = media.CreatedAt,
                PreviousMediaId = previousId,
                NextMediaId = nextId,
                CurrentGameNumber = gameNumber,


            };

            // Döngüsel referansları görmezden gelmek için JsonSerializerSettings oluşturun
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            // Oyun durumunu session'da saklayın, oluşturulan ayarları kullanarak
            HttpContext.Session.SetString($"PlayGameViewModel_{media.Id}", JsonConvert.SerializeObject(viewModel, settings));

            return View(viewModel);
        }

        // Tahmin Yapma veya Pas Geçme (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PlayGame(PlayGameViewModel submittedModel, string submitButton) // Gelen model ve submitButton
        {

            // Session'dan mevcut oyun durumunu al
            var sessionKey = $"PlayGameViewModel_{submittedModel.MediaId}"; // Doğru MediaId'yi kullan
            var sessionData = HttpContext.Session.GetString(sessionKey);

            if (string.IsNullOrEmpty(sessionData))
            {
                TempData["ErrorMessage"] = "Oyun oturumu bulunamadı. Lütfen oyunu tekrar başlatın.";
                // Hangi kategoriye döneceğini bilmek zor, bu yüzden ana sayfaya veya genel bir oyun listesine yönlendirme
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

            // Gelen tahmini ViewModel'e ata (eğer formdan direkt bağlanmıyorsa)
            viewModel.UserGuess = submittedModel.UserGuess;


            // ViewBag.PreviousCategoryId'yi POST işleminden sonra da View'a taşımak gerekebilir,
            // eğer View bu bilgiye ihtiyaç duyuyorsa (örneğin "Oyun Listesine Dön" linki için).
            // Session'dan okunan viewModel.AllImages.FirstOrDefault()?.Media?.CategoryId gibi bir yolla bulunabilir
            // veya GET sırasında Session'a ayrıca kaydedilebilir.
            // Şimdilik en basit yol, media'nın CategoryId'sini de viewModel içinde saklamak veya
            // GET'teki gibi tekrar veritabanından çekmemek için Session'daki viewModel'den almak.
            // Media'yı tekrar çekmemek için CategoryId'yi PlayGameViewModel'e ekleyebiliriz.
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
                return View(viewModel); // Oyun zaten bitmişse bir şey yapma, mevcut durumu göster
            }

            bool guessedCorrectly = false;

            if (submitButton == "guess") // Tahmin butonu tıklandıysa
            {
                if (!string.IsNullOrWhiteSpace(viewModel.UserGuess)) // viewModel.UserGuess'i kullan
                {
                    if (viewModel.UserGuess.Trim().Equals(viewModel.MediaTitle.Trim(), System.StringComparison.OrdinalIgnoreCase))
                    {
                        guessedCorrectly = true;
                        viewModel.IsCorrect = true;
                        viewModel.GameOver = true;
                        viewModel.Message = "Bildiniz!";
                        // Doğru bilince son resmi göster (isteğe bağlı)
                        if (viewModel.AllImages.Any()) // Resim listesinin boş olmadığından emin ol
                        {
                            viewModel.CurrentImagePath = viewModel.AllImages.Last().ImagePath;
                            viewModel.CurrentImageIndex = viewModel.AllImages.Count - 1;
                        }
                    }
                    else
                    {
                        viewModel.Message = "Yanlış tahmin!"; // Yanlış tahminde mesaj
                    }
                }
                else
                {
                    viewModel.Message = "Lütfen bir tahmin girin."; // Boş tahmin durumu
                }
            }

            // "pass" (Pas geç) veya yanlış tahmin durumunda (veya boş tahmin) bir sonraki resme geç
            if (!guessedCorrectly) // Eğer doğru bilinmediyse (yanlış tahmin veya pas geçildiyse)
            {
                // Sadece yanlış tahminde deneme hakkını artır
                if (submitButton == "guess")
                {
                    viewModel.Attempts++;
                }

                // Deneme hakkı varsa ve son resme gelinmediyse → bir sonraki resme geç
                if (viewModel.Attempts < viewModel.MaxAttempts &&
                    viewModel.CurrentImageIndex + 1 < viewModel.AllImages.Count)
                {
                    viewModel.CurrentImageIndex++;
                    viewModel.CurrentImagePath = viewModel.AllImages[viewModel.CurrentImageIndex].ImagePath;

                    if (submitButton == "pass")
                    {
                        viewModel.Message = ""; // Pas geçildiğinde mesaj temizle
                    }
                }
                else
                {
                    // Oyun bitti: ya deneme hakkı bitti ya da son görsele gelindi
                    viewModel.GameOver = true;
                    viewModel.Message = $"Bilemediniz! Doğru Cevap: {viewModel.MediaTitle}";

                    if (viewModel.AllImages.Any())
                    {
                        viewModel.CurrentImagePath = viewModel.AllImages.Last().ImagePath;
                        viewModel.CurrentImageIndex = viewModel.AllImages.Count - 1;
                    }
                }
            }


            // Güncellenmiş oyun durumunu session'a kaydet
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
