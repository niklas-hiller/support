using Discord.WebSocket;

namespace Support.Discord.Services
{
    internal static class HelperService
    {
        public static object GetDataObjectFromSlashCommand(SocketSlashCommand command, string name)
        {
            return command.Data.Options.First(x => x.Name == name).Value;
        }
    }
}
