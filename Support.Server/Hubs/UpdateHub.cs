using Support.Shared;
using Microsoft.AspNetCore.SignalR;

namespace Support.Server.Hubs;

public class UpdateHub : Hub
{
    private readonly ILogger logger;

    public UpdateHub(ILogger<UpdateHub> logger)
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

    public async Task SendUpdateEvent(Session session, UpdateEvent updateEvent)
    {
        logger.LogInformation($"Received Update Event from {session.Name}");
        logger.LogInformation($"Sending Update Event to all sessions in group '{SessionGroups.Listener}'");
        await Clients.Group(SessionGroups.Listener).SendAsync(ServerBroadcasts.SendUpdateEvent, updateEvent);
    }
}