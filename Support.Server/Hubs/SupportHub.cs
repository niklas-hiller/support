using Microsoft.AspNetCore.SignalR;
using Support.Shared;
using Support.Shared.Enums;
using System.Net.Sockets;

namespace Support.Server.Hubs;

internal class SupportHub : Hub
{
    private readonly ILogger logger;
    private static readonly Dictionary<string, Project> projects = new Dictionary<string, Project>();
    private static readonly List<Ticket> tickets = new List<Ticket>();

    public SupportHub(ILogger<SupportHub> logger)
    {
        this.logger = logger;
    }

    public async Task SessionConnected(Session session)
    {
        // Add the session to the group name.
        await Groups.AddToGroupAsync(Context.ConnectionId, session.GroupName);
        logger.LogInformation($"{session.Name} connected as {session.GroupName}");

        // await Clients.OthersInGroup(session.GroupName).SendAsync(ServerBroadcasts.SessionConnected, session);
    }

    /// <summary>
    /// Receives a project create request from client.
    /// Will create a new project with the given information of the request.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="projectCreate"></param>
    /// <returns>The created project</returns>
    public async Task CreateNewProject(Session session, ProjectCreateRequest projectCreate)
    {
        Project project = new Project(projectCreate.Name);
        projects.Add(project.Id, project);
        logger.LogInformation($"{session.Name} created a new Project '{project.Name}' ({project.Id})");

        await Clients.Caller.SendAsync(ServerBroadcasts.SendProject, projectCreate.RequestId, project);
    }

    /// <summary>
    /// Receives a ticket create request from client.
    /// Will create a new ticket with the informations contained in the ticket create request.
    /// Created ticket will be added to corresponding project.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="ticketCreate"></param>
    /// <returns>The created ticket</returns>
    public async Task SendTicketCreate(Session session, TicketCreateRequest ticketCreate)
    {
        logger.LogInformation($"Received new ticket from {session.Name}");

        Project project = projects[ticketCreate.ProjectId];
        Ticket ticket = new Ticket(ticketCreate);
        project.Tickets.Add(ticket);
        tickets.Add(ticket);

        logger.LogInformation($"Sending new ticket ({ticket.Id}) to all sessions in group '{SessionGroups.Listener}'");
        await Clients.Group(SessionGroups.Listener).SendAsync(ServerBroadcasts.SendTicketCreate, ticket);
    }

    /// <summary>
    /// Receives Ticket Update request from client.
    /// Will try to update priority, status and last updated value of the ticket.
    /// If ticket is not found, will send a Server Error message to the caller.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="ticketUpdate"></param>
    /// <returns>The updated ticket</returns>
    public async Task SendTicketUpdate(Session session, TicketUpdateRequest ticketUpdate)
    {
        logger.LogInformation($"Received updated ticket ({ticketUpdate.Id}) from {session.Name}");

        try
        {
            Ticket ticket;
            ticket = tickets.First(x => x.Id == ticketUpdate.Id);

            ticket.Priority = ticketUpdate.Priority;
            ticket.Status = ticketUpdate.Status;
            ticket.LastUpdatedAt = DateTimeOffset.Now;

            logger.LogInformation($"Sending updated ticket ({ticket.Id}) to all sessions in group '{SessionGroups.Listener}'");
            await Clients.Group(SessionGroups.Listener).SendAsync(ServerBroadcasts.SendTicketUpdate, ticket);
        }
        catch (InvalidOperationException e)
        {
            var error = new ServerError(EServerError.TICKET_ID_NOT_FOUND, $"Couldn't find any ticket with the id {ticketUpdate.Id}");
            logger.LogError(error.Message);
            await Clients.Caller.SendAsync(ServerBroadcasts.SendServerError, error);
            return;
        }

    }
}