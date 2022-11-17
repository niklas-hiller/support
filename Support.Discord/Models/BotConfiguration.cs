using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Support.Discord.Models
{
    internal class BotConfiguration
    {
        public string Token { get; set; }
        public ulong RootGuildId { get; set; }
    }
}
