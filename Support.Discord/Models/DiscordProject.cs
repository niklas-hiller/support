using Support.Shared;

namespace Support.Discord.Models
{
    internal class Synchronization
    {
        public readonly ulong GuildId;
        public readonly ulong ChannelId;

        public Synchronization(ulong guildId, ulong channelId) 
        {
            GuildId = guildId;
            ChannelId = channelId;
        }
    }

    internal class DiscordProject : Project
    {
        public readonly ulong ProjectOwner;
        public Synchronization? Synchronization;
        public new List<DiscordTicket> Tickets { get; protected set; } = new List<DiscordTicket>();

        public DiscordProject(Project project, ulong projectOwner)
            : base(project.Id, project.Name, project.Tickets)
        {
            this.ProjectOwner = projectOwner;
        }
    }
}
