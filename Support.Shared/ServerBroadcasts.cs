namespace Support.Shared
{
    public static class ServerBroadcasts
    {
        public const string SessionConnected = "SessionConnected";
        public const string SendTicketUpdate = "SendTicketUpdate";
        [Obsolete]
        public const string SendUpdateEvent = "SendUpdateEvent";
    }
}
