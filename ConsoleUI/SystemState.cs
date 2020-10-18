using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TamrielTradeCentreScraper;

namespace ConsoleUI
{
    class SystemState
    {
        public List<Watcher> Watchers { get; set; }

        public TimeSpan SearchInterval = TimeSpan.FromSeconds(900);

        public SystemState() { }
    }
}
