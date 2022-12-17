using Discord;

namespace Support.Discord.Models
{
    internal class LocalizedString
    {
        public string Default { get; set; }
        public Dictionary<string, string> Localization { get; set; }
        
        public string LocalizedDefault() {
            return Localization[Default];
        }
    }

    internal class DiscordCommandChoices
    {
        public LocalizedString Name { get; set; }
        public string Value { get; set; }
    }

    internal class DiscordCommandOptions
    {
        public LocalizedString Name { get; set; }
        public LocalizedString Description { get; set; }
        public bool Required { get; set; } = false;
        public string Type { get; set; }
        public List<DiscordCommandChoices> Choices { get; set; } = new();

        public ApplicationCommandOptionType OptionType()
        {
            return Enum.Parse<ApplicationCommandOptionType>(Type, true);
        }
    }

    internal class DiscordCommand
    {
        public LocalizedString Name { get; set; }
        public LocalizedString Description { get; set; }
        public List<DiscordCommandOptions> Options { get; set; } = new();
    }
}
