using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kyrsova.Services
{
    public class DBService
    {
        // рядок підключення (адреса, ім'я бази і пароль)
        private readonly string _connectionString = "Host=localhost;Port=5432;Database=Pexelsbot;Username=postgres;Password=1488;";

        // метод для збереження картинки в базу
        public async Task AddFavoriteAsync(ulong dsUserId, long photoId, string photoUrl)
        {
            try
            {
                // з'єднання з базою
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // пишемо SQL-запит
                string sql = "INSERT INTO public.favorites (\"DsUserId\", \"Photoid\", \"Photourl\") VALUES (@userId, @photoId, @photoUrl)";

                using var command = new NpgsqlCommand(sql, connection);
                // діскорд айді занадто великий для звичайного int, тому зберігаємо як long
                command.Parameters.AddWithValue("userId", (long)dsUserId);
                command.Parameters.AddWithValue("photoId", photoId);
                command.Parameters.AddWithValue("photoUrl", photoUrl);

                //виконуємо запит (записуємо дані)
                await command.ExecuteNonQueryAsync();
            }
            catch (System.Exception ex)
            {
                // виводимо помилку в консоль, а потім передаємо її далі, щоб у дс бот написав "Не вдалося зберегти фото"
                System.Console.ForegroundColor = System.ConsoleColor.Red;
                System.Console.WriteLine($"[ПОМИЛКА БАЗИ ДАНИХ - ЗБЕРЕЖЕННЯ]: {ex.Message}");
                System.Console.ResetColor();
                throw;
            }
        }

        // метод для отримання списку улюблених фотографій 
        public async Task<List<Kyrsova.Models.modelsBD>> GetFavoritesAsync(ulong discordUserId)
        {
            try
            {
                var favoriteslist = new List<Kyrsova.Models.modelsBD>(); // створюємо порожній список для моделей

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // SQL-запит: тепер вибираємо всі колонки з таблиці, щоб заповнити модель
                string sql = "SELECT \"Id\", \"DsUserId\", \"Photoid\", \"Photourl\" FROM public.favorites WHERE \"DsUserId\" = @userId";

                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("userId", (long)discordUserId);

                // виконуємо запит, який повертає дані
                using var reader = await command.ExecuteReaderAsync();

                // читаємо рядок за рядком
                while (await reader.ReadAsync())
                {
                    // створюємо об'єкт моделі та заповнюємо його даними з бази відповідно до індексів колонок у запиті
                    var fav = new Kyrsova.Models.modelsBD
                    {
                        Id = reader.GetInt32(0),
                        DsUserId = reader.GetInt64(1),
                        PhotoId = reader.GetInt64(2),
                        PhotoUrl = reader.GetString(3) // посилання тепер лежить під індексом 3
                    };

                    favoriteslist.Add(fav); // додаємо готову модель у список
                }

                return favoriteslist; // повертаємо готовий список
            }
            catch (System.Exception ex)
            {
                System.Console.ForegroundColor = System.ConsoleColor.Red;
                System.Console.WriteLine($"[ПОМИЛКА БАЗИ ДАНИХ - ЧИТАННЯ]: {ex.Message}");
                System.Console.ResetColor();
                // при помилці повертаємо порожній список моделей
                return new List<Kyrsova.Models.modelsBD>();
            }
        }

        // метод для видалення фотографії з улюблених
        public async Task RemoveFavoriteAsync(ulong discordUserId, string photoUrl)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // SQL-запит: DELETE (видалити) FROM (з таблиці) WHERE (де умови збігаються)
                string sql = "DELETE FROM public.favorites WHERE \"DsUserId\" = @userId AND \"Photourl\" = @url";

                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("userId", (long)discordUserId);
                command.Parameters.AddWithValue("url", photoUrl);

                await command.ExecuteNonQueryAsync();
            }
            catch (System.Exception ex)
            {
                System.Console.ForegroundColor = System.ConsoleColor.Red;
                System.Console.WriteLine($"[ПОМИЛКА БАЗИ ДАНИХ - ВИДАЛЕННЯ]: {ex.Message}");
                System.Console.ResetColor();
            }
        }
    }
}