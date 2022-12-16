using Support.Discord.Enums;

namespace Support.Discord.Exceptions
{
    internal class RuleException : Exception
    {
        public ECommandRules Reason { get; protected set; }

        public readonly static Dictionary<ECommandRules, string> Rules = new Dictionary<ECommandRules, string>
        {
            { ECommandRules.NO_DM, "You can't use this command outside of guilds." },
        };

        public RuleException(ECommandRules reason)
        {
            this.Reason = reason;
        }

        public override string ToString()
        {
            return Rules[Reason];
        }
    }
}
