using LibAlerts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TamrielTradeCentreScraper;

namespace ConsoleUI
{
    class FunctionCaller
    {
        TimeSpan searchInterval = TimeSpan.FromSeconds(300);
        VoiceSynth voice = new VoiceSynth();

        public void CallSiteScraper()
        {
            List<Watcher> watchers = new List<Watcher>();
            //watchers.Add(new Watcher(4909) { Description = "Crawlers under 3 gold", MaxPricePerUnit = 3, MinStackPrice = 600 });
            watchers.Add(new Watcher(6132) { Description = "Perfect Roe under 8000 gold", MaxPricePerUnit = 8000 });
            watchers.Add(new Watcher(511) { Description = "100+ stack Corn Flower under 250 gold each", MaxPricePerUnit = 250, MinStackPrice = 7500 });
            watchers.Add(new Watcher(211) { Description = "Dreugh Wax under 5000 gold", MaxPricePerUnit = 5000 });
            watchers.Add(new Watcher(5687) { Description = "Tempering Alloy under 4000 gold", MaxPricePerUnit = 4000 });

            // White Fish
            watchers.Add(new Watcher(6268) { Description = "Slaughterfish stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });
            watchers.Add(new Watcher(2275) { Description = "Trodh stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });
            watchers.Add(new Watcher(1220) { Description = "River Betty stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });
            watchers.Add(new Watcher(801) { Description = "Salmon stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });
            watchers.Add(new Watcher(941) { Description = "Spadetail stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });
            watchers.Add(new Watcher(3482) { Description = "Silverside Perch stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });
            watchers.Add(new Watcher(4438) { Description = "Longfin stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });
            watchers.Add(new Watcher(5579) { Description = "Dhufish stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });

            while (true)
            {
                Stopwatch searchTime = Stopwatch.StartNew();
                //Console.Clear();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("===========================================================================================");
                Console.WriteLine($"{DateTime.Now.ToString("hh:mm:ss")} Refreshing items on your watch list ");
                Console.WriteLine("===========================================================================================");
                Console.ResetColor();

                foreach (var watcher in watchers)
                {
                    int scrapeDelay = GetScrapeDelay((int)searchInterval.TotalMilliseconds, watchers.Count);
                    watcher.TryProcessWatcher(scrapeDelay);
                }

                searchTime.Stop();
                if (searchInterval > searchTime.Elapsed)
                {
                    Thread.Sleep(searchInterval - searchTime.Elapsed);
                }
            }
        }

        private int GetScrapeDelay(int searchIntervalMs, int totalWatchers)
        {
            int maxDelay = searchIntervalMs / totalWatchers * 2;
            int delay = new Random().Next(0, maxDelay);
            return delay;
        }

    }
}
