using Discord.WebSocket;
using NLog;
using Support.Discord.Services;

namespace Support.Discord.Handler
{
    internal class ComponentHandler
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static async Task HandleMenu(SocketMessageComponent component)
        {
            logger.Info($"User interacted with menu {component.Data.CustomId}");
            switch (component.Data.CustomId.Split("$")[0])
            {
                case "watch-menu":
                    await HandleWatchComponent(component);
                    break;
            }
        }

        private static async Task HandleWatchComponent(SocketMessageComponent menu)
        => await SupportService.WatchComponent(menu);
    }
}
