using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Support.Shared
{
    public class RequestContext
    {
        public string Id { get; set; }
        public string? Agent { get; set; }

        public RequestContext(string agent)
        {
            this.Id = Guid.NewGuid().ToString();
            this.Agent = agent;
        }
    }
}
