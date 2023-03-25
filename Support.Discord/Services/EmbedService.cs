using Discord;
using Discord.WebSocket;
using Support.Discord.Models;
using Support.Shared.Enums;

namespace Support.Discord.Services
{
    internal static class EmbedService
    {
        private static readonly DiscordSocketClient client = Program.client;

        private static void AttachTicketCustomFields(EmbedBuilder builder, DiscordTicket ticket)
        {
            foreach (KeyValuePair<string, string> entry in ticket.CustomFields)
            {
                builder.AddField(entry.Key, entry.Value);
            }
        }

        public static Embed GetTicketEmbedded(DiscordTicket ticket)
        {
            string typeEmoji = EmojiService.GetTypeEmoji(ticket.Type)?.ToString() ?? string.Empty;
            DiscordProject project = SupportService.GetProjectById(ticket.ProjectId);

            var builder = new EmbedBuilder();
            builder
                .WithAuthor(client.CurrentUser.ToString(), client.CurrentUser.GetAvatarUrl() ?? client.CurrentUser.GetDefaultAvatarUrl())
                .WithTitle($"{typeEmoji} [{project.SimplifiedName()}] {ticket.Title}")
                .WithDescription(
                $"**Reporter:** {ticket.Author}\n" +
                $"**Created At:** <t:{ticket.CreatedAt.ToUnixTimeSeconds()}:R>\n" +
                $"**Last Updated At:** <t:{ticket.LastUpdatedAt.ToUnixTimeSeconds()}:R>")
                .WithColor(Color.Green)
                .WithCurrentTimestamp()
                .WithFooter($"Ticket Id: {ticket.Id}");
            AttachTicketCustomFields(builder, ticket);
            return builder.Build();
        }

        public static Embed GetWatcherEmbedded(DiscordTicket ticket)
        {
            var guild = SupportService.GetGuildByProjectId(ticket.ProjectId);
            DiscordProject project = SupportService.GetProjectById(ticket.ProjectId);

            var builder = new EmbedBuilder();
            builder
                .WithAuthor(client.CurrentUser.ToString(), client.CurrentUser.GetAvatarUrl() ?? client.CurrentUser.GetDefaultAvatarUrl())
                .WithTitle($"[Notification] Ticket {ticket.Id} was updated")
                .WithDescription(
                $"There was an update in **{guild.Name}** for the ticket **[{project.SimplifiedName()}] {ticket.Title}**.\n\n" +
                "**If you no longer want to be informed about the ticket, please select unwatch on the ticket.**\n")
                .WithColor(Color.Green)
                .WithCurrentTimestamp()
                .WithFooter($"Type '/force-unwatch {ticket.Id}' to force a ticket unwatch. Keep in mind that the select menu might display a wrong value then!");
            return builder.Build();
        }

        public static Embed GetProjectEmbedded(DiscordProject project)
        {
            var builder = new EmbedBuilder();
            builder
                .WithAuthor(client.CurrentUser.ToString(), client.CurrentUser.GetAvatarUrl() ?? client.CurrentUser.GetDefaultAvatarUrl())
                .WithTitle($"[PROJECT] {project.Name}")
                .WithDescription(
                $"**Owner:** <@{project.Owner}>\n" +
                $"**Created At:** <t:{project.CreatedAt.ToUnixTimeSeconds()}:R>")
                .WithColor(Color.Green)
                .WithCurrentTimestamp()
                .WithFooter($"Project Id: {project.Id}");
            return builder.Build();
        }
    }
}
