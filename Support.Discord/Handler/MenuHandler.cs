using Discord.WebSocket;
using Support.Discord.Services;

namespace Support.Discord.Handler
{
    internal class MenuHandler
    {
        public static async Task HandleMenu(SocketMessageComponent menu)
        {
            switch (menu.Data.CustomId.Split(" ")[0])
            {
                case "watch-menu":
                    await HandleWatchMenu(menu);
                    break;
            }
        }

        private static async Task HandleWatchMenu(SocketMessageComponent menu)
        {
            bool isWatching = string.Join(", ", menu.Data.Values) == "watch" ? true : false;
            string ticketId = menu.Data.CustomId.Split(" ")[1];

            SupportService.SetWatchTicket(ticketId, menu.User.Id, isWatching);
            if (isWatching)
            {
                await menu.RespondAsync($"You will now be informed if there's any update regarding the ticket {ticketId}", ephemeral: true);
            }
            else
            {
                await menu.RespondAsync($"You will no longer be informed if there's any update regarding the ticket {ticketId}", ephemeral: true);
            }
        }
    }
}
