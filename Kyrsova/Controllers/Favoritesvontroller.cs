using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Kyrsova.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FavoritesController : ControllerBase
    {
        // перший метод гет: отримати всі улюблені фото користувача
        [HttpGet("get-favorites/{discordUserId}")]
        public async Task<IActionResult> GetFavorites(ulong discordUserId)
        {
            var dbService = new Kyrsova.Services.DBService();
            var favs = await dbService.GetFavoritesAsync(discordUserId);

            if (favs == null || favs.Count == 0)
            {
                return NotFound("У цього користувача немає збережених фото.");
            }
            return Ok(favs);
        }

        // другий иетод пост: додати фото в улюблені
        [HttpPost("add-favorite")]
        public async Task<IActionResult> AddFavorite(ulong discordUserId, long photoId, string photoUrl)    
        {
            var dbService = new Kyrsova.Services.DBService();
            try
            {
                await dbService.AddFavoriteAsync(discordUserId, photoId, photoUrl);
                return Ok($"Фото {photoId} успішно додано для користувача {discordUserId}.");
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Помилка додавання: Можливо, фото вже є в базі. Деталі: {ex.Message}");
            }
        }

        // третій метод деліт: видалити фото з улюблених
        [HttpDelete("remove-favorite")]
        public async Task<IActionResult> RemoveFavorite(ulong discordUserId, string photoUrl)
        {
            var dbService = new Kyrsova.Services.DBService();
            try
            {
                await dbService.RemoveFavoriteAsync(discordUserId, photoUrl);
                return Ok($"Фото успішно видалено зі списку користувача {discordUserId}.");
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Помилка видалення: {ex.Message}");
            }
        }
    }
}