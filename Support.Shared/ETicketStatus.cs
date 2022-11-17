namespace Support.Shared
{
    public enum ETicketStatus
    {
        Open = 0,
        In_Progress = 1,
        Done = 2,
        Declined = 3,
        Unknown = 4,
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