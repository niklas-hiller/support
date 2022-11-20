using Support.Shared;

namespace Support.Discord.Models
{
    public class DiscordTicket : Ticket
    {
        public ulong? MessageId { get; set; }
        public List<ulong> Watchers { get; set; } = new List<ulong>();

        public DiscordTicket(Ticket ticket) :
            base(ticket.Id, ticket.Type, ticket.Status, ticket.Priority,
                ticket.Title, ticket.CustomFields, ticket.Author,
                ticket.CreatedAt, ticket.LastUpdatedAt, ticket.ProjectId)
        { }

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
    }
}
