﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Support.Discord.Models
{
    internal class DiscordUser
    {
        public ulong DiscordId { get; set; }
        public string ServerId { get; set; }

        public DiscordUser(ulong discordId)
        {
            DiscordId = discordId;
            ServerId = discordId.ToString();
        }
    }
}
