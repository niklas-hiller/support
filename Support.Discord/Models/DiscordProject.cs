using Support.Shared;

namespace Support.Discord.Models
{
    internal class DiscordProject : Project
    {
        public readonly ulong GuildId;
        public ulong? ChannelId { get; set; }
        public new List<DiscordTicket> Tickets { get; protected set; } = new List<DiscordTicket>();

        public DiscordProject(Project project, ulong GuildId)
            : base(project.Id, project.Name, project.Tickets)
        {
            this.GuildId = GuildId;
        }
    }
}
