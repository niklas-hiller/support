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
                case "bug":
                    return ETicketType.Bug;
                case "request":
                    return ETicketType.Request;
            }
            return ETicketType.Unknown;
        }
    }
}
