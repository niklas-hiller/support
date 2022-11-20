using Discord;
using Discord.WebSocket;
using Support.Discord.Models;
using Support.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Support.Discord.Services
{
    internal static class ComponentService
    {
        private static void AttachTicketStatusComponent(ComponentBuilder builder, ETicketStatus status)
        {
            switch (status)
            {
                case ETicketStatus.Unknown:
                    builder.WithButton("Status: Unknown", customId: "ticket-status-unknown",
                        style: ButtonStyle.Secondary);
                    break;
                case ETicketStatus.Open:
                    builder.WithButton("Status: Open", customId: "ticket-status-open",
                        style: ButtonStyle.Secondary);
                    break;
                case ETicketStatus.In_Progress:
                    builder.WithButton("Status: In Progress", customId: "ticket-status-inprogress",
                        style: ButtonStyle.Primary);
                    break;
                case ETicketStatus.Done:
                    builder.WithButton("Status: Done", customId: "ticket-status-done",
                        style: ButtonStyle.Success);
                    break;
                case ETicketStatus.Declined:
                    builder.WithButton("Status: Declined", customId: "ticket-status-declined",
                        style: ButtonStyle.Danger);
                    break;
            }
        }

        private static void AttachTicketPriorityComponent(ComponentBuilder builder, ETicketPriority priority)
        {
            switch (priority)
            {
                case ETicketPriority.Unknown:
                    builder.WithButton("Priority: Unknown", customId: "ticket-priority-unknown",
                        style: ButtonStyle.Secondary, emote: EmojiService.GetPriorityEmoji(priority));
                    break;
                case ETicketPriority.Trivial:
                    builder.WithButton("Priority: Trivial", customId: "ticket-priority-trivial",
                        style: ButtonStyle.Secondary, emote: EmojiService.GetPriorityEmoji(priority));
                    break;
                case ETicketPriority.Minor:
                    builder.WithButton("Priority: Minor", customId: "ticket-priority-minor",
                        style: ButtonStyle.Secondary, emote: EmojiService.GetPriorityEmoji(priority));
                    break;
                case ETicketPriority.Lowest:
                    builder.WithButton("Priority: Lowest", customId: "ticket-priority-lowest",
                        style: ButtonStyle.Secondary, emote: EmojiService.GetPriorityEmoji(priority));
                    break;
                case ETicketPriority.Low:
                    builder.WithButton("Priority: Low", customId: "ticket-priority-low",
                        style: ButtonStyle.Secondary, emote: EmojiService.GetPriorityEmoji(priority));
                    break;
                case ETicketPriority.Medium:
                    builder.WithButton("Priority: Medium", customId: "ticket-priority-medium",
                        style: ButtonStyle.Secondary, emote: EmojiService.GetPriorityEmoji(priority));
                    break;
                case ETicketPriority.High:
                    builder.WithButton("Priority: High", customId: "ticket-priority-high",
                        style: ButtonStyle.Secondary, emote: EmojiService.GetPriorityEmoji(priority));
                    break;
                case ETicketPriority.Highest:
                    builder.WithButton("Priority: Highest", customId: "ticket-priority-highest",
                        style: ButtonStyle.Secondary, emote: EmojiService.GetPriorityEmoji(priority));
                    break;
                case ETicketPriority.Major:
                    builder.WithButton("Priority: Major", customId: "ticket-priority-major",
                        style: ButtonStyle.Secondary, emote: EmojiService.GetPriorityEmoji(priority));
                    break;
                case ETicketPriority.Critical:
                    builder.WithButton("Priority: Critical", customId: "ticket-priority-critical",
                        style: ButtonStyle.Secondary, emote: EmojiService.GetPriorityEmoji(priority));
                    break;
                case ETicketPriority.Blocker:
                    builder.WithButton("Priority: Blocker", customId: "ticket-priority-blocker",
                        style: ButtonStyle.Secondary, emote: EmojiService.GetPriorityEmoji(priority));
                    break;
            }
        }

        public static MessageComponent GetTicketComponents(DiscordTicket ticket)
        {
            var builder = new ComponentBuilder();
            AttachTicketStatusComponent(builder, ticket.Status);
            AttachTicketPriorityComponent(builder, ticket.Priority);
            return builder.Build();
        }

        private static void AttachTicketWatchComponent(ComponentBuilder builder, string ticketId)
        {
            var menuBuilder = new SelectMenuBuilder()
                .WithCustomId($"watch-menu {ticketId}")
                .WithMinValues(1)
                .WithMaxValues(1)
                .AddOption("Don't Watch", "unwatch", "You will not be informed anytime the ticket gets updated", isDefault: true)
                .AddOption("Watch", "watch", "You will be informed anytime the ticket gets updated");

            builder.WithSelectMenu(menuBuilder);
        }

        public static MessageComponent GetTicketMenuComponent(DiscordTicket ticket)
        {
            var builder = new ComponentBuilder();
            AttachTicketWatchComponent(builder, ticket.Id);
            return builder.Build();
        }
    }
}
