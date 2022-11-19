﻿using Discord.WebSocket;
using Support.Discord.Services;
using Support.Shared;

namespace Support.Discord.Handler
{
    public static class ModalHandler
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