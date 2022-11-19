using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.SignalR.Client;
using NLog;
using Support.Discord.Models;
using Support.Shared;

namespace Support.Discord.Services
{
    internal static class SupportService
    {
        private static readonly DiscordSocketClient client = Program.client;
        private static readonly List<DiscordTicket> tickets = new List<DiscordTicket>();
        private static readonly Dictionary<ulong, ulong> supportChannels = new();
        private static HubConnection hubConnection;
        private static readonly Session session = new() { GroupName = SessionGroups.Listener, Name = "Discord" };
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static async Task ConnectHub()
        {
            if (hubConnection == null)
            {
                hubConnection = new HubConnectionBuilder()
                    .WithUrl("https://localhost:7290/Support")
                    .Build();

                hubConnection.On<Ticket>(ServerBroadcasts.SendTicketUpdate, async ticket =>
                {
                    try
                    {
                        await ReceiveTicket(ticket);
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Failed to receive ticket");
                        logger.Error(ex);
                    }
                });

                await hubConnection.StartAsync();

                await hubConnection.SendAsync(
                    ServerBroadcasts.SessionConnected,
                    session
                );
            }
        }

        public static Dictionary<string, string> GetTicketCustomFieldsFromComponent(List<SocketMessageComponentData> components, ETicketType type)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            switch (type)
            {
                case ETicketType.Request:
                    string description = components
                        .First(x => x.CustomId == "description").Value;
                    dictionary.Add("Description", description);
                    break;
                case ETicketType.Bug:
                    string environment = components
                        .First(x => x.CustomId == "environment").Value;
                    string steps = components
                        .First(x => x.CustomId == "steps").Value;
                    string currentBehaviour = components
                        .First(x => x.CustomId == "currentBehaviour").Value;
                    string expectedBehaviour = components
                        .First(x => x.CustomId == "expectedBehaviour").Value;
                    dictionary.Add("Environment", environment);
                    dictionary.Add("Steps to reproduce", steps);
                    dictionary.Add("Current Behaviour", currentBehaviour);
                    dictionary.Add("Expected Behaviour", expectedBehaviour);
                    break;
            }
            return dictionary;
        }

        public static async Task CreateTicket(SocketModal modal, ETicketType type)
        {
            List<SocketMessageComponentData> components =
                modal.Data.Components.ToList();

            string name = components
                .First(x => x.CustomId == "name").Value;
            Dictionary<string, string> customFields = GetTicketCustomFieldsFromComponent(components, type);

            ulong guildId = modal.GuildId ?? 0;

            DiscordTicket ticket = new DiscordTicket(
                Type: type, Status: ETicketStatus.Open, Priority: ETicketPriority.Unknown,
                Title: name, CustomFields: customFields, Author: modal.User.ToString(),
                CreatedAt: DateTimeOffset.Now, DateTimeOffset.Now, guildId);

            tickets.Add(ticket);
            await TransmitTicket(ticket);
            await modal.RespondAsync($"Successfully submitted your ticket. ({ticket.Id})", ephemeral: true);
        }

        private static async Task TransmitTicket(DiscordTicket discordTicket)
        {
            Ticket ticket = discordTicket.Downgrade();
            // Send ticket to hub
            await hubConnection.SendAsync(
                ServerBroadcasts.SendTicketUpdate,
                session,
                ticket
            );
        }

        private static async Task ReceiveTicket(Ticket ticket)
        {
            // Receive ticket from hub
            try
            {
                logger.Info($"Ticket ID: {ticket.Id}");
                DiscordTicket discordTicket = tickets.First(x => x.Id == ticket.Id);
                discordTicket.Update(ticket);
                await UpdateTicket(discordTicket);
            }
            catch (ArgumentNullException ex)
            {
                logger.Error($"Tried to receive unknown ticket. {ex}");
            }
        }

        public static async Task UpdateTicket(string ticketId, ETicketStatus newStatus, ETicketPriority newPriority)
        {
            // Receive ticket from hub
            try
            {
                DiscordTicket discordTicket = tickets.First(x => x.Id == ticketId);
                discordTicket.Status = newStatus;
                discordTicket.Priority = newPriority;
                discordTicket.LastUpdatedAt = DateTimeOffset.Now;
                await TransmitTicket(discordTicket);
            }
            catch (ArgumentNullException ex)
            {
                logger.Warn($"Tried to receive unknown ticket. {ex}");
            }
        }

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

        private static MessageComponent GetTicketComponents(DiscordTicket ticket)
        {
            var builder = new ComponentBuilder();
            AttachTicketStatusComponent(builder, ticket.Status);
            AttachTicketPriorityComponent(builder, ticket.Priority);
            return builder.Build();
        }

        public static void AttachTicketCustomFields(EmbedBuilder builder, DiscordTicket ticket)
        {
            foreach (KeyValuePair<string, string> entry in ticket.CustomFields)
            {
                builder.AddField(entry.Key, entry.Value);
            }
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

            var builder = new EmbedBuilder();
            builder
                .WithAuthor(client.CurrentUser.ToString(), client.CurrentUser.GetAvatarUrl() ?? client.CurrentUser.GetDefaultAvatarUrl())
                .WithTitle($"[{name_prefix}] {ticket.Title}")
                .WithDescription(
                $"**Reporter:** {ticket.Author}\n" +
                $"**Created At:** <t:{ticket.CreatedAt.ToUnixTimeSeconds()}:R>\n" +
                $"**Last Updated At:** <t:{ticket.LastUpdatedAt.ToUnixTimeSeconds()}:R>")
                .WithColor(Color.Green)
                .WithCurrentTimestamp()
                .WithFooter($"Ticket Id: {ticket.Id}");
            AttachTicketCustomFields(builder, ticket);
            return builder.Build();
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
            SocketTextChannel channel = command.Data.Options.First(x => x.Name == "channel").Value as SocketTextChannel;
            if (command.GuildId != null && command.ChannelId != null)
            {
                supportChannels.Add((ulong)command.GuildId, channel.Id);
            }
            await command.RespondAsync($"Successfully initiated support channel in {channel.Mention}.");
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
                    x.Components = GetTicketComponents(ticket);
                });
            }
            else
            {
                var newTicketMessage = await channel.SendMessageAsync(
                    embed: GetTicketEmbedded(ticket),
                    components: GetTicketComponents(ticket));
                ticket.MessageId = newTicketMessage.Id;
            }
        }
    }
}
