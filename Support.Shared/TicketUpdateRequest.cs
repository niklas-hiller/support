using Support.Shared.Enums;

namespace Support.Shared
{
    public class TicketUpdateRequest
    {
        public string Id { get; set; }
        public ETicketStatus Status { get; set; }
        public ETicketPriority Priority { get; set; }

        public TicketUpdateRequest() { }

        public TicketUpdateRequest(string id, ETicketStatus status, ETicketPriority priority)
        {
            this.Id = id;
            this.Status = status;
            this.Priority = priority;
        }
    }
}
