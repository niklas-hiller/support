namespace Support.Shared
{
    public class ServerError
    {
        public EServerError ErrorType { get; set; }
        public string Message { get; set; }

        public ServerError() { }

        public ServerError(EServerError errorType, string message)
        {
            this.ErrorType = errorType;
            this.Message = message;
        }
    }
}
