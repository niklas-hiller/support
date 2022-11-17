using Support.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Support.Discord.Models
{
    public class DiscordTicket : Ticket
    {
        public readonly ulong GuildId;
        public ulong? MessageId { get; set; }

        public DiscordTicket(ETicketType Type, ETicketStatus Status,
            string Title, string Description, string Author,
            DateTimeOffset CreatedAt, DateTimeOffset LastUpdatedAt,
            ulong GuildId) :
                base(Type, Status, Title, Description, Author, CreatedAt, LastUpdatedAt)
        {
            this.GuildId = GuildId;
        }

        public DiscordTicket(string Id, ETicketType Type, ETicketStatus Status,
            string Title, string Description, string Author,
            DateTimeOffset CreatedAt, DateTimeOffset LastUpdatedAt,
            ulong GuildId): 
                base(Id, Type, Status, Title, Description, Author, CreatedAt, LastUpdatedAt)
        {
            this.GuildId = GuildId;
        }

        public void Update(Ticket ticket)
        {
            this.Type = ticket.Type;
            this.Status = ticket.Status;
            this.Title = ticket.Title;
            this.Description = ticket.Description;
            this.Author = ticket.Author;
            this.LastUpdatedAt = ticket.LastUpdatedAt;
        }

        public Ticket Downgrade() => new Ticket(Id, Type, Status, Title, Description, Author, CreatedAt, LastUpdatedAt);
    }
}
