using Discord.WebSocket;

namespace Support.Discord.Services
{
    public static class HelperService
    {
        public static object GetDataObjectFromSlashCommand(SocketSlashCommand command, string name)
        {
            return command.Data.Options.First(x => x.Name == name).Value;
        }
    }
}
