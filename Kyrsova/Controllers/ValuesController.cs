using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Kyrsova.Models;
using System.Linq;
using System;

namespace Kyrsova.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PexelsController : ControllerBase
    {
        [HttpGet("get-photo")] // 1-й метод: основний пошук
        public async Task<IActionResult> GetPhotoInfo(string zapyt)
        {
            using HttpClient client = new HttpClient();

            string apikey = "PEXELS_API_KEY";

            client.DefaultRequestHeaders.Add("Authorization", apikey);

            string url = $"https://api.pexels.com/v1/search?query={zapyt}&per_page=15"; 

            HttpResponseMessage response = await client.GetAsync(url);
            string jsonresponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode) // перевіряємо, чи успішно пройшов запит до Pexels
            {
                return BadRequest($"Помилка від Pexels! Статус: {response.StatusCode}. Деталі: {jsonresponse}");
            }

            PexelsSearch pexels = JsonConvert.DeserializeObject<PexelsSearch>(jsonresponse); // перетворюємо текст на готові класи

            if (pexels == null || pexels.Photos == null || pexels.Photos.Length == 0) // перевіряємо, чи взагалі прийшли якісь дані і чи є в масиві фотографії
            {
                return NotFound($"На жаль, за темою '{zapyt}' нічого не знайдено.");
            }

            var processedphotos = pexels.Photos
                .Where(p => p.Width > p.Height).OrderBy(p => p.Photographer).ToList();
            // фільтраціял: для кожної картинки p, перевіряємо, чи її ширина більша за її висоту 
            // сортування: список картинок тепер відсортований за алфавітом імен фотографів (від А до Z)
            // ToList() просто допоміг зберегти цей  результат у змінну processedphotos

            return Ok(new // красива відповідь для бота
            {
                SearchTopic = zapyt,
                Message = "Фотографії знайдено",
                TotalResultsOnServer = pexels.TotalResults,
                ProcessedPhotosCount = processedphotos.Count, // скільки фото пройшли фільтр

                Photos = processedphotos.Select(p => new // виводимо масив уже відфільтрованих фотографій
                {
                    p.Id,
                    OriginalLink = p.Src?.Original
                })
            });
        }

        [HttpGet("details/{id}")] // 2-й метод: пошук деталей за ID
        public async Task<IActionResult> GetPhotoDetails(long id)
        {
            using HttpClient client = new HttpClient();
            string apikey = "PEXELS_API_KEY";

            string url = $"https://api.pexels.com/v1/photos/{id}"; // формуємо URL до конкретної фотографії за її ID

            client.DefaultRequestHeaders.Add("Authorization", apikey);

            HttpResponseMessage response = await client.GetAsync(url);
            string jsonresponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return BadRequest($"Не вдалося отримати деталі фото. Статус: {response.StatusCode}");
            }

            Photo photo = JsonConvert.DeserializeObject<Photo>(jsonresponse);

            if (photo == null)
            {
                return NotFound("Інформацію про фото не знайдено.");
            }

            return Ok(new
            {
                Message = "Детальна інформація про фото:",
                Photographer = photo.Photographer,
                Dimensions = $"{photo.Width} x {photo.Height}", // точні розміри
                HighQualityUrl = photo.Src?.Original,           // посилання на оригінал
                MainColor = photo.AvgColor                      // середній колір фото
            });
        }

        [HttpGet("popular")] // 3-й метод: популярні картинки
        public async Task<IActionResult> GetPopularPhotos()
        {
            using HttpClient client = new HttpClient();
            string apikey = "PEXELS_API_KEY";

            string url = "https://api.pexels.com/v1/curated?per_page=10";

            client.DefaultRequestHeaders.Add("Authorization", apikey);

            HttpResponseMessage response = await client.GetAsync(url);
            string jsonresponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return BadRequest($"Не вдалося отримати популярні фото. Деталі: {jsonresponse}");
            }

            PexelsSearch pexels = JsonConvert.DeserializeObject<PexelsSearch>(jsonresponse); // десеріалізуємо в наш стандартний PexelsSearch, бо структура списку однакова

            return Ok(new
            {
                Message = "Топ популярних картинок на Pexels зараз:",
                Count = pexels.Photos.Length,
                Photos = pexels.Photos.Select(p => new
                {
                    Id = p.Id,
                    Author = p.Photographer,
                    Link = p.Src?.Medium // для прев'ю в боті краще брати середній розмір
                })
            });
        }
    }
}
