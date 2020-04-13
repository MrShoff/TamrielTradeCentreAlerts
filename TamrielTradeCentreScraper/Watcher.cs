using LibAlerts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TamrielTradeCentreScraper
{
    public class Watcher
    {
        public int ItemId { get; private set; }
        public int MaxPricePerUnit { get; set; } = 9999999;
        public int MinStackPrice { get; set; } = 0;
        public int LastSeenThresholdMinutes { get; set; } = 90;
        public int MaxResultCount { get; set; } = 40;
        public string Description { get; set; } = "unknown";

        private List<long> _alertsSent = new List<long>();
        VoiceSynth _voice = new VoiceSynth();

        public Watcher(int itemId)
        {
            ItemId = itemId;
        }

        public bool TryProcessWatcher(int scrapeDelayMs)
        {
            Stopwatch timer = Stopwatch.StartNew();
            if (MaxResultCount == 1 && Description == "Robot Test")
            {
                var scraperResults = SearchResultsScraper.Scrape(GetUrl(), MaxResultCount, scrapeDelayMs);
                if (scraperResults.Count() != 1)
                {
                    return false;
                }
                ProcessResults(scraperResults);
            }
            else
            {
                ProcessResults(SearchResultsScraper.Scrape(GetUrl(), MaxResultCount, scrapeDelayMs));
            }
            timer.Stop();
            if (scrapeDelayMs > timer.Elapsed.TotalMilliseconds) Thread.Sleep(scrapeDelayMs - (int)timer.Elapsed.TotalMilliseconds);
            return true;
        }

        private string GetUrl()
        {
            //string url = $"https://us.tamrieltradecentre.com/pc/Trade/SearchResult?ItemID=4909&SearchType=Sell&ItemNamePattern={name}&ItemCategory1ID=&ItemTraitID=&ItemQualityID=&IsChampionPoint=false&LevelMin=&LevelMax=&MasterWritVoucherMin=&MasterWritVoucherMax=&AmountMin=200&AmountMax=&PriceMin=&PriceMax={maxPpu}&SortBy=Price&Order=asc";
            string url = "https://us.tamrieltradecentre.com/pc/Trade/SearchResult?SearchType=Sell";
            url += $"&ItemID={ItemId}&PriceMax={MaxPricePerUnit}&AmountMin={MinStackPrice}";
            return url;
        }

        private void ProcessResults(IEnumerable<Result> results)
        {
            var filteredResults = from r in results where r.LastSeen > DateTime.Now.AddMinutes(-LastSeenThresholdMinutes) orderby r.PricePerUnit select r; // limit search results to lastSeenThresholdMinutes

            // header
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"{DateTime.Now.ToString("hh:mm:ss")} ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(Description);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($" ({results.Count()}/{MaxResultCount} found, {filteredResults.Count()} in the last {LastSeenThresholdMinutes} minutes) ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($":");
            Console.ResetColor();
            

            // print results to console
            foreach (var result in filteredResults)
            {
                Console.Write("   ");
                if (!_alertsSent.Contains(result.TradeId)) // alert if new
                {
                    _voice.Speak($"New {Description} found!");
                    _alertsSent.Add(result.TradeId);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("NEW! ");
                    Console.ResetColor();
                }
                Console.WriteLine(result.ToString());
            }
        }
    }
}
