namespace Support.Shared.Enums
{
    public enum ETicketPriority
    {
        Unknown,
        Trivial,
        Minor,
        Lowest,
        Low,
        Medium,
        High,
        Highest,
        Major,
        Critical,
        Blocker
    }

    public static class TicketPriority
    {
        public static ETicketPriority FromString(string priority)
        {
            switch (priority.ToLower().Replace(" ", ""))
            {
                case "trivial":
                    return ETicketPriority.Trivial;
                case "minor":
                    return ETicketPriority.Minor;
                case "lowest":
                    return ETicketPriority.Lowest;
                case "low":
                    return ETicketPriority.Low;
                case "medium":
                    return ETicketPriority.Medium;
                case "high":
                    return ETicketPriority.High;
                case "highest":
                    return ETicketPriority.Highest;
                case "major":
                    return ETicketPriority.Major;
                case "critical":
                    return ETicketPriority.Critical;
                case "blocker":
                    return ETicketPriority.Blocker;
            }
            return ETicketPriority.Unknown;
        }
    }
}
