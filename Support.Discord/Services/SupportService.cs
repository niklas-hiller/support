using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Support.Discord.Models;
using Support.Shared;
using System.Net;

namespace Support.Discord.Services
{
    internal static class SupportService
    {
        private static readonly DiscordSocketClient client = Program.client;
        private static readonly List<DiscordTicket> tickets = new List<DiscordTicket>();
        private static readonly Dictionary<ulong, ulong> supportChannels = new Dictionary<ulong, ulong>();

        public static async Task CreateTicket(SocketModal modal, ETicketType type)
        {
            List<SocketMessageComponentData> components =
                modal.Data.Components.ToList();

            string name = components
                .First(x => x.CustomId == "name").Value;
            string description = components
                .First(x => x.CustomId == "description").Value;
            ulong guildId = modal.GuildId ?? 0;

            DiscordTicket ticket = new DiscordTicket(
                Type: type, Status: ETicketStatus.Open,
                Title: name, Description: description, Author: modal.User.ToString(),
                CreatedAt: DateTimeOffset.Now, DateTimeOffset.Now, guildId);

            tickets.Add(ticket);
            await TransmitTicket(ticket);
            await modal.RespondAsync($"Successfully submitted your ticket. ({ticket.Id})", ephemeral: true);
        }

        private static async Task TransmitTicket(DiscordTicket ticket)
        {
            // Send ticket to hub
        }

        private static async Task ReceiveTicket(Ticket ticket)
        {
            // Receive ticket from hub
            try
            {
                DiscordTicket discordTicket = tickets.First(x => x.Id == ticket.Id);
                discordTicket.Update(ticket);
                await UpdateTicket(discordTicket);
            } 
            catch (ArgumentNullException ex)
            {
                Console.WriteLine($"Tried to receive unknown ticket. {ex}");
            }
        }

        public static async Task SimulateReceiveTicket(string ticketId, ETicketStatus newStatus)
        {
            // Receive ticket from hub
            try
            {
                DiscordTicket discordTicket = tickets.First(x => x.Id == ticketId);
                discordTicket.Status = newStatus;
                discordTicket.LastUpdatedAt = DateTimeOffset.Now;
                await UpdateTicket(discordTicket);
            }
            catch (ArgumentNullException ex)
            {
                Console.WriteLine($"Tried to receive unknown ticket. {ex}");
            }
        }

        private static MessageComponent GetTicketStatusComponent(ETicketStatus status)
        {
            switch (status)
            {
                case ETicketStatus.Open:
                    return new ComponentBuilder()
                        .WithButton("Status: Open", customId: "ticket-status", style: ButtonStyle.Secondary, disabled: true)
                        .Build();
                case ETicketStatus.In_Progress:
                    return new ComponentBuilder()
                        .WithButton("Status: In Progress", customId: "ticket-status", style: ButtonStyle.Primary, disabled: true)
                        .Build();
                case ETicketStatus.Done:
                    return new ComponentBuilder()
                        .WithButton("Status: Done", customId: "ticket-status", style: ButtonStyle.Success, disabled: true)
                        .Build();
                case ETicketStatus.Declined:
                    return new ComponentBuilder()
                        .WithButton("Status: Declined", customId: "ticket-status", style: ButtonStyle.Danger, disabled: true)
                        .Build();             
            }
            return new ComponentBuilder()
                .WithButton("Status: Unknown", customId: "ticket-status", style: ButtonStyle.Secondary, disabled: true)
                .Build();
        }

        private static Embed GetTicketEmbedded(DiscordTicket ticket)
        {
            string name_prefix = "Unknown";
            switch (ticket.Type)
            {
                case ETicketType.Bug:
                    name_prefix = "Bug";
                    break;
                case ETicketType.Request:
                    name_prefix = "Request";
                    break;
            }

            return new EmbedBuilder()
                .WithAuthor(client.CurrentUser.ToString(), client.CurrentUser.GetAvatarUrl() ?? client.CurrentUser.GetDefaultAvatarUrl())
                .WithTitle($"[{name_prefix}] {ticket.Title}")
                .WithDescription(
                $"{ticket.Description}\n\n" +
                $"**Created At: <t:{ticket.CreatedAt.ToUnixTimeSeconds()}:R>**\n" +
                $"**Last Updated At: <t:{ticket.LastUpdatedAt.ToUnixTimeSeconds()}:R>**")
                .WithColor(Color.Green)
                .WithCurrentTimestamp()
                .Build();
        }

        private static SocketTextChannel? GetSupportChannel(SocketGuild guild)
        {
            ulong channelId;

            if (!supportChannels.TryGetValue(guild.Id, out channelId))
            {
                return null;
            }

            return guild.GetTextChannel(channelId);
        }

        public static async Task RegisterSupportChannel(SocketSlashCommand command)
        {
            if (command.GuildId != null && command.ChannelId != null)
            {
                supportChannels.Add((ulong)command.GuildId, (ulong)command.ChannelId);
            }
            await command.RespondAsync("Successfully initiated support channel.");
        }

        private static async Task UpdateTicket(DiscordTicket ticket)
        {
            var guild = client.GetGuild(ticket.GuildId);
            var channel = GetSupportChannel(guild);
            if (channel == null)
            {
                return;
            }

            if (ticket.MessageId != null)
            {
                // Check what happens if message dont exist
                await channel.ModifyMessageAsync((ulong)ticket.MessageId, x =>
                {
                    x.Embed = GetTicketEmbedded(ticket);
                    x.Components = GetTicketStatusComponent(ticket.Status);
                });
            } 
            else
            {
                var newTicketMessage = await channel.SendMessageAsync(embed: GetTicketEmbedded(ticket), components: GetTicketStatusComponent(ticket.Status));
                ticket.MessageId = newTicketMessage.Id;
            }
        }
    }
}
