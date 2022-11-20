using Support.Shared.Enums;

namespace Support.Shared
{
    public class TicketCreateRequest
    {
        public string RequestId { get; set; }

        public ETicketType Type { get; set; }
        public ETicketStatus Status { get; set; }
        public ETicketPriority Priority { get; set; }
        public string Title { get; set; }
        public Dictionary<string, string> CustomFields { get; set; }
        public string Author { get; set; }

        public TicketCreateRequest() { }

        public TicketCreateRequest(ETicketType Type, ETicketStatus Status, ETicketPriority Priority,
            string Title, Dictionary<string, string> CustomFields, string Author)
        {
            this.RequestId = Guid.NewGuid().ToString();
            this.Type = Type;
            this.Status = Status;
            this.Priority = Priority;
            this.Title = Title;
            this.CustomFields = CustomFields;
            this.Author = Author;
        }
    }
}
