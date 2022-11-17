using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Support.Shared
{
    public class Ticket
    {
        public readonly string Id;
        public ETicketType Type { get; set; }
        public ETicketStatus Status { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public readonly DateTimeOffset CreatedAt;
        public DateTimeOffset LastUpdatedAt { get; set; }

        public Ticket(ETicketType Type, ETicketStatus Status,
            string Title, string Description, string Author,
            DateTimeOffset CreatedAt, DateTimeOffset LastUpdatedAt)
        {
            this.Id = Guid.NewGuid().ToString();
            this.Type = Type;
            this.Status = Status;
            this.Title = Title;
            this.Description = Description;
            this.Author = Author;
            this.CreatedAt = CreatedAt;
            this.LastUpdatedAt = LastUpdatedAt;
        }

        public Ticket(string Id, ETicketType Type, ETicketStatus Status, 
            string Title, string Description, string Author,
            DateTimeOffset CreatedAt, DateTimeOffset LastUpdatedAt)
        {
            this.Id = Id;
            this.Type = Type;
            this.Status = Status;
            this.Title = Title;
            this.Description = Description;
            this.Author = Author;
            this.CreatedAt = CreatedAt;
            this.LastUpdatedAt = LastUpdatedAt;
        }
    }
}
