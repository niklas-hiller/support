using Support.Shared;

namespace Support.Discord.Models
{
    internal class Synchronization
    {
        public readonly ulong GuildId;
        public readonly ulong ChannelId;
        public readonly ulong MessageId;

        public Synchronization(ulong guildId, ulong channelId, ulong messageId) 
        {
            GuildId = guildId;
            ChannelId = channelId;
            MessageId = messageId;
        }
    }

    internal class DiscordProject : Project
    {
        public Synchronization? Synchronization;
        public new List<DiscordTicket> Tickets { get; protected set; } = new List<DiscordTicket>();

        public DiscordProject(Project project)
            : base(project.Id, project.Name, project.Tickets, project.Owner, project.CreatedAt)
        { }

        public string SimplifiedName()
        {
            List<string> nameSeparated = this.Name.Split(' ').ToList();
            string simplifiedName = "";
            nameSeparated.ForEach(s =>
            {
                simplifiedName += s.Substring(0, 1);
            });
            return simplifiedName;
        }
    }
}
