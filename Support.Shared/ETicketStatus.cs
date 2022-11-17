namespace Support.Shared
{
    public enum ETicketStatus
    {
        Unknown,
        Open,
        In_Progress,
        Done,
        Declined,
    }

    public static class TicketStatus
    {
        public static ETicketStatus FromString(string status)
        {
            switch (status.ToLower().Replace(" ", ""))
            {
                case "open":
                    return ETicketStatus.Open;
                case "inprogress":
                    return ETicketStatus.In_Progress;
                case "done":
                    return ETicketStatus.Done;
                case "declined":
                    return ETicketStatus.Declined;
            }
            return ETicketStatus.Unknown;
        }
    }  
}