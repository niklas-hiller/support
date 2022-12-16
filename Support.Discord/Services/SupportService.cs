using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.AspNetCore.SignalR.Client;
using NLog;
using Support.Discord.Models;
using Support.Shared;
using Support.Shared.Enums;
using System.Threading.Channels;

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

        private static async Task TransmitProjectCreate(ProjectCreateRequest projectCreateRequest)
        {
            logger.Info($"Sending Project ({projectCreateRequest.Name}) create to server");
            await hubConnection.SendAsync(
                ServerBroadcasts.CreateProject,
                session,
                projectCreateRequest
            );
        }

        public static async Task CreateProject(SocketSlashCommand command)
        {
            var projectName = HelperService.GetDataObjectFromSlashCommand(command, "project-name").ToString();

            ProjectCreateRequest request = new ProjectCreateRequest(projectName);
            projectCreateQueue.Add(request.RequestId, command);

            await TransmitProjectCreate(request);
        }

        private static async Task TransmitProjectDelete(ProjectDeleteRequest projectDeleteRequest)
        {
            logger.Info($"Sending Project ({projectDeleteRequest.ProjectId}) delete to server");
            await hubConnection.SendAsync(
                ServerBroadcasts.DeleteProject,
                session,
                projectDeleteRequest
            );
        }

        public static async Task DeleteProject(SocketSlashCommand command)
        {
            var projectId = HelperService.GetDataObjectFromSlashCommand(command, "project-id").ToString();

            ProjectDeleteRequest request = new ProjectDeleteRequest(projectId);
            projectDeleteQueue.Add(request.RequestId, command);

            await TransmitProjectDelete(request);
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
                    discordProject.GuildId = correspondingCommand.GuildId;
                    discordProject.ChannelId = correspondingCommand.ChannelId;
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
                
                projects.Remove(discordProject);
                await correspondingCommand.RespondAsync($"Successfully deleted your project '{project.Name}'. ({project.Id})", ephemeral: true);
            }
            else
            {
                logger.Info($"Couldn't identify Project ({project.Id}) from server, discarded ticket.");
                return;
            }


        }

        private static async Task TransmitTicketCreate(TicketCreateRequest ticketCreateRequest)
        {
            logger.Info($"Sending Ticket ({ticketCreateRequest.Title}) create to server");
            await hubConnection.SendAsync(
                ServerBroadcasts.TicketCreate,
                session,
                ticketCreateRequest
            );
        }

        private static async Task TransmitTicketUpdate(TicketUpdateRequest ticketUpdateRequest)
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
            return client.GetGuild((ulong)project.GuildId);
        }

        public static bool HasProject(ulong guildId)
        {
            return GetProjectFromGuild(guildId) != null;
        }

        public static async Task SynchronizeProjectToChannel(SocketSlashCommand command)
        {
            var projectId = HelperService.GetDataObjectFromSlashCommand(command, "project-id").ToString();
            if (command.GuildId != null && command.ChannelId != null && projectId != null)
            {
                DiscordProject project = GetProjectById(projectId);
                if (project.GuildId == null && project.ChannelId == null)
                {
                    project.GuildId = command.GuildId;
                    project.ChannelId = command.ChannelId;

                    project.Tickets.ForEach(async ticket =>
                    {
                        await UpdateTicket(ticket);
                    });

                    await command.RespondAsync($"Successfully synchronized the project {projectId}.", ephemeral: true);
                    return;
                } 
                else
                {
                    await command.RespondAsync($"Your project is already synchronized to a different channel.", ephemeral: true);
                    return;
                }
            }
            await command.RespondAsync($"Failed to initialize synchronization for the project {projectId}.", ephemeral: true);
        }

        public static async Task UnsynchronizeProjectFromChannel(SocketSlashCommand command)
        {
            var projectId = HelperService.GetDataObjectFromSlashCommand(command, "project-id").ToString();
            if (projectId != null)
            {
                DiscordProject project = GetProjectById(projectId);
                if (project.GuildId != null && project.ChannelId != null)
                {
                    var guild = client.GetGuild((ulong)project.GuildId);
                    var channel = guild.GetTextChannel((ulong)project.ChannelId);

                    if (guild == null || channel == null)
                    {
                        logger.Warn($"Project {project.Id} has a broken sync!");
                        return;
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

                    await command.RespondAsync($"Successfully unsynchronized the project {projectId}.", ephemeral: true);
                    return;
                }
                else
                {
                    await command.RespondAsync($"Your project is not synchronized to any channel.", ephemeral: true);
                    return;
                }
            }
            await command.RespondAsync($"Failed to initialize unsynchronization for the project {projectId}.", ephemeral: true);
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
    }
}
