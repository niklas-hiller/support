using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.SignalR.Client;
using NLog;
using Support.Discord.Models;
using Support.Shared;
using Support.Shared.Enums;

namespace Support.Discord.Services
{
    internal static class SupportService
    {
        private static readonly DiscordSocketClient client = Program.client;
        private static readonly List<DiscordProject> projects = new List<DiscordProject>();
        private static readonly List<DiscordTicket> tickets = new List<DiscordTicket>();
        private static readonly Dictionary<string, ulong> projectCreateQueue = new Dictionary<string, ulong>();
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

                hubConnection.On<string, Project>(ServerBroadcasts.SendProject, async (requestId, project) =>
                {
                    try
                    {
                        await ReceiveProject(requestId, project);
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Failed to receive project");
                        logger.Error(ex);
                    }
                });

                hubConnection.On<Ticket>(ServerBroadcasts.SendTicketCreate, async ticket =>
                {
                    try
                    {
                        await ReceiveTicketCreate(ticket);
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

        private static async Task TransmitProjectCreate(ProjectCreateRequest projectCreateRequest)
        {
            logger.Info($"Sending Project ({projectCreateRequest.Name}) create to server");
            await hubConnection.SendAsync(
                ServerBroadcasts.CreateNewProject,
                session,
                projectCreateRequest
            );
        }

        public static async Task CreateProject(SocketSlashCommand command)
        {
            var projectName = HelperService.GetDataObjectFromSlashCommand(command, "project-name").ToString();

            ulong guildId = command.GuildId ?? 0;

            ProjectCreateRequest request = new ProjectCreateRequest(
                name: projectName);
            projectCreateQueue.Add(request.RequestId, guildId);

            await TransmitProjectCreate(request);
            await command.RespondAsync($"Successfully submitted your project.", ephemeral: true);
        }

        private static async Task ReceiveProject(string requestId, Project project)
        {
            // Receive project from hub
            logger.Info($"Received Project ({project.Id}) from server");
            if (!projectCreateQueue.ContainsKey(requestId))
            {
                logger.Info($"Couldn't identify Project ({project.Id}) from server, discarded ticket.");
                return;
            }
            DiscordProject discordProject = new DiscordProject(project, projectCreateQueue[requestId]);
            projectCreateQueue.Remove(requestId);
            projects.Add(discordProject);
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

        private static async Task ReceiveTicketCreate(Ticket ticket)
        {
            // Receive ticket from hub
            logger.Info($"Received Ticket ({ticket.Id}) from server");
            DiscordProject? project = GetProjectById(ticket.ProjectId);
            if (project == null)
            {
                logger.Info($"Couldn't identify Ticket ({ticket.Id}) from server, discarded ticket.");
                return;
            }
            DiscordTicket discordTicket = new DiscordTicket(ticket);
            project.Tickets.Add(discordTicket);
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
            DiscordProject project = GetProjectFromGuild(guildId);

            TicketCreateRequest request = new TicketCreateRequest(
                ProjectId: project.Id, Type: type, Status: ETicketStatus.Open, Priority: ETicketPriority.Unknown,
                Title: name, CustomFields: customFields, Author: modal.User.ToString());

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

        private static DiscordProject? GetProjectFromGuild(ulong guildId)
        {
            try
            {
                return projects.First(x => x.GuildId == guildId);
            }
            catch (InvalidOperationException e)
            {
                logger.Warn($"Couldn't find a project with the guild id {guildId}");
                return null;
            }
        }

        private static DiscordProject GetProjectById(string projectId)
        {
            return projects.First(x => x.Id == projectId);
        }

        public static SocketGuild GetGuildByProjectId(string projectId)
        {
            DiscordProject project = GetProjectById(projectId);
            return client.GetGuild(project.GuildId);
        }

        public static bool HasProject(ulong guildId)
        {
            return GetProjectFromGuild(guildId) != null;
        }

        private static SocketTextChannel? GetSupportChannel(SocketGuild guild)
        {
            ulong? channelId = GetProjectFromGuild(guild.Id).ChannelId;
            if (channelId == null) return null;

            return guild.GetTextChannel((ulong)channelId);
        }

        public static bool HasSupportChannel(ulong guildId)
        {
            return GetProjectFromGuild(guildId).ChannelId != null;
        }

        public static async Task RegisterSupportChannel(SocketSlashCommand command)
        {
            SocketTextChannel channel = HelperService.GetDataObjectFromSlashCommand(command, "channel") as SocketTextChannel;
            if (command.GuildId != null && command.ChannelId != null)
            {
                ulong guildId = (ulong)command.GuildId;
                if (!HasSupportChannel(guildId))
                {
                    DiscordProject project = GetProjectFromGuild(guildId);
                    project.ChannelId = command.ChannelId;
                    await command.RespondAsync($"Successfully initiated support channel in {channel.Mention}.", ephemeral: true);
                    return;
                }
            }
            await command.RespondAsync($"Failed to initiate support channel in {channel.Mention}.", ephemeral: true);
        }

        private static Dictionary<string, string> GetTicketCustomFieldsFromComponent(List<SocketMessageComponentData> components, ETicketType type)
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

        private static async Task InformWatchers(DiscordTicket ticket)
        {
            var guild = GetGuildByProjectId(ticket.ProjectId);
            foreach (ulong watcherId in ticket.Watchers)
            {
                var user = guild.Users.First(x => x.Id == watcherId);
                if (user == null) return;
                var dmChannel = await user.CreateDMChannelAsync();
                await dmChannel.SendMessageAsync(
                    embed: EmbedService.GetWatcherEmbedded(ticket));
            }
        }

        private static async Task UpdateTicket(DiscordTicket ticket)
        {
            var guild = GetGuildByProjectId(ticket.ProjectId);
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
                    x.Embed = EmbedService.GetTicketEmbedded(ticket);
                    x.Components = ComponentService.GetTicketComponents(ticket);
                });
            }
            else
            {
                var newTicketMessage = await channel.SendMessageAsync(
                    embed: EmbedService.GetTicketEmbedded(ticket),
                    components: ComponentService.GetTicketComponents(ticket));
                ticket.MessageId = newTicketMessage.Id;
                await newTicketMessage.ReplyAsync(
                    "You can select anytime if you want to watch or unwatch the ticket. If you watch a ticket, you will be informed via DM when there's an update regarding the ticket.\n" +
                    $"Ticket ID: {ticket.Id}",
                    components: ComponentService.GetTicketMenuComponent(ticket));
            }

            await InformWatchers(ticket);
        }
    }
}
