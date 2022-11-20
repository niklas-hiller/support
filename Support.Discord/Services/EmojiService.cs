using Discord;
using Discord.WebSocket;
using Support.Discord.Models;
using Support.Shared.Enums;

namespace Support.Discord.Services
{
    public static class EmojiService
    {
        private static readonly DiscordSocketClient client = Program.client;

        private static readonly BotConfiguration configuration = Program.configuration;

        private static Dictionary<string, string> Emojis = new Dictionary<string, string>
        {
            { "priority_unknown", "Images/Unknown.png" },
            { "priority_trivial", "Images/Trivial.png" },
            { "priority_minor", "Images/Minor.png" },
            { "priority_lowest", "Images/Lowest.png" },
            { "priority_low", "Images/Low.png" },
            { "priority_medium", "Images/Medium.png" },
            { "priority_high", "Images/High.png" },
            { "priority_highest", "Images/Highest.png" },
            { "priority_major", "Images/Major.png" },
            { "priority_critical", "Images/Critical.png" },
            { "priority_blocker", "Images/Blocker.png" },
        };

        private static Emote? GetEmoji(string emojiName)
        {
            var source = client.GetGuild(configuration.RootGuildId);
            if (source == null) return null;

            var emoji = source.Emotes.FirstOrDefault(x => x.Name == emojiName, null);

            if (emoji == null)
            {
                string emojiPath;
                if (!Emojis.TryGetValue(emojiName, out emojiPath))
                {
                    return null;
                }
                return source.CreateEmoteAsync(emojiName, new Image(emojiPath)).GetAwaiter().GetResult();
            }

            return source
                .Emotes
                .FirstOrDefault(x => x.Name.IndexOf(
                    emojiName, StringComparison.OrdinalIgnoreCase) != -1);
        }

        public static Emote? GetPriorityEmoji(ETicketPriority priority)
        {
            string? emojiName = null;
            switch (priority)
            {
                case ETicketPriority.Unknown:
                    emojiName = "priority_unknown";
                    break;
                case ETicketPriority.Trivial:
                    emojiName = "priority_trivial";
                    break;
                case ETicketPriority.Minor:
                    emojiName = "priority_minor";
                    break;
                case ETicketPriority.Lowest:
                    emojiName = "priority_lowest";
                    break;
                case ETicketPriority.Low:
                    emojiName = "priority_low";
                    break;
                case ETicketPriority.Medium:
                    emojiName = "priority_medium";
                    break;
                case ETicketPriority.High:
                    emojiName = "priority_high";
                    break;
                case ETicketPriority.Highest:
                    emojiName = "priority_highest";
                    break;
                case ETicketPriority.Major:
                    emojiName = "priority_major";
                    break;
                case ETicketPriority.Critical:
                    emojiName = "priority_critical";
                    break;
                case ETicketPriority.Blocker:
                    emojiName = "priority_blocker";
                    break;
            }
            if (emojiName == null) return null;

            return GetEmoji(emojiName);
        }
    }
}
