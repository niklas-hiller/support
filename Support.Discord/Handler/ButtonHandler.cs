using Discord.WebSocket;
using NLog;
using Support.Discord.Services;

namespace Support.Discord.Handler
{
    internal static class ButtonHandler
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static async Task HandleButton(SocketMessageComponent component)
        {
            logger.Info($"User interacted with button {component.Data.CustomId}");
            switch (component.Data.CustomId.Split('$')[0])
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
                case "project-update":
                    await component.RespondAsync("Not implemented.");
                    break;
                case "project-delete":
                    await HandleProjectDeleteComponent(component);
                    break;
                case "project-unsync":
                    await HandleProjectUnsynchronizeComponent(component);
                    break;
                case "ticket-create-story":
                    await HandleTicketCreateStoryComponent(component);
                    break;
                case "ticket-create-bug":
                    await HandleTicketCreateBugComponent(component);
                    break;
            }
        }

        private static async Task HandleProjectDeleteComponent(SocketMessageComponent component)
        => await SupportService.ProjectDeleteComponent(component);

        private static async Task HandleProjectUnsynchronizeComponent(SocketMessageComponent component)
        => await SupportService.ProjectUnsynchronizeComponent(component);

        private static async Task HandleTicketCreateStoryComponent(SocketMessageComponent component)
        => await SupportService.TicketCreateStoryComponent(component);

        private static async Task HandleTicketCreateBugComponent(SocketMessageComponent component)
        => await SupportService.TicketCreateBugComponent(component);
    }
}
