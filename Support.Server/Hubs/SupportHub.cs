using Microsoft.AspNetCore.SignalR;
using Support.Shared;

namespace Support.Server.Hubs;

public class SupportHub : Hub
{
    private readonly ILogger logger;

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

    public async Task SendTicketUpdate(Session session, Ticket ticket)
    {
        logger.LogInformation($"Received ticket ({ticket.Id}) from {session.Name}");
        logger.LogInformation($"Sending ticket ({ticket.Id}) to all sessions in group '{SessionGroups.Listener}'");
        await Clients.Group(SessionGroups.Listener).SendAsync(ServerBroadcasts.SendTicketUpdate, ticket);
    }
}