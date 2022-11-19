using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using Support.Discord.Handler;
using Support.Discord.Models;
using Support.Discord.Services;

namespace Support.Discord
{
    internal class Program
    {
        static Task Main(string[] args) => new Program().MainAsync();

        public static readonly DiscordSocketClient client = new DiscordSocketClient();

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static readonly BotConfiguration configuration = JsonConvert.DeserializeObject<BotConfiguration>(File.ReadAllText("configuration.json"));

        public async Task MainAsync()
        {
            LogManager.Configuration = new XmlLoggingConfiguration("NLog.config");

            client.Log += Log;

            client.Ready += Client_Ready;

            client.SlashCommandExecuted += CommandHandler.HandleCommand;

            client.ButtonExecuted += ButtonHandler.HandleButton;

            client.ModalSubmitted += ModalHandler.HandleModal;

            client.SelectMenuExecuted += MenuHandler.HandleMenu;

            await client.SetGameAsync("if tickets were updated...", type: ActivityType.Watching);

            // Some alternative options would be to keep your token in an Environment Variable or a standalone file.
            // var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
            // var token = File.ReadAllText("token.txt");
            // var token = JsonConvert.DeserializeObject<BotConfiguration>(File.ReadAllText("configuration.json")).Token;

            await client.LoginAsync(TokenType.Bot, configuration.Token);
            await client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            logger.Info(msg.ToString());
            return Task.CompletedTask;
        }

        public async Task Client_Ready()
        {
            await SupportService.ConnectHub();

            await CommandHandler.InitializeCommands();

            // await HandleRegularUpdateEvent();

        }
    }
}