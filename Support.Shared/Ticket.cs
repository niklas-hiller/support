using Support.Shared.Enums;

namespace Support.Shared
{
    public class Ticket
    {
        public string Id { get; set; }
        public ETicketType Type { get; set; }
        public ETicketStatus Status { get; set; }
        public ETicketPriority Priority { get; set; }
        public string Title { get; set; }
        public Dictionary<string, string> CustomFields { get; set; }
        public string Author { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset LastUpdatedAt { get; set; }
        public string ProjectId { get; set; }

        public Ticket() { }

        public Ticket(string Id, ETicketType Type, ETicketStatus Status, ETicketPriority Priority,
            string Title, Dictionary<string, string> CustomFields, string Author,
            DateTimeOffset CreatedAt, DateTimeOffset LastUpdatedAt, string ProjectId)
        {
            this.Id = Id;
            this.Type = Type;
            this.Status = Status;
            this.Priority = Priority;
            this.Title = Title;
            this.CustomFields = CustomFields;
            this.Author = Author;
            this.CreatedAt = CreatedAt;
            this.LastUpdatedAt = LastUpdatedAt;
            this.ProjectId = ProjectId;
        }

        public Ticket(TicketCreateRequest request)
        {
            this.Id = Guid.NewGuid().ToString();
            this.Type = request.Type;
            this.Status = request.Status;
            this.Priority = request.Priority;
            this.Title = request.Title;
            this.CustomFields = request.CustomFields;
            this.Author = request.Author;
            this.CreatedAt = DateTimeOffset.Now;
            this.LastUpdatedAt = this.CreatedAt;
            this.ProjectId = request.ProjectId;
        }
    }
}
