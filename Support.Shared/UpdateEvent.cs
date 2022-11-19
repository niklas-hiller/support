using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Support.Shared
{
    [Obsolete]
    public class UpdateEvent
    {
        public EUpdateEventType Type { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public DateTime ReleaseDate { get; set; }

        public UpdateEvent() { }

        public UpdateEvent(EUpdateEventType Type, string Version, string Description, DateTime ReleaseDate)
        {
            this.Type = Type;
            this.Version = Version;
            this.Description = Description;
            this.ReleaseDate = ReleaseDate;
        }
    }
}
