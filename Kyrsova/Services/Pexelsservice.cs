using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Kyrsova.Models;
using System.Linq;
using System.Collections.Generic;

namespace Kyrsova.Services
{
    public class PexelsService
    {
        // 1-й метод: основний пошук
        public async Task<List<Photo>> SearchPhotosAsync(string zapyt)
        {
            try
            {
                using HttpClient client = new HttpClient();
                string apikey = "PEXELS_API_KEY";
                client.DefaultRequestHeaders.Add("Authorization", apikey);

                string url = $"https://api.pexels.com/v1/search?query={zapyt}&per_page=15";

                HttpResponseMessage response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode) // перевіряємо, чи успішно пройшов запит до Pexels
                {
                    return null; // для бота просто повертаємо null при помилці
                }

                string jsonresponse = await response.Content.ReadAsStringAsync();
                PexelsSearch pexels = JsonConvert.DeserializeObject<PexelsSearch>(jsonresponse); // перетворюємо текст на готові класи

                if (pexels == null || pexels.Photos == null || pexels.Photos.Length == 0) // перевіряємо, чи взагалі прийшли якісь дані і чи є в масиві фотографії
                {
                    return null;
                }

                var processedphotos = pexels.Photos
                    .Where(p => p.Width > p.Height).OrderBy(p => p.Photographer).ToList();
                // фільтраціял: для кожної картинки p, перевіряємо, чи її ширина більша за її висоту 
                // сортування: список картинок тепер відсортований за алфавітом імен фотографів (від А до Z)
                // ToList() просто допоміг зберегти цей  результат у змінну processedphotos

                return processedphotos; // повертаємо готовий список відфільтрованих фотографій
            }
            catch (System.Exception ex)
            {
                // виводимо точну помилку у консоль для розробника
                System.Console.ForegroundColor = System.ConsoleColor.Red;
                System.Console.WriteLine($"[ПОМИЛКА PEXELS API]: {ex.Message}");
                System.Console.ResetColor(); // повертаємо стандартний колір консолі

                return null; // для Discord залишаємо безпечну відповідь
            }
        }

        // 2-й метод: пошук деталей за ID
        public async Task<Photo> GetPhotoDetailsAsync(long id)
        {
            try
            {
                using HttpClient client = new HttpClient();
                string apikey = "PEXELS_API_KEY";
                string url = $"https://api.pexels.com/v1/photos/{id}"; // формуємо URL до конкретної фотографії за її ID

                client.DefaultRequestHeaders.Add("Authorization", apikey);

                HttpResponseMessage response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                string jsonresponse = await response.Content.ReadAsStringAsync();
                Photo photo = JsonConvert.DeserializeObject<Photo>(jsonresponse);

                return photo; // повертаємо об'єкт однієї фотографії
            }
            catch (System.Exception ex)
            {
                // виводимо точну помилку у консоль для розробника
                System.Console.ForegroundColor = System.ConsoleColor.Red;
                System.Console.WriteLine($"[ПОМИЛКА PEXELS API]: {ex.Message}");
                System.Console.ResetColor(); // повертаємо стандартний колір консолі

                return null; // для Discord залишаємо безпечну відповідь
            }
        }

        // 3-й метод: популярні картинки
        public async Task<List<Photo>> GetPopularPhotosAsync()
        {
            try
            {
                using HttpClient client = new HttpClient();
                string apikey = "PEXELS_API_KEY";
                string url = "https://api.pexels.com/v1/curated?per_page=10";

                client.DefaultRequestHeaders.Add("Authorization", apikey);

                HttpResponseMessage response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                string jsonresponse = await response.Content.ReadAsStringAsync();
                PexelsSearch pexels = JsonConvert.DeserializeObject<PexelsSearch>(jsonresponse); // десеріалізуємо в наш стандартний PexelsSearch, бо структура списку однакова

                if (pexels == null || pexels.Photos == null)
                {
                    return null;
                }

                return pexels.Photos.ToList(); // повертаємо список популярних фото
            }
            catch (System.Exception ex)
            {
                // виводимо точну помилку у консоль для розробника
                System.Console.ForegroundColor = System.ConsoleColor.Red;
                System.Console.WriteLine($"[ПОМИЛКА PEXELS API]: {ex.Message}");
                System.Console.ResetColor(); // повертаємо стандартний колір консолі

                return null; // для Discord залишаємо безпечну відповідь
            }
        }
    }
}
