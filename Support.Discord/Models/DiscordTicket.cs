using Support.Shared;

namespace Support.Discord.Models
{
    public class DiscordTicket : Ticket
    {
        public readonly ulong GuildId;
        public ulong? MessageId { get; set; }

        public DiscordTicket(ETicketType Type, ETicketStatus Status, ETicketPriority Priority,
            string Title, Dictionary<string, string> CustomFields, string Author,
            DateTimeOffset CreatedAt, DateTimeOffset LastUpdatedAt,
            ulong GuildId) :
                base(Type, Status, Priority, Title, CustomFields, Author, CreatedAt, LastUpdatedAt)
        {
            this.GuildId = GuildId;
        }

        public DiscordTicket(string Id, ETicketType Type, ETicketStatus Status, ETicketPriority Priority,
            string Title, Dictionary<string, string> CustomFields, string Author,
            DateTimeOffset CreatedAt, DateTimeOffset LastUpdatedAt,
            ulong GuildId): 
                base(Id, Type, Status, Priority, Title, CustomFields, Author, CreatedAt, LastUpdatedAt)
        {
            this.GuildId = GuildId;
        }

        public void Update(Ticket ticket)
        {
            this.Type = ticket.Type;
            this.Status = ticket.Status;
            this.Priority = ticket.Priority;
            this.Title = ticket.Title;
            this.CustomFields = ticket.CustomFields;
            this.Author = ticket.Author;
            this.LastUpdatedAt = ticket.LastUpdatedAt;
        }

        public Ticket Downgrade() => new Ticket(Id, Type, Status, Priority, Title, CustomFields, Author, CreatedAt, LastUpdatedAt);
    }
}
