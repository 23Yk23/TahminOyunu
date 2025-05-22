using BusinessLayer.Concrete;
using DataAccessLayer.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http;
using TahminOyunu.Models;

namespace TahminOyunu.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;


        CategoryManager cm = new CategoryManager(new EfCategoryRepository());
        MediaManager mm = new MediaManager(new EfMediaRepository());

        // API anahtarını buraya yapıştır. Uygulamayı dağıtırken appsettings.json'dan okumak daha güvenlidir.
        private readonly string _tmdbApiKey;
        private readonly string _tmdbApiBaseUrl = "https://api.themoviedb.org/3/";
        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;

            _tmdbApiKey = _configuration["TmdbApiKey"];
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
                return RedirectToAction("GameList", new { id = ViewBag.PreviousCategoryId ?? 0 });
            }

            ViewBag.PreviousCategoryId = media.CategoryId;

            var sortedImages = media.MediaImages.OrderBy(img => img.OrderNo).ToList();

            if (!sortedImages.Any())
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

            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            HttpContext.Session.SetString($"PlayGameViewModel_{media.Id}", JsonConvert.SerializeObject(viewModel, settings));

            return View(viewModel);
        }

        // Tahmin Yapma veya Pas Geçme (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PlayGame(PlayGameViewModel submittedModel, string submitButton) // Gelen model ve submitButton
        {
            var sessionKey = $"PlayGameViewModel_{submittedModel.MediaId}"; // Doğru MediaId'yi kullan
            var sessionData = HttpContext.Session.GetString(sessionKey);

            if (string.IsNullOrEmpty(sessionData))
            {
                TempData["ErrorMessage"] = "Oyun oturumu bulunamadı. Lütfen oyunu tekrar başlatın.";
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

            viewModel.UserGuess = submittedModel.UserGuess;

            if (viewModel.AllImages.Any() && viewModel.AllImages.First().Media != null)
            {
                ViewBag.PreviousCategoryId = viewModel.AllImages.First().Media.CategoryId;
            }
            else
            {
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
                        if (viewModel.AllImages.Any())
                        {
                            viewModel.CurrentImagePath = viewModel.AllImages.Last().ImagePath;
                            viewModel.CurrentImageIndex = viewModel.AllImages.Count - 1;
                        }
                    }
                    else
                    {
                        viewModel.Message = "Yanlış tahmin!";
                    }
                }
                else
                {
                    viewModel.Message = "Lütfen bir tahmin girin.";
                }
            }

            if (!guessedCorrectly) // Eğer doğru bilinmediyse (yanlış tahmin veya pas geçildiyse)
            {
                if (submitButton == "guess")
                {
                    viewModel.Attempts++;
                }

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
                    viewModel.GameOver = true;
                    viewModel.Message = $"Bilemediniz! Doğru Cevap: {viewModel.MediaTitle}";

                    if (viewModel.AllImages.Any())
                    {
                        viewModel.CurrentImagePath = viewModel.AllImages.Last().ImagePath;
                        viewModel.CurrentImageIndex = viewModel.AllImages.Count - 1;
                    }
                }
            }

            HttpContext.Session.SetString(sessionKey, JsonConvert.SerializeObject(viewModel));

            return View(viewModel);
        }



        // Bu metot hem film hem de dizi sonuçlarını döndürecek
        [HttpGet]
        public async Task<JsonResult> SearchMoviesAndTv(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return Json(new List<object>()); // Boş terim gelirse boş liste döndür
            }

            var client = _httpClientFactory.CreateClient();
            // The Movie Database (TMDb) multi-search API uç noktasını kullanıyoruz
            // Bu, tek bir aramayla hem filmleri hem dizileri bulur
            var requestUrl = $"{_tmdbApiBaseUrl}search/multi?api_key={_tmdbApiKey}&query={Uri.EscapeDataString(term)}&language=tr-TR";

            try
            {
                var response = await client.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode(); // HTTP hata kodlarını kontrol et

                var jsonString = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(jsonString);

                var results = new List<object>();

                if (data["results"] is JArray items)
                {
                    foreach (var item in items)
                    {
                        string mediaType = item["media_type"]?.ToString();
                        string title = "";

                        // Eğer film ise 'title' özelliğini kullan, dizi ise 'name' özelliğini
                        if (mediaType == "movie")
                        {
                            title = item["title"]?.ToString();
                        }
                        else if (mediaType == "tv")
                        {
                            title = item["name"]?.ToString();
                        }

                        // Sadece geçerli bir başlık varsa listeye ekle
                        if (!string.IsNullOrEmpty(title))
                        {
                            // jQuery UI Autocomplete için { label: "Görünecek Metin", value: "Input'a Yazılacak Metin" } formatında döndürüyoruz
                            results.Add(new { label = title, value = title });
                        }
                    }
                }
                // İlk 10 sonucu al (isteğe bağlı, API'dan daha fazla gelirse)
                return Json(results.Take(10).ToList());
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "TMDb API isteği hatası: {ErrorMessage}", ex.Message);
                return Json(new List<object>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Genel hata: {ErrorMessage}", ex.Message);
                return Json(new List<object>());
            }
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
