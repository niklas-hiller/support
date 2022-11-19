using Discord.WebSocket;
using Support.Discord.Models;
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
            DiscordTicket ticket = SupportService.GetTicketById(ticketId);
            if (isWatching)
            {
                ticket.Watchers.Add(menu.User.Id);
                await menu.RespondAsync($"You will now be informed if there's any update regarding the ticket {ticket.Id}", ephemeral: true);
            }
            else
            {
                ticket.Watchers.Remove(menu.User.Id);
                await menu.RespondAsync($"You will no longer be informed if there's any update regarding the ticket {ticket.Id}", ephemeral: true);
            }
        }
    }
}
