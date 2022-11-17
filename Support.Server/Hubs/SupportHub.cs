using Support.Shared;
using Microsoft.AspNetCore.SignalR;

namespace Support.Server.Hubs;

public class SupportHub : Hub
{
    public async Task SessionConnected(Session session)
    {
        // Add the session to the group name.
        await Groups.AddToGroupAsync(Context.ConnectionId, session.GroupName);
        Console.WriteLine($"{session.Name} connected as {session.GroupName}");

        // await Clients.OthersInGroup(session.GroupName).SendAsync(ServerBroadcasts.SessionConnected, session);
    }

    public async Task SendTicketUpdate(Session session, Ticket ticket)
    {
        Console.WriteLine($"Received {ticket.Title} from {session.Name}");
        Console.WriteLine($"Sending {ticket.Title} to all sessions in group '{SessionGroups.Listener}'");
        await Clients.Group(SessionGroups.Listener).SendAsync(ServerBroadcasts.SendTicketUpdate, ticket);
    }
}