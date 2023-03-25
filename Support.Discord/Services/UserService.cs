using Support.Discord.Models;

namespace Support.Discord.Services
{
    internal class UserService
    {
        private static readonly List<DiscordUser> users = new List<DiscordUser>();

        public static DiscordUser GetUserByServerId(string userId)
        {
            return users.First(x => x.ServerId == userId);
        }

        public static DiscordUser GetUserByDiscordId(ulong userId)
        {
            try
            {
                return users.First(x => x.DiscordId == userId);
            }
            catch (InvalidOperationException)
            {
                DiscordUser user = new DiscordUser(userId);
                users.Append(user);
                return user;
            }
        }
    }
}
