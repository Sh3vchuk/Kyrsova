using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System.Threading.Tasks;

namespace Kyrsova.Commands
{
    public class PexelsCommands : BaseCommandModule
    {
        [Command("start")]
        public async Task StartCommand(CommandContext ctx)
        {
            // створюємо красиву картку
            var embed = new DiscordEmbedBuilder()
                .WithTitle("👋 Привіт! Я бот для пошуку зображень")
                .WithDescription("Я допоможу тобі знайти найкращі фотографії у високій якості. Ось що я вмію:")
                .AddField("🔎 !search [тема]", "Пошук фотографій за ключовим словом (наприклад: !search nature)")
                .AddField("🌟 !popular", "Показати топ найпопулярніших фотографій на Pexels")
                .AddField("👀 !details [id]", "Отримати детальну інформацію про конкретне фото за його айді")
                .AddField("❤️ !favorites", "Відкрити меню улюблених фотографій")
                .WithColor(DiscordColor.Azure); // колір смужки збоку

            // відправляємо картку в чат
            await ctx.RespondAsync(embed);
        }

        [Command("search")]
        public async Task SearchCommand(CommandContext ctx, [RemainingText] string zapyt)
        {
            // перевіряємо, чи користувач взагалі ввів слово для пошуку
            if (string.IsNullOrWhiteSpace(zapyt))
            {
                await ctx.RespondAsync("🟥 Будь ласка, вкажи тему для пошуку! Наприклад: `!search nature`");
                return;
            }

            // надсилаємо тимчасове повідомлення, щоб користувач бачив, що бот працює
            var loadingMsg = await ctx.RespondAsync($"⏳ Шукаю найкращі фото за запитом: **{zapyt}**...");

            // звертаємося до нашого сервісу 
            var pexelsService = new Kyrsova.Services.PexelsService();
            var photos = await pexelsService.SearchPhotosAsync(zapyt);

            // якщо нічого не знайдено або сталася помилка
            if (photos == null || photos.Count == 0)
            {
                await loadingMsg.ModifyAsync($"На жаль, за темою **{zapyt}** нічого не знайдено 😢");
                return;
            }

            // видаляємо минуле тимчасове повідомлення
            await loadingMsg.DeleteAsync();

            int currentIndex = 0; // починаємо з першої картинки (індекс 0)

            // створюємо наші кнопки
            var btnPrev = new DiscordButtonComponent(ButtonStyle.Primary, "prev", "⬅️");
            var btnFav = new DiscordButtonComponent(ButtonStyle.Success, "fav", "❤️ Додати");
            var btnNext = new DiscordButtonComponent(ButtonStyle.Primary, "next", "➡️");

            // локальний метод для створення сторінки
            DiscordEmbedBuilder GeneratePage()
            {
                // беремо поточну фотку з відфільтрованого списку
                var currentPhoto = photos[currentIndex];

                // формуємо красиву картку з картинкою
                return new DiscordEmbedBuilder()
                    .WithTitle($"Результат пошуку: {zapyt}")
                    .WithDescription($"📸 Автор: **{currentPhoto.Photographer}**\n🆔 ID: {currentPhoto.Id} \nВсього знайдено (після фільтрації): {photos.Count}\nФото {currentIndex + 1} з {photos.Count}")
                    .WithImageUrl(currentPhoto.Src.Original) // вставляємо саме зображення
                    .WithColor(DiscordColor.SpringGreen);
            }

            // формуємо повідомлення з картинкою та прикріплюємо кнопки знизу
            var messageBuilder = new DiscordMessageBuilder()
                .WithEmbed(GeneratePage())
                .AddComponents(btnPrev, btnFav, btnNext);

            // надсилаємо готову картку
            var msg = await ctx.RespondAsync(messageBuilder);

            // обробка кліків
            var interactivity = ctx.Client.GetInteractivity();

            // запускаємо нескінченний цикл, який чекає натискань
            while (true)
            {
                // чекаємо на клік саме по нашому повідомленню і саме від того, хто викликав команду
                var result = await interactivity.WaitForButtonAsync(msg, ctx.User, TimeSpan.FromMinutes(2));

                if (result.TimedOut)
                {
                    // якщо минуло 2 хвилини - просто прибираємо кнопки
                    var timeoutBuilder = new DiscordMessageBuilder().WithEmbed(GeneratePage());
                    timeoutBuilder.ClearComponents(); // викликаємо окремим рядком

                    await msg.ModifyAsync(timeoutBuilder);
                    break;
                }

                // перевіряємо, яку саме кнопку натиснули
                if (result.Result.Id == "next")
                {
                    currentIndex++;
                    if (currentIndex >= photos.Count) currentIndex = 0; // гортаємо по колу

                    // одразу оновлюємо повідомлення (вирішує проблему з помилкою взаємодії)
                    await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder().AddEmbed(GeneratePage()).AddComponents(btnPrev, btnFav, btnNext));
                }
                else if (result.Result.Id == "prev")
                {
                    currentIndex--;
                    if (currentIndex < 0) currentIndex = photos.Count - 1; // гортаємо у зворотний бік

                    // одразу оновлюємо повідомлення
                    await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder().AddEmbed(GeneratePage()).AddComponents(btnPrev, btnFav, btnNext));
                }

                else if (result.Result.Id == "fav")
                {
                    // погоджуємося з кліком, щоб дс не видав помилку
                    await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                    // звертаємося до нашого сервісу бази даних
                    var dbService = new Kyrsova.Services.DBService();

                    try
                    {
                        // передаємо айді користувача дс, айді фотографії та посилання на неї
                        await dbService.AddFavoriteAsync(ctx.User.Id, photos[currentIndex].Id, photos[currentIndex].Src.Original.ToString());

                        await ctx.RespondAsync($"✅ Фото `{photos[currentIndex].Id}` успішно збережено у твої улюблені!");
                    }
                    catch (System.Exception)
                    {
                        // Якщо сталася помилка (наприклад, база вимкнена, або ти намагаєшся додати те саме фото вдруге)
                        await ctx.RespondAsync($"⚠️ Не вдалося зберегти фото. Можливо, воно вже є у твоєму списку!");
                    }
                }
            }
        }


        [Command("favorites")]
        public async Task FavoritesCommand(CommandContext ctx)
        {
            // звертаємося до бази даних
            var dbService = new Kyrsova.Services.DBService();
            var favUrls = await dbService.GetFavoritesAsync(ctx.User.Id);

            // якщо список порожній - кажемо про це
            if (favUrls == null || favUrls.Count == 0)
            {
                await ctx.RespondAsync("💔 У тебе ще немає збережених фотографій!");
                return;
            }

            int currentIndex = 0; // починаємо з першої картинки

            // створюємо кнопки 
            var btnprev = new DiscordButtonComponent(ButtonStyle.Primary, "prev", "⬅️");
            var btnremove = new DiscordButtonComponent(ButtonStyle.Danger, "remove", "💔 Видалити");
            var btnnext = new DiscordButtonComponent(ButtonStyle.Primary, "next", "➡️");

            // локальний метод генерації сторінки
            DiscordEmbedBuilder GenerateFavPage()
            {
                return new DiscordEmbedBuilder()
                    .WithTitle("❤️ Твої улюблені фотографії")
                    .WithDescription($"Фото {currentIndex + 1} з {favUrls.Count}")
                    .WithImageUrl(favUrls[currentIndex].PhotoUrl) // дістаємо посилання з нашого об'єкта
                    .WithColor(DiscordColor.HotPink);
            }

            var messageBuilder = new DiscordMessageBuilder()
                .WithEmbed(GenerateFavPage())
                .AddComponents(btnprev, btnremove, btnnext); // додаємо всі три кнопки відразу

            var msg = await ctx.RespondAsync(messageBuilder);
            var interactivity = ctx.Client.GetInteractivity();

            // цикл для гортання 
            while (true)
            {
                var result = await interactivity.WaitForButtonAsync(msg, ctx.User, TimeSpan.FromMinutes(2));

                if (result.TimedOut)
                {
                    var timeoutBuilder = new DiscordMessageBuilder().WithEmbed(GenerateFavPage());
                    timeoutBuilder.ClearComponents(); // правильне безпечне видалення кнопок при тайм-ауті
                    await msg.ModifyAsync(timeoutBuilder);
                    break;
                }

                if (result.Result.Id == "next")
                {
                    currentIndex++;
                    if (currentIndex >= favUrls.Count) currentIndex = 0;

                    await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder().AddEmbed(GenerateFavPage()).AddComponents(btnprev, btnremove, btnnext));
                }

                else if (result.Result.Id == "prev")
                {
                    currentIndex--;
                    if (currentIndex < 0) currentIndex = favUrls.Count - 1;

                    await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder().AddEmbed(GenerateFavPage()).AddComponents(btnprev, btnremove, btnnext));
                }

                else if (result.Result.Id == "remove")
                {
                    // видаляємо з бази даних назавжди
                    await dbService.RemoveFavoriteAsync(ctx.User.Id, favUrls[currentIndex].PhotoUrl);

                    // видаляємо з пам'яті бота прямо зараз
                    favUrls.RemoveAt(currentIndex);

                    // перевірка на порожній список
                    if (favUrls.Count == 0)
                    {
                        var emptyBuilder = new DiscordInteractionResponseBuilder()
                            .WithContent("Список улюблених порожній! Ти щойно видалив останнє фото 🚨");
                        emptyBuilder.ClearComponents(); // правильно прибираємо кнопки

                        await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, emptyBuilder);
                        break; // зупиняємо цикл, бо більше немає що гортати
                    }

                    // зсув індексу, якщо видалили останню фотку в списку
                    if (currentIndex >= favUrls.Count) currentIndex = favUrls.Count - 1;

                    // показуємо наступну (або попередню) фотку, яка стала на це місце
                    await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder().AddEmbed(GenerateFavPage()).AddComponents(btnprev, btnremove, btnnext));
                }

            }
        }

        [Command("popular")]
        public async Task PopularCommand(CommandContext ctx)
        {
            // повідомлення про завантаження
            var loadingMsg = await ctx.RespondAsync("⏳ Завантажую топ найпопулярніших фотографій з Pexels...");

            // звертаємося до сервісу за популярними фото 
            var pexelsService = new Kyrsova.Services.PexelsService();
            var photos = await pexelsService.GetPopularPhotosAsync();

            if (photos == null || photos.Count == 0)
            {
                await loadingMsg.ModifyAsync("На жаль, не вдалося завантажити популярні фотографії 😢");
                return;
            }

            await loadingMsg.DeleteAsync();

            int currentIndex = 0;

            // створюємо наші 3 кнопки
            var btnPrev = new DiscordButtonComponent(ButtonStyle.Primary, "prev", "⬅️");
            var btnFav = new DiscordButtonComponent(ButtonStyle.Success, "fav", "❤️ Додати");
            var btnNext = new DiscordButtonComponent(ButtonStyle.Primary, "next", "➡️");

            // генерація сторінки 
            DiscordEmbedBuilder GeneratePage()
            {
                var currentPhoto = photos[currentIndex];
                return new DiscordEmbedBuilder()
                    .WithTitle("🌟 Найпопулярніші фото на Pexels прямо зараз")
                    .WithDescription($"📸 Автор: **{currentPhoto.Photographer}**\n🆔 ID: {currentPhoto.Id}\nФото {currentIndex + 1} з {photos.Count}")
                    .WithImageUrl(currentPhoto.Src.Original)
                    .WithColor(DiscordColor.Gold); // золотий колір для популярних!
            }

            var messageBuilder = new DiscordMessageBuilder()
                .WithEmbed(GeneratePage())
                .AddComponents(btnPrev, btnFav, btnNext);

            var msg = await ctx.RespondAsync(messageBuilder);
            var interactivity = ctx.Client.GetInteractivity();

            // цикл обробки кнопок (такий самий як у search)
            while (true)
            {
                var result = await interactivity.WaitForButtonAsync(msg, ctx.User, TimeSpan.FromMinutes(2));

                if (result.TimedOut)
                {
                    var timeoutBuilder = new DiscordMessageBuilder().WithEmbed(GeneratePage());
                    timeoutBuilder.ClearComponents();
                    await msg.ModifyAsync(timeoutBuilder);
                    break;
                }

                if (result.Result.Id == "next")
                {
                    currentIndex++;
                    if (currentIndex >= photos.Count) currentIndex = 0;

                    await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder().AddEmbed(GeneratePage()).AddComponents(btnPrev, btnFav, btnNext));
                }
                else if (result.Result.Id == "prev")
                {
                    currentIndex--;
                    if (currentIndex < 0) currentIndex = photos.Count - 1;

                    await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder().AddEmbed(GeneratePage()).AddComponents(btnPrev, btnFav, btnNext));
                }
                else if (result.Result.Id == "fav")
                {
                    await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                    var dbService = new Kyrsova.Services.DBService();
                    try
                    {
                        // Зберігаємо популярне фото в базу
                        await dbService.AddFavoriteAsync(ctx.User.Id, photos[currentIndex].Id, photos[currentIndex].Src.Original.ToString());
                        await ctx.RespondAsync($"✅ Фото `{photos[currentIndex].Id}` успішно збережено у твої улюблені!");
                    }
                    catch (System.Exception)
                    {
                        await ctx.RespondAsync($"⚠️ Не вдалося зберегти фото. Можливо, воно вже є у твоєму списку!");
                    }
                }
            }
        }

        [Command("details")]
        public async Task DetailsCommand(CommandContext ctx, string photoIdRaw)
        {
            // перевіряємо, чи користувач взагалі ввів ID фотографії
            if (string.IsNullOrWhiteSpace(photoIdRaw))
            {
                await ctx.RespondAsync("🟥 Будь ласка, вкажи ID фотографії! Наприклад: `!details 19852036`");
                return;
            }

            // намагаємося перетворити текст на число long, щоб захистити бота від букв чи опечаток
            if (!long.TryParse(photoIdRaw, out long photoId))
            {
                await ctx.RespondAsync("🟥 ID фотографії має складатися лише з чисел!");
                return;
            }

            // тимчасове повідомлення
            var loadingMsg = await ctx.RespondAsync($"⏳ Отримую детальну інформацію про фото з ID `{photoId}`...");

            // викликаємо 2-й метод нашого сервісу Pexels
            var pexelsService = new Kyrsova.Services.PexelsService();
            var photo = await pexelsService.GetPhotoDetailsAsync(photoId);

            // якщо фото з таким номером не існує
            if (photo == null)
            {
                await loadingMsg.ModifyAsync($"❌ Фото з ID `{photoId}` не знайдено на Pexels 😢");
                return;
            }

            await loadingMsg.DeleteAsync();

            // створюємо одну кнопку для збереження в базу прямо звідси
            var btnFav = new DiscordButtonComponent(ButtonStyle.Success, "fav_details", "❤️ Додати в улюблені");

            // локальний метод створення картки (виводимо розширену інфу: розмір у пікселях та лінк на автора)
            DiscordEmbedBuilder GenerateDetailsPage()
            {
                return new DiscordEmbedBuilder()
                    .WithTitle($"👀 Детальний перегляд фото")
                    .WithDescription($"📸 Автор: **{photo.Photographer}**\n🆔 ID: `{photo.Id}`\n📐 Розширення: {photo.Width} x {photo.Height} пікселів\n🌐 [Профіль фотографа на Pexels]({photo.PhotographerUrl})")
                    .WithImageUrl(photo.Src.Original)
                    .WithColor(DiscordColor.Purple); 
            }

            var messageBuilder = new DiscordMessageBuilder()
                .WithEmbed(GenerateDetailsPage())
                .AddComponents(btnFav); // прикріплюємо одну кнопку

            var msg = await ctx.RespondAsync(messageBuilder);
            var interactivity = ctx.Client.GetInteractivity();

            // цикл для обробки єдиної кнопки
            while (true)
            {
                var result = await interactivity.WaitForButtonAsync(msg, ctx.User, TimeSpan.FromMinutes(2));

                if (result.TimedOut)
                {
                    var timeoutBuilder = new DiscordMessageBuilder().WithEmbed(GenerateDetailsPage());
                    timeoutBuilder.ClearComponents();
                    await msg.ModifyAsync(timeoutBuilder);
                    break;
                }

                if (result.Result.Id == "fav_details")
                {
                    await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                    var dbService = new Kyrsova.Services.DBService();
                    try
                    {
                        await dbService.AddFavoriteAsync(ctx.User.Id, photo.Id, photo.Src.Original.ToString());
                        await ctx.RespondAsync($"✅ Фото `{photo.Id}` успішно збережено у твої улюблені!");
                    }
                    catch (System.Exception)
                    {
                        await ctx.RespondAsync($"⚠️ Не вдалося зберегти фото. Можливо, воно вже є у твоєму списку!");
                    }
                }
            }
        }
    }
}