using Discord.WebSocket;
using Support.Discord.Services;
using Support.Shared.Enums;

namespace Support.Discord.Handler
{
    internal static class ModalHandler
    {
        public static async Task HandleModal(SocketModal modal)
        {
            switch (modal.Data.CustomId)
            {
                case "bug-modal":
                    await SupportService.CreateTicket(modal, ETicketType.Bug);
                    break;
                case "request-modal":
                    await SupportService.CreateTicket(modal, ETicketType.Request);
                    break;
            }
        }
    }
}
