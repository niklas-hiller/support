namespace Support.Shared
{
    public enum ETicketType
    {
        Unknown,
        Bug,
        Request,
    }

    public static class TicketType
    {
        public static ETicketType FromString(string type)
        {
            switch (type.ToLower().Replace(" ", ""))
            {
                case "open":
                    return ETicketType.Bug;
                case "inprogress":
                    return ETicketType.Request;
            }
            return ETicketType.Unknown;
        }
    }
}
