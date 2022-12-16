using Discord.WebSocket;
using Support.Discord.Services;

namespace Support.Discord.Handler
{
    internal class ComponentHandler
    {
        public static async Task HandleMenu(SocketMessageComponent component)
        {
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
