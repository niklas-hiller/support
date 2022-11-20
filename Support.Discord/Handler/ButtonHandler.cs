using Discord.WebSocket;

namespace Support.Discord.Handler
{
    internal static class ButtonHandler
    {
        public static async Task HandleButton(SocketMessageComponent component)
        {
            switch (component.Data.CustomId)
            {
                case "ticket-status-unknown":
                    await component.RespondAsync($"Ticket Status 'Unknown' is exception status when the server couldn't retrieve the status", ephemeral: true);
                    break;
                case "ticket-status-open":
                    await component.RespondAsync($"Ticket Status 'Open' means that the ticket was created, but is nobody is working on it.", ephemeral: true);
                    break;
                case "ticket-status-inprogress":
                    await component.RespondAsync($"Ticket Status 'In Progress' means that someone is working on the ticket.", ephemeral: true);
                    break;
                case "ticket-status-done":
                    await component.RespondAsync($"Ticket Status 'Done' means the content of the ticket was finished.", ephemeral: true);
                    break;
                case "ticket-status-declined":
                    await component.RespondAsync($"Ticket Status 'Declined' means the ticket will not be done.", ephemeral: true);
                    break;
                case "ticket-priority-unknown":
                    await component.RespondAsync($"Ticket Priority 'Unknown' means nobody assigned a priority to this ticket yet.", ephemeral: true);
                    break;
                case "ticket-priority-trivial":
                    await component.RespondAsync($"Ticket Priorities are in order: Blocker > Major > Highest > High > Medium > Low > Lowest > Minor > Trivial", ephemeral: true);
                    break;
                case "ticket-priority-minor":
                    await component.RespondAsync($"Ticket Priorities are in order: Blocker > Major > Highest > High > Medium > Low > Lowest > Minor > Trivial", ephemeral: true);
                    break;
                case "ticket-priority-lowest":
                    await component.RespondAsync($"Ticket Priorities are in order: Blocker > Major > Highest > High > Medium > Low > Lowest > Minor > Trivial", ephemeral: true);
                    break;
                case "ticket-priority-low":
                    await component.RespondAsync($"Ticket Priorities are in order: Blocker > Major > Highest > High > Medium > Low > Lowest > Minor > Trivial", ephemeral: true);
                    break;
                case "ticket-priority-medium":
                    await component.RespondAsync($"Ticket Priorities are in order: Blocker > Major > Highest > High > Medium > Low > Lowest > Minor > Trivial", ephemeral: true);
                    break;
                case "ticket-priority-high":
                    await component.RespondAsync($"Ticket Priorities are in order: Blocker > Major > Highest > High > Medium > Low > Lowest > Minor > Trivial", ephemeral: true);
                    break;
                case "ticket-priority-highest":
                    await component.RespondAsync($"Ticket Priorities are in order: Blocker > Major > Highest > High > Medium > Low > Lowest > Minor > Trivial", ephemeral: true);
                    break;
                case "ticket-priority-major":
                    await component.RespondAsync($"Ticket Priorities are in order: Blocker > Major > Highest > High > Medium > Low > Lowest > Minor > Trivial", ephemeral: true);
                    break;
                case "ticket-priority-critical":
                    await component.RespondAsync($"Ticket Priorities are in order: Blocker > Major > Highest > High > Medium > Low > Lowest > Minor > Trivial", ephemeral: true);
                    break;
                case "ticket-priority-blocker":
                    await component.RespondAsync($"Ticket Priorities are in order: Blocker > Major > Highest > High > Medium > Low > Lowest > Minor > Trivial", ephemeral: true);
                    break;
            }
        }
    }
}
