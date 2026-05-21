using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Kyrsova.Commands;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

namespace Kyrsova.Services
{
    // BackgroundService дозволяє боту працювати у фоні разом із нашим апі
    public class BotService : BackgroundService
    {
        private DiscordClient _client;

        public BotService()
        {
            // налаштовуємо клієнт бота
            _client = new DiscordClient(new DiscordConfiguration()
            {
                Token = "DISCORD_TOKEN_HERE",
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents // дозвіл читати повідомлення
            });

            // префікс для команд
            var commandsConfig = _client.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new[] { "!" } 
            });

            // вмикаємо модуль інтерактивності (для кнопок)
            _client.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromMinutes(2) // кнопки будуть активні 2 хвилини після виклику
            });

            // реєструємо наш клас із командами
            commandsConfig.RegisterCommands<PexelsCommands>();
        }

        // цей метод автоматично викликається при запуску програми
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _client.ConnectAsync(); // підключаємось до серверів Discord

            // затримуємо задачу назавжди, щоб бот не вимикався
            await Task.Delay(-1, stoppingToken);
        }
    }
}
