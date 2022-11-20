using Microsoft.AspNetCore.SignalR;
using Support.Shared;
using System.Net.Sockets;

namespace Support.Server.Hubs;

public class SupportHub : Hub
{
    private readonly ILogger logger;
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

    public async Task SendTicketCreate(Session session, TicketCreateRequest ticketCreate)
    {
        logger.LogInformation($"Received new ticket from {session.Name}");

        Ticket ticket = new Ticket(ticketCreate);
        tickets.Add(ticket);

        logger.LogInformation($"Sending new ticket ({ticket.Id}) to all sessions in group '{SessionGroups.Listener}'");
        await Clients.Group(SessionGroups.Listener).SendAsync(ServerBroadcasts.SendTicketCreate, ticketCreate.RequestId, ticket);
    }

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