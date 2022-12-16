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
        private static readonly Dictionary<string, SocketSlashCommand> projectCreateQueue = new Dictionary<string, SocketSlashCommand>();
        private static readonly Dictionary<string, SocketSlashCommand> projectDeleteQueue = new Dictionary<string, SocketSlashCommand>();
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

                hubConnection.On<Ticket>(ServerBroadcasts.SendTicket, async ticket =>
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

        private static async Task TransmitProject(ProjectCreateRequest projectCreateRequest)
        {
            logger.Info($"Sending Project ({projectCreateRequest.Name}) create to server");
            await hubConnection.SendAsync(
                ServerBroadcasts.CreateProject,
                session,
                projectCreateRequest
            );
        }

        private static async Task TransmitProject(ProjectDeleteRequest projectDeleteRequest)
        {
            logger.Info($"Sending Project ({projectDeleteRequest.ProjectId}) delete to server");
            await hubConnection.SendAsync(
                ServerBroadcasts.DeleteProject,
                session,
                projectDeleteRequest
            );
        }

        private static async Task ReceiveProject(string requestId, Project project)
        {
            // Receive project from hub
            logger.Info($"Received Project ({project.Id}) from server");

            if (projectCreateQueue.ContainsKey(requestId))
            {
                logger.Info("Identified received project as 'Create Project' request");

                var correspondingCommand = projectCreateQueue[requestId];
                projectCreateQueue.Remove(requestId);

                DiscordProject discordProject = new DiscordProject(project, correspondingCommand.User.Id);

                var isSync = (bool)HelperService.GetDataObjectFromSlashCommand(correspondingCommand, "sync");
                if (isSync)
                {
                    SynchronizeProject(discordProject, (ulong)correspondingCommand.GuildId, (ulong)correspondingCommand.ChannelId);
                }

                projects.Add(discordProject);
                await correspondingCommand.RespondAsync($"Successfully created your project '{project.Name}'. ({project.Id})", ephemeral: true);
            }
            else if (projectDeleteQueue.ContainsKey(requestId))
            {
                logger.Info("Identified received project as 'Delete Project' request");

                var correspondingCommand = projectDeleteQueue[requestId];
                projectDeleteQueue.Remove(requestId);

                DiscordProject discordProject = GetProjectById(project.Id);

                UnsynchronizeProject(discordProject);

                projects.Remove(discordProject);
                await correspondingCommand.RespondAsync($"Successfully deleted your project '{project.Name}'. ({project.Id})", ephemeral: true);
            }
            else
            {
                logger.Info($"Couldn't identify Project ({project.Id}) from server, discarded ticket.");
                return;
            }


        }

        private static async Task TransmitTicket(TicketCreateRequest ticketCreateRequest)
        {
            logger.Info($"Sending Ticket ({ticketCreateRequest.Title}) create to server");
            await hubConnection.SendAsync(
                ServerBroadcasts.TicketCreate,
                session,
                ticketCreateRequest
            );
        }

        private static async Task TransmitTicket(TicketUpdateRequest ticketUpdateRequest)
        {
            logger.Info($"Sending Ticket ({ticketUpdateRequest.Id}) update to server");
            await hubConnection.SendAsync(
                ServerBroadcasts.TicketUpdate,
                session,
                ticketUpdateRequest
            );
        }

        private static async Task ReceiveTicket(Ticket ticket)
        {
            // Receive ticket from hub
            logger.Info($"Received Ticket ({ticket.Id}) from server");
            DiscordProject? project = GetProjectById(ticket.ProjectId);
            if (project == null)
            {
                logger.Info($"Couldn't identify Ticket ({ticket.Id}) from server, discarded ticket.");
                return;
            }

            // Check if ticket already exists on discord client
            DiscordTicket discordTicket;
            if (project.Tickets.FirstOrDefault(x => x.Id == ticket.Id, null) == null)
            {
                discordTicket = new DiscordTicket(ticket);
                project.Tickets.Add(discordTicket);
                tickets.Add(discordTicket);
            }
            else
            {
                discordTicket = GetTicketById(ticket.Id);
                discordTicket.Update(ticket);
            }

            // Update ticket message
            await UpdateTicket(discordTicket);
        }

        public static async Task CreateTicket(SocketModal modal, string? projectId, ETicketType type)
        {
            if (projectId == null)
            {
                await modal.RespondAsync($"Failed to submit your ticket. (Missing project id)", ephemeral: true);
                return;
            }

            List<SocketMessageComponentData> components =
                modal.Data.Components.ToList();

            string name = components
                .First(x => x.CustomId == "name").Value;
            Dictionary<string, string> customFields = GetTicketCustomFieldsFromComponent(components, type);

            DiscordProject project = GetProjectById(projectId);

            TicketCreateRequest request = new TicketCreateRequest(
                ProjectId: project.Id, Type: type, Status: ETicketStatus.Open, Priority: ETicketPriority.Unknown,
                Title: name, CustomFields: customFields, Author: modal.User.ToString());

            await TransmitTicket(request);
            await modal.RespondAsync($"Successfully submitted your ticket.", ephemeral: true);
        }

        private static async Task InitiateTransmitUpdateTicket(string ticketId, ETicketStatus newStatus, ETicketPriority newPriority)
        {
            if (GetTicketById(ticketId) == null) throw new KeyNotFoundException();

            TicketUpdateRequest request = new TicketUpdateRequest(
                id: ticketId, status: newStatus, priority: newPriority);

            await TransmitTicket(request);
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
            return client.GetGuild((ulong)project.GuildId);
        }

        public static bool HasProject(ulong guildId)
        {
            return GetProjectFromGuild(guildId) != null;
        }

        private static bool SynchronizeProject(DiscordProject project, ulong guildId, ulong channelId)
        {
            if (project.GuildId == null && project.ChannelId == null)
            {
                project.GuildId = guildId;
                project.ChannelId = channelId;

                project.Tickets.ForEach(async ticket =>
                {
                    await UpdateTicket(ticket);
                });

                return true;
            }
            return false;
        }

        private static bool UnsynchronizeProject(DiscordProject project)
        {
            if (project.GuildId != null && project.ChannelId != null)
            {
                var guild = client.GetGuild((ulong)project.GuildId);
                var channel = guild.GetTextChannel((ulong)project.ChannelId);

                if (guild == null || channel == null)
                {
                    logger.Warn($"Project {project.Id} has a broken sync!");
                    return false;
                }

                project.Tickets.ForEach(async ticket =>
                {
                    if (ticket.MessageId != null)
                    {
                        await channel.DeleteMessageAsync((ulong)ticket.MessageId);
                    }
                    if (ticket.WatchMessageId != null)
                    {
                        await channel.DeleteMessageAsync((ulong)ticket.WatchMessageId);
                    }
                    ticket.MessageId = null;
                    ticket.WatchMessageId = null;
                    ticket.Watchers = new List<ulong>();
                });
                project.GuildId = null;
                project.ChannelId = null;

                return true;
            }
            return false;
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
            DiscordProject project = GetProjectById(ticket.ProjectId);
            if (project.GuildId == null || project.ChannelId == null)
            {
                logger.Warn($"Project {project.Id} is currently not synched!");
                return;
            }

            var guild = client.GetGuild((ulong)project.GuildId);
            var channel = guild.GetTextChannel((ulong)project.ChannelId);


            if (guild == null || channel == null)
            {
                logger.Warn($"Project {project.Id} has a broken sync!");
                return;
            }

            if (ticket.MessageId != null)
            {
                // Check what happens if message dont exist
                logger.Info($"Ticket {ticket.Id} component found, updating...");
                await channel.ModifyMessageAsync((ulong)ticket.MessageId, x =>
                {
                    x.Embed = EmbedService.GetTicketEmbedded(ticket);
                    x.Components = ComponentService.GetTicketComponents(ticket);
                });
            }
            else
            {
                logger.Info($"Ticket {ticket.Id} component not found, creating new one...");

                var newTicketMessage = await channel.SendMessageAsync(
                    embed: EmbedService.GetTicketEmbedded(ticket),
                    components: ComponentService.GetTicketComponents(ticket));
                ticket.MessageId = newTicketMessage.Id;

                var newWatchMessage = await newTicketMessage.ReplyAsync(
                    "You can select anytime if you want to watch or unwatch the ticket. If you watch a ticket, you will be informed via DM when there's an update regarding the ticket.\n" +
                    $"Ticket ID: {ticket.Id}",
                    components: ComponentService.GetTicketMenuComponent(ticket));
                ticket.WatchMessageId = newWatchMessage.Id;
            }

            await InformWatchers(ticket);
        }

        public static async Task CreateProjectCommand(SocketSlashCommand command)
        {
            var projectName = HelperService.GetDataObjectFromSlashCommand(command, "project-name").ToString();

            ProjectCreateRequest request = new ProjectCreateRequest(projectName);
            projectCreateQueue.Add(request.RequestId, command);

            await TransmitProject(request);
        }

        public static async Task DeleteProjectCommand(SocketSlashCommand command)
        {
            var projectId = HelperService.GetDataObjectFromSlashCommand(command, "project-id").ToString();

            ProjectDeleteRequest request = new ProjectDeleteRequest(projectId);
            projectDeleteQueue.Add(request.RequestId, command);

            await TransmitProject(request);
        }

        public static async Task CreateTicketCommand(SocketSlashCommand command)
        {
            var projectId = HelperService.GetDataObjectFromSlashCommand(command, "project-id").ToString();
            var modal = new ModalBuilder();
            ETicketType type = TicketType.FromString(HelperService.GetDataObjectFromSlashCommand(command, "type").ToString());
            switch (type)
            {
                case ETicketType.Bug:
                    modal
                        .WithTitle("Create Ticket (Bug)")
                        .WithCustomId($"bug-modal${projectId}")
                        .AddTextInput("Name", "name", placeholder: "Please enter a short meaningful name for the ticket.", required: true)
                        .AddTextInput("Environment", "environment", placeholder: "The environment information during the bug occurence, i.e. Software Version.", style: TextInputStyle.Paragraph, required: true)
                        .AddTextInput("Steps to reproduce", "steps", placeholder: "Steps to reproduce written down as bullet points.", style: TextInputStyle.Paragraph, required: true)
                        .AddTextInput("Current Behaviour", "currentBehaviour", placeholder: "The behaviour that currently occurs.", style: TextInputStyle.Paragraph, required: true)
                        .AddTextInput("Expected Behaviour", "expectedBehaviour", placeholder: "The behaviour you would expect to occur.", style: TextInputStyle.Paragraph, required: true);
                    break;
                case ETicketType.Request:
                    modal
                        .WithTitle("Create Ticket (Request)")
                        .WithCustomId($"request-modal${projectId}")
                        .AddTextInput("Name", "name", placeholder: "Please enter a short meaningful name for the ticket.", required: true)
                        .AddTextInput("Description", "description", placeholder: "Please enter a description to the ticket.", style: TextInputStyle.Paragraph, required: true);
                    break;
            }
            await command.RespondWithModalAsync(modal.Build());
        }

        public static async Task UpdateTicketCommand(SocketSlashCommand command)
        {
            var statusStr = HelperService.GetDataObjectFromSlashCommand(command, "status").ToString() ?? "";
            var status = TicketStatus.FromString(statusStr);

            var priorityStr = HelperService.GetDataObjectFromSlashCommand(command, "priority").ToString() ?? "";
            var priority = TicketPriority.FromString(priorityStr);

            var ticketId = HelperService.GetDataObjectFromSlashCommand(command, "ticket").ToString() ?? "0";

            try
            {
                await InitiateTransmitUpdateTicket(ticketId, status, priority);
                await command.RespondAsync($"Successfully updated Ticket {ticketId}", ephemeral: true);
            }
            catch (KeyNotFoundException e)
            {
                await command.RespondAsync($"Couldn't updated Ticket {ticketId}. (Ticket Id does not exist)", ephemeral: true);
            }
        }

        public static async Task SynchronizeProjectCommand(SocketSlashCommand command)
        {
            var projectId = HelperService.GetDataObjectFromSlashCommand(command, "project-id").ToString();
            if (command.GuildId != null && command.ChannelId != null && projectId != null)
            {
                DiscordProject project = GetProjectById(projectId);
                if (SynchronizeProject(project, (ulong)command.GuildId, (ulong)command.ChannelId))
                {
                    await command.RespondAsync($"Successfully synchronized the project {projectId}.", ephemeral: true);
                    return;
                }
                else
                {
                    await command.RespondAsync($"Failed to synchronize the project (ALready active synchronization).", ephemeral: true);
                    return;
                }
            }
            await command.RespondAsync($"Failed to synchronize the project (No guildId, channelId, or projectId provided).", ephemeral: true);
        }

        public static async Task UnsynchronizeProjectCommand(SocketSlashCommand command)
        {
            var projectId = HelperService.GetDataObjectFromSlashCommand(command, "project-id").ToString();
            if (projectId != null)
            {
                DiscordProject project = GetProjectById(projectId);
                if (UnsynchronizeProject(project))
                {
                    await command.RespondAsync($"Successfully unsynchronized the project {projectId}.", ephemeral: true);
                }
                else
                {
                    await command.RespondAsync($"Failed to unsynchronize the project (No active synchronization).", ephemeral: true);
                    return;
                }
            }
            await command.RespondAsync($"Failed to unsynchronize the project (No project id provided).", ephemeral: true);
        }

        public static async Task ForceUnwatchCommand(SocketSlashCommand command)
        {
            string ticketId = HelperService.GetDataObjectFromSlashCommand(command, "ticket").ToString() ?? "0";
            try
            {
                bool success = SetWatchTicket(ticketId, command.User.Id, false);
                if (success)
                {
                    await command.RespondAsync(
                        $"Successfully force removed ticket {ticketId} from your watchlist.\n" +
                        "Please keep in mind that the select menu of that ticket will still show 'Watching'.", ephemeral: true);
                }
                else
                {
                    await command.RespondAsync(
                        $"Failed to force removed ticket {ticketId} from your watchlist.\n" +
                        "Reason: You are not watching that ticket. Are you sure you entered the right id?", ephemeral: true);
                }

            }
            catch (KeyNotFoundException e)
            {
                await command.RespondAsync(
                    $"Failed to force removed ticket {ticketId} from your watchlist.\n" +
                    "Reason: A ticket with the given Id does not exist. Maybe the ticket does not exist anymore?", ephemeral: true);
            }
        }
    }
}
