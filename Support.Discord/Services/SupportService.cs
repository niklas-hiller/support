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
        private static readonly Dictionary<string, ulong> ticketCreateQueue = new Dictionary<string, ulong>();
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

                hubConnection.On<string, Ticket>(ServerBroadcasts.SendTicketCreate, async (requestId, ticket) =>
                {
                    try
                    {
                        await ReceiveTicketCreate(requestId, ticket);
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Failed to receive ticket");
                        logger.Error(ex);
                    }
                });

                hubConnection.On<Ticket>(ServerBroadcasts.SendTicketUpdate, async ticket =>
                {
                    try
                    {
                        await ReceiveTicketUpdate(ticket);
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Failed to receive ticket");
                        logger.Error(ex);
                    }
                });

                hubConnection.On<ServerError>(ServerBroadcasts.SendServerError, error =>
                {
                    logger.Error($"The server raised a exception on one of the requests from this application: {error.Message}");
                });

                await hubConnection.StartAsync();

                await hubConnection.SendAsync(
                    ServerBroadcasts.SessionConnected,
                    session
                );
            }
        }

        public static DiscordTicket? GetTicketById(string ticketId)
        {
            try
            {
                return tickets.First(x => x.Id == ticketId);
            }
            catch (InvalidOperationException e)
            {
                logger.Warn($"Couldn't find a ticket with the id {ticketId}");
                return null;
            }
        }

        private static async Task TransmitTicketCreate(TicketCreateRequest ticketCreateRequest)
        {
            logger.Info($"Sending Ticket ({ticketCreateRequest.Title}) create to server");
            await hubConnection.SendAsync(
                ServerBroadcasts.SendTicketCreate,
                session,
                ticketCreateRequest
            );
        }

        private static async Task TransmitTicketUpdate(TicketUpdateRequest ticketUpdateRequest)
        {
            logger.Info($"Sending Ticket ({ticketUpdateRequest.Id}) update to server");
            await hubConnection.SendAsync(
                ServerBroadcasts.SendTicketUpdate,
                session,
                ticketUpdateRequest
            );
        }

        private static async Task ReceiveTicketCreate(string requestId, Ticket ticket)
        {
            // Receive ticket from hub
            logger.Info($"Received Ticket ({ticket.Id}) from server");
            if (!ticketCreateQueue.ContainsKey(requestId))
            {
                logger.Info($"Couldn't identify Ticket ({ticket.Id}) from server, discarded ticket.");
                return;
            }
            DiscordTicket discordTicket = new DiscordTicket(ticket, ticketCreateQueue[requestId]);
            ticketCreateQueue.Remove(requestId);
            tickets.Add(discordTicket);
            await UpdateTicket(discordTicket);
        }

        private static async Task ReceiveTicketUpdate(Ticket ticket)
        {
            // Receive ticket from hub
            try
            {
                logger.Info($"Received Ticket ({ticket.Id}) from server");
                DiscordTicket discordTicket = GetTicketById(ticket.Id);
                discordTicket.Update(ticket);
                await UpdateTicket(discordTicket);
            }
            catch (ArgumentNullException ex)
            {
                logger.Error($"Tried to receive unknown ticket. {ex}");
            }
        }

        public static async Task CreateTicket(SocketModal modal, ETicketType type)
        {
            List<SocketMessageComponentData> components =
                modal.Data.Components.ToList();

            string name = components
                .First(x => x.CustomId == "name").Value;
            Dictionary<string, string> customFields = GetTicketCustomFieldsFromComponent(components, type);

            ulong guildId = modal.GuildId ?? 0;

            TicketCreateRequest request = new TicketCreateRequest(
                Type: type, Status: ETicketStatus.Open, Priority: ETicketPriority.Unknown,
                Title: name, CustomFields: customFields, Author: modal.User.ToString());
            ticketCreateQueue.Add(request.RequestId, guildId);

            await TransmitTicketCreate(request);
            await modal.RespondAsync($"Successfully submitted your ticket.", ephemeral: true);
        }

        public static async Task InitiateTransmitUpdateTicket(string ticketId, ETicketStatus newStatus, ETicketPriority newPriority)
        {
            if (GetTicketById(ticketId) == null) throw new KeyNotFoundException();

            TicketUpdateRequest request = new TicketUpdateRequest(
                id: ticketId, status: newStatus, priority: newPriority);

            await TransmitTicketUpdate(request);
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

        public static bool HasSupportChannel(ulong guildId)
        {
            return supportChannels.ContainsKey(guildId);
        }

        public static async Task RegisterSupportChannel(SocketSlashCommand command)
        {
            SocketTextChannel channel = HelperService.GetDataObjectFromSlashCommand(command, "channel") as SocketTextChannel;
            if (command.GuildId != null && command.ChannelId != null)
            {
                supportChannels.Add((ulong)command.GuildId, channel.Id);
            }
            await command.RespondAsync($"Successfully initiated support channel in {channel.Mention}.");
        }

        public static bool SetWatchTicket(string ticketId, ulong userId, bool isWatching)
        {
            DiscordTicket? ticket = GetTicketById(ticketId);
            if (ticket == null) throw new KeyNotFoundException();
            if (isWatching)
            {
                if (ticket.Watchers.Contains(userId)) return false;
                ticket.Watchers.Add(userId);
            }
            else
            {
                if (!ticket.Watchers.Contains(userId)) return false;
                ticket.Watchers.Remove(userId);
            }
            return true;
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

        public static void AttachTicketCustomFields(EmbedBuilder builder, DiscordTicket ticket)
        {
            foreach (KeyValuePair<string, string> entry in ticket.CustomFields)
            {
                builder.AddField(entry.Key, entry.Value);
            }
        }

        private static MessageComponent GetTicketComponents(DiscordTicket ticket)
        {
            var builder = new ComponentBuilder();
            AttachTicketStatusComponent(builder, ticket.Status);
            AttachTicketPriorityComponent(builder, ticket.Priority);
            return builder.Build();
        }

        private static MessageComponent GetTicketMenuComponent(DiscordTicket ticket)
        {
            var builder = new ComponentBuilder();
            AttachTicketWatchComponent(builder, ticket.Id);
            return builder.Build();
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

        private static Embed GetWatcherEmbedded(DiscordTicket ticket)
        {
            var guild = client.GetGuild(ticket.GuildId);
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
                .WithTitle($"[Notification] A watched ticket was updated")
                .WithDescription(
                $"There was an update in **{guild.Name}** for the ticket **[{name_prefix}] {ticket.Title}**.\n\n" +
                "**If you no longer want to be informed about the ticket, please select unwatch on the ticket.**\n")
                .WithColor(Color.Green)
                .WithCurrentTimestamp()
                .WithFooter($"Type '/force-unwatch {ticket.Id}' to force a ticket unwatch. Keep in mind that the select menu might display a wrong value then!");
            return builder.Build();
        }

        private static async Task InformWatchers(DiscordTicket ticket)
        {
            var guild = client.GetGuild(ticket.GuildId);
            foreach (ulong watcherId in ticket.Watchers)
            {
                var user = guild.Users.First(x => x.Id == watcherId);
                if (user == null) return;
                var dmChannel = await user.CreateDMChannelAsync();
                await dmChannel.SendMessageAsync(
                    embed: GetWatcherEmbedded(ticket));
            }
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
                await newTicketMessage.ReplyAsync(
                    "You can select anytime if you want to watch or unwatch the ticket. If you watch a ticket, you will be informed via DM when there's an update regarding the ticket.\n" +
                    $"Ticket ID: {ticket.Id}",
                    components: GetTicketMenuComponent(ticket));
            }

            await InformWatchers(ticket);
        }
    }
}
