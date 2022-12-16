using Discord.WebSocket;
using Support.Discord.Services;
using Support.Shared.Enums;

namespace Support.Discord.Handler
{
    internal static class ModalHandler
    {
        public static async Task HandleModal(SocketModal modal)
        {
            string[] data = modal.Data.CustomId.Split('$');
            string? custom = data.Length > 1 ? data[1] : null;

            switch (data[0])
            {
                case "bug-modal":
                    await SupportService.CreateTicket(modal, custom, ETicketType.Bug);
                    break;
                case "request-modal":
                    await SupportService.CreateTicket(modal, custom, ETicketType.Request);
                    break;
            }
        }
    }
}
