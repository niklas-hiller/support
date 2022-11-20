﻿using Support.Discord.Enums;

namespace Support.Discord.Exceptions
{
    internal class RuleException : Exception
    {
        public ECommandRules Reason { get; protected set; }

        public readonly static Dictionary<ECommandRules, string> Rules = new Dictionary<ECommandRules, string>
        {
            { ECommandRules.NO_DM, "You can't use this command outside of guilds." },
            { ECommandRules.REQUIRES_INITIALIZE, "The server is not initialized yet, please use '/initiate' first." },
            { ECommandRules.REQUIRES_SUPPORT_CHANNEL, "The server does not has a support channel yet, please use '/support-channel' first." }
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
