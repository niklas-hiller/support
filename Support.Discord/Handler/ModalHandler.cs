using Discord.WebSocket;
using NLog;
using Support.Discord.Services;
using System.ComponentModel;

namespace Support.Discord.Handler
{
    internal static class ModalHandler
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static async Task HandleModal(SocketModal modal)
        {
            logger.Info($"User interacted with modal {modal.Data.CustomId}");
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
