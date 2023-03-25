using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.AspNetCore.SignalR.Client;
using NLog;
using Support.Discord.Models;
using Support.Shared;
using Support.Shared.Enums;
using System.Net.Sockets;

namespace Support.Discord.Services
{
    internal static class SupportService
    {
        private static readonly DiscordSocketClient client = Program.client;
        private static readonly List<DiscordProject> projects = new List<DiscordProject>();
        private static readonly List<DiscordTicket> tickets = new List<DiscordTicket>();
        private static readonly Dictionary<string, SocketInteraction> projectCreateQueue = new Dictionary<string, SocketInteraction>();
        private static readonly Dictionary<string, SocketInteraction> projectDeleteQueue = new Dictionary<string, SocketInteraction>();
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

        #region Getter

        private static DiscordTicket? GetTicketById(string ticketId)
        {
            return tickets.FirstOrDefault(x => x.Id == ticketId, null);
        }

        public static DiscordProject? GetProjectById(string projectId)
        {
            return projects.FirstOrDefault(x => x.Id == projectId, null);
        }

        public static SocketGuild? GetGuildByProjectId(string projectId)
        {
            DiscordProject project = GetProjectById(projectId);
            return project.Synchronization != null ? client.GetGuild(project.Synchronization.GuildId) : null;
        }

        #endregion

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

                SocketInteraction? correspondingInteraction = projectCreateQueue[requestId];
                projectCreateQueue.Remove(requestId);

                DiscordProject discordProject = new DiscordProject(project);

                #region Sync Option for Slash Commands
                if (correspondingInteraction.GetType() == typeof(SocketSlashCommand))
                {
                    bool isSync = (bool)HelperService.GetDataObjectFromSlashCommand((SocketSlashCommand)correspondingInteraction, "sync");
                    if (isSync)
                    {
                        ulong guildId = correspondingInteraction.GuildId ?? 0;
                        logger.Info($"Project was requested to be synchronized to guild {guildId}");
                        await SynchronizeProject(discordProject, guildId);
                    }
                };
                #endregion

                projects.Add(discordProject);
                await correspondingInteraction.RespondAsync($"Successfully created your project '{project.Name}'. ({project.Id})", ephemeral: true);
            }
            else if (projectDeleteQueue.ContainsKey(requestId))
            {
                logger.Info("Identified received project as 'Delete Project' request");

                var correspondingInteraction = projectDeleteQueue[requestId];
                projectDeleteQueue.Remove(requestId);

                DiscordProject discordProject = GetProjectById(project.Id);

                await UnsynchronizeProject(discordProject);

                projects.Remove(discordProject);
                await correspondingInteraction.RespondAsync($"Successfully deleted your project '{project.Name}'. ({project.Id})", ephemeral: true);
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
            DiscordTicket? discordTicket = GetTicketById(ticket.Id);
            if (discordTicket == null)
            {
                discordTicket = new DiscordTicket(ticket);
                project.Tickets.Add(discordTicket);
                tickets.Add(discordTicket);
            }
            else
            {
                discordTicket.Update(ticket);
            }

            // Update ticket message
            await UpdateTicket(discordTicket);
        }

        private static async Task CreateTicket(string projectId, ETicketType ticketType, string ticketName, Dictionary<string, string> customFields, string ticketAuthor)
        {
            DiscordProject project = GetProjectById(projectId);

            TicketCreateRequest request = new TicketCreateRequest(
                ProjectId: project.Id, Type: ticketType, Status: ETicketStatus.Open, Priority: ETicketPriority.Unknown,
                Title: ticketName, CustomFields: customFields, Author: ticketAuthor);

            await TransmitTicket(request);
        }

        private static async Task InitiateTransmitUpdateTicket(string ticketId, ETicketStatus newStatus, ETicketPriority newPriority)
        {
            if (GetTicketById(ticketId) == null) throw new KeyNotFoundException();

            TicketUpdateRequest request = new TicketUpdateRequest(
                id: ticketId, status: newStatus, priority: newPriority);

            await TransmitTicket(request);
        }

        #region Project Synchronization

        private async static Task<bool> SynchronizeProject(DiscordProject project, ulong guildId)
        {
            if (project.Synchronization == null)
            {
                SocketGuild guild = client.GetGuild(guildId);
                if (guild == null)
                {
                    logger.Warn($"Project {project.Id} could not be synchronized!");
                    return false;
                }
                RestTextChannel channel = await guild.CreateTextChannelAsync($"{project.SimplifiedName()}-tickets");

                var newTicketMessage = await channel.SendMessageAsync(
                    embed: EmbedService.GetProjectEmbedded(project),
                    components: ComponentService.GetProjectComponents(project));
                await newTicketMessage.PinAsync();
                var messageId = newTicketMessage.Id;


                project.Synchronization = new(guild.Id, channel.Id, messageId);

                project.Tickets.ForEach(async ticket =>
                {
                    await UpdateTicket(ticket);
                });

                return true;
            }
            return false;
        }

        private static async Task<bool> UnsynchronizeProject(DiscordProject project)
        {
            if (project.Synchronization != null)
            {
                var guild = client.GetGuild(project.Synchronization.GuildId);
                var channel = guild.GetTextChannel(project.Synchronization.ChannelId);

                if (guild == null || channel == null)
                {
                    logger.Warn($"Project {project.Id} has a broken sync!");
                    return false;
                }
                project.Tickets.ForEach(ticket =>
                {
                    ticket.MessageId = null;
                    ticket.WatchMessageId = null;
                    ticket.Watchers = new List<ulong>();
                });
                await channel.DeleteAsync();
                project.Synchronization = null;

                return true;
            }
            return false;
        }

        #endregion

        private static Dictionary<string, string> GetTicketCustomFieldsFromComponent(List<SocketMessageComponentData> components, ETicketType type)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            switch (type)
            {
                case ETicketType.Story:
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

        #region Watch Ticket

        private static bool SetWatchTicket(string ticketId, ulong userId, bool isWatching)
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

        private static bool InformWatchers(DiscordTicket ticket)
        {
            SocketGuild? guild = GetGuildByProjectId(ticket.ProjectId);
            if (guild == null)
            {
                logger.Error($"Couldn't retrieve synchronized guild of project {ticket.ProjectId}");
                return false;
            }

            List<SocketGuildUser> users = guild.Users.Where(x => ticket.Watchers.Contains(x.Id)).ToList();
            users.ForEach(async user =>
            {
                IDMChannel dmChannel = await user.CreateDMChannelAsync();
                await dmChannel.SendMessageAsync(
                    embed: EmbedService.GetWatcherEmbedded(ticket));
            });

            return true;
        }

        #endregion

        #region Update Messages

        private static async Task UpdateProject(DiscordProject project)
        {
            if (project.Synchronization == null)
            {
                logger.Warn($"Project {project.Id} is currently not synched!");
                return;
            }

            SocketGuild guild = client.GetGuild(project.Synchronization.GuildId);
            SocketTextChannel channel = guild.GetTextChannel(project.Synchronization.ChannelId);

            if (guild == null || channel == null)
            {
                logger.Warn($"Project {project.Id} has a broken sync!");
                return;
            }

            // Check what happens if message dont exist
            logger.Info($"Project {project.Id} component found, updating...");
            await channel.ModifyMessageAsync(project.Synchronization.MessageId, x =>
            {
                x.Embed = EmbedService.GetProjectEmbedded(project);
                x.Components = ComponentService.GetProjectComponents(project);
            });
        }

        private static async Task UpdateTicket(DiscordTicket ticket)
        {
            DiscordProject project = GetProjectById(ticket.ProjectId);
            if (project.Synchronization == null)
            {
                logger.Warn($"Project {project.Id} is currently not synched!");
                return;
            }

            SocketGuild guild = client.GetGuild(project.Synchronization.GuildId);
            SocketTextChannel channel = guild.GetTextChannel(project.Synchronization.ChannelId);
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

            InformWatchers(ticket);
        }

        #endregion

        public static async Task TicketCreateStoryComponent(SocketMessageComponent button)
        {
            string projectId = button.Data.CustomId.Split("$")[1];
            logger.Info($"Initiate create story for Project {projectId}");

            var modal = new ModalBuilder()
                .WithTitle("Create Ticket (Story)")
                .WithCustomId($"story-modal${projectId}")
                .AddTextInput("Name", "name", placeholder: "Please enter a short meaningful name for the ticket.", required: true)
                .AddTextInput("Description", "description", placeholder: "Please enter a description to the ticket.", style: TextInputStyle.Paragraph, required: true);

            await button.RespondWithModalAsync(modal.Build());
        }

        public static async Task TicketCreateBugComponent(SocketMessageComponent button)
        {
            string projectId = button.Data.CustomId.Split("$")[1];
            logger.Info($"Initiate create bug for Project {projectId}");

            var modal = new ModalBuilder()
                .WithTitle("Create Ticket (Bug)")
                .WithCustomId($"bug-modal${projectId}")
                .AddTextInput("Name", "name", placeholder: "Please enter a short meaningful name for the ticket.", required: true)
                .AddTextInput("Environment", "environment", placeholder: "The environment information during the bug occurence, i.e. Software Version.", style: TextInputStyle.Paragraph, required: true)
                .AddTextInput("Steps to reproduce", "steps", placeholder: "Steps to reproduce written down as bullet points.", style: TextInputStyle.Paragraph, required: true)
                .AddTextInput("Current Behaviour", "currentBehaviour", placeholder: "The behaviour that currently occurs.", style: TextInputStyle.Paragraph, required: true)
                .AddTextInput("Expected Behaviour", "expectedBehaviour", placeholder: "The behaviour you would expect to occur.", style: TextInputStyle.Paragraph, required: true);

            await button.RespondWithModalAsync(modal.Build());
        }

        public static async Task ProjectUnsynchronizeComponent(SocketMessageComponent button)
        {
            string projectId = button.Data.CustomId.Split("$")[1];
            logger.Info($"Initiate unsychronize of Project {projectId}");

            DiscordProject? project = GetProjectById(projectId);

            if (project == null)
            {
                logger.Error($"Project ({projectId}) could not be found.");
                return;
            }

            await UnsynchronizeProject(project);
        }

        public static async Task ProjectDeleteComponent(SocketMessageComponent button)
        {
            string projectId = button.Data.CustomId.Split("$")[1];
            logger.Info($"Initiate delete of Project {projectId}");

            ProjectDeleteRequest request = new ProjectDeleteRequest(projectId, new(button.User.Id.ToString()));
            projectDeleteQueue.Add(request.Context.Id, button);

            await TransmitProject(request);
        }

        public static async Task WatchComponent(SocketMessageComponent menu)
        {
            bool isWatching = string.Join(", ", menu.Data.Values) == "watch" ? true : false;
            string ticketId = menu.Data.CustomId.Split("$")[1];

            SetWatchTicket(ticketId, menu.User.Id, isWatching);
            if (isWatching)
            {
                await menu.RespondAsync($"You will now be informed if there's any update regarding the ticket {ticketId}", ephemeral: true);
            }
            else
            {
                await menu.RespondAsync($"You will no longer be informed if there's any update regarding the ticket {ticketId}", ephemeral: true);
            }
        }

        public static async Task CreateTicketModal(SocketModal modal)
        {
            string[] data = modal.Data.CustomId.Split('$');
            string? projectId = data.Length > 1 ? data[1] : null;
            ETicketType ticketType = ETicketType.Unknown;

            switch (data[0])
            {
                case "bug-modal":
                    ticketType = ETicketType.Bug;
                    break;
                case "story-modal":
                    ticketType = ETicketType.Story;
                    break;
            }

            if (projectId == null)
            {
                await modal.RespondAsync($"Failed to submit your ticket. (Missing project id)", ephemeral: true);
                return;
            }

            List<SocketMessageComponentData> components =
                modal.Data.Components.ToList();

            string ticketName = components.First(x => x.CustomId == "name").Value;
            Dictionary<string, string> customFields = GetTicketCustomFieldsFromComponent(components, ticketType);
            string ticketAuthor = modal.User.ToString();

            await CreateTicket(projectId, ticketType, ticketName, customFields, ticketAuthor);

            await modal.RespondAsync($"Successfully submitted your ticket.", ephemeral: true);
        }

        public static async Task CreateProjectCommand(SocketSlashCommand command)
        {
            string? projectName = HelperService.GetDataObjectFromSlashCommand(command, "project-name").ToString();

            if (projectName == null)
            {
                logger.Error("Project Create Command did not provide any project name");
                return;
            }

            ProjectCreateRequest request = new ProjectCreateRequest(projectName, new(command.User.Id.ToString()));
            projectCreateQueue.Add(request.Context.Id, command);

            await TransmitProject(request);
        }

        public static async Task DeleteProjectCommand(SocketSlashCommand command)
        {
            string? projectId = HelperService.GetDataObjectFromSlashCommand(command, "project-id").ToString();

            if (projectId == null)
            {
                logger.Error("Project Delete Command did not provide any project id");
                return;
            }
            if (GetProjectById(projectId) == null)
            {
                logger.Error($"Project ({projectId}) could not be found.");
                await command.RespondAsync($"Couldn't find the requested project.", ephemeral: true);
                return;
            }

            ProjectDeleteRequest request = new ProjectDeleteRequest(projectId, new(command.User.Id.ToString()));
            projectDeleteQueue.Add(request.Context.Id, command);

            await TransmitProject(request);
        }

        public static async Task CreateTicketCommand(SocketSlashCommand command)
        {
            var projectId = HelperService.GetDataObjectFromSlashCommand(command, "project-id").ToString();
            if (projectId == null)
            {
                logger.Error("Ticket Create Command did not provide any project id");
                return;
            }
            if (GetProjectById(projectId) == null)
            {
                logger.Error($"Project ({projectId}) could not be found.");
                await command.RespondAsync($"Couldn't find the requested project.", ephemeral: true);
                return;
            }
            var modal = new ModalBuilder();
            ETicketType type = TicketType.FromString(HelperService.GetDataObjectFromSlashCommand(command, "type").ToString() ?? "");
            switch (type)
            {
                case ETicketType.Unknown:
                    logger.Error("Received unkown ticket type request");
                    return;
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
                case ETicketType.Story:
                    modal
                        .WithTitle("Create Ticket (Story)")
                        .WithCustomId($"story-modal${projectId}")
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

            var ticketId = HelperService.GetDataObjectFromSlashCommand(command, "ticket-id").ToString() ?? "0";

            try
            {
                await InitiateTransmitUpdateTicket(ticketId, status, priority);
                await command.RespondAsync($"Successfully updated Ticket {ticketId}", ephemeral: true);
            }
            catch (KeyNotFoundException)
            {
                await command.RespondAsync($"Couldn't updated Ticket {ticketId}. (Ticket Id does not exist)", ephemeral: true);
            }
        }

        public static async Task SynchronizeProjectCommand(SocketSlashCommand command)
        {
            string? projectId = HelperService.GetDataObjectFromSlashCommand(command, "project-id").ToString();
            if (projectId == null)
            {
                logger.Error("Synchronize Project Command did not provide any project id");
                return;
            }
            if (command.GuildId == null)
            {
                logger.Error("Synchronize Project Command did not provide any guild id");
                return;
            }
            DiscordProject? project = GetProjectById(projectId);
            if (project == null)
            {
                logger.Error($"Project ({projectId}) could not be found.");
                await command.RespondAsync($"Couldn't find the requested project.", ephemeral: true);
                return;
            }
            if (await SynchronizeProject(project, (ulong)command.GuildId))
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

        public static async Task UnsynchronizeProjectCommand(SocketSlashCommand command)
        {
            string? projectId = HelperService.GetDataObjectFromSlashCommand(command, "project-id").ToString();
            if (projectId == null)
            {
                logger.Error("Unsynchronize Project Command did not provide any project id");
                return;
            }
            DiscordProject? project = GetProjectById(projectId);
            if (project == null)
            {
                logger.Error($"Project ({projectId}) could not be found.");
                await command.RespondAsync($"Couldn't find the requested project.", ephemeral: true);
                return;
            }
            if (await UnsynchronizeProject(project))
            {
                await command.RespondAsync($"Successfully unsynchronized the project {projectId}.", ephemeral: true);
                return;
            }
            else
            {
                await command.RespondAsync($"Failed to unsynchronize the project (No active synchronization).", ephemeral: true);
                return;
            }
        }

        public static async Task ForceUnwatchCommand(SocketSlashCommand command)
        {
            string? ticketId = HelperService.GetDataObjectFromSlashCommand(command, "ticket-id").ToString();
            if (ticketId == null)
            {
                logger.Error("Project Delete Command did not provide any project id");
                return;
            }

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
            catch (KeyNotFoundException)
            {
                await command.RespondAsync(
                    $"Failed to force removed ticket {ticketId} from your watchlist.\n" +
                    "Reason: A ticket with the given Id does not exist. Maybe the ticket does not exist anymore?", ephemeral: true);
            }
        }
    }
}
