namespace Support.Shared
{
    public static class ServerBroadcasts
    {
        public const string SessionConnected = "SessionConnected";

        public const string CreateProject = "CreateProject";
        public const string DeleteProject = "DeleteProject";
        public const string RetrieveProject = "RetrieveProject";
        public const string SendProject = "SendProject";

        public const string TicketCreate = "TicketCreate";
        public const string TicketUpdate = "TicketUpdate";
        public const string SendTicket = "SendTicket";

        public const string SendServerError = "SendServerError";
    }
}
