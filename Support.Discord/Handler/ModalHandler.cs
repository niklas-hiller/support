using Discord.WebSocket;
using Support.Discord.Services;

namespace Support.Discord.Handler
{
    internal static class ModalHandler
    {
        public static async Task HandleModal(SocketModal modal)
        {
            switch (modal.Data.CustomId.Split('$')[0])
            {
                case "bug-modal":
                    await SupportService.CreateTicketModal(modal);
                    break;
                case "request-modal":
                    await SupportService.CreateTicketModal(modal);
                    break;
            }
        }
    }
}
