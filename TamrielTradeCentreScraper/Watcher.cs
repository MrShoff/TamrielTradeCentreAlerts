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
        public double MaxPricePerUnit { get; set; } = 9999999;
        public int MinStackSize { get; set; } = 1;
        public int LastSeenThresholdMinutes { get; set; } = 60;
        public int MaxResultCount { get; set; } = 40;
        public string Description { get; set; } = "unknown";
        public Uri SearchUrl { get; set; }
        public bool PauseProcessing { get; set; } = false;
        public bool Stackable { get; set; } = true;
        public int MinItemCount { get; set; } = 1;

        private List<long> _alertsSent = new List<long>();

        public Watcher() { }

        public Watcher(int itemId)
        {
            ItemId = itemId;
        }

        public Watcher(Uri searchUri)
        {
            SearchUrl = searchUri;
        }

        public override string ToString()
        {
            return $"{Description}: ItemID: {ItemId}, MaxPricePerUnit: {MaxPricePerUnit}, MinStackSize: {MinStackSize}, LastSeenThreshold: {LastSeenThresholdMinutes}, MaxResultCount: {MaxResultCount}";
        }

        public bool TryProcessWatcher(int scrapeDelayMs)
        {
            Stopwatch timer = Stopwatch.StartNew();
            var scraperResults = SearchResultsScraper.Scrape(GetUrl(), LastSeenThresholdMinutes, MaxResultCount, scrapeDelayMs);
            while (PauseProcessing)
            {
                Thread.Sleep(1000);
            }
            if (Stackable)
            {
                ProcessResults(scraperResults);
            }
            else
            {
                ProcessNonStackableResults(scraperResults);
            }
            timer.Stop();
            if (scrapeDelayMs > timer.Elapsed.TotalMilliseconds) Thread.Sleep(scrapeDelayMs - (int)timer.Elapsed.TotalMilliseconds);
            return true;
        }

        private string GetUrl()
        {
            if (SearchUrl != null) return SearchUrl.AbsoluteUri;
            //string url = $"https://us.tamrieltradecentre.com/pc/Trade/SearchResult?ItemID=4909&SearchType=Sell&ItemNamePattern={name}&ItemCategory1ID=&ItemTraitID=&ItemQualityID=&IsChampionPoint=false&LevelMin=&LevelMax=&MasterWritVoucherMin=&MasterWritVoucherMax=&AmountMin=200&AmountMax=&PriceMin=&PriceMax={maxPpu}&SortBy=Price&Order=asc";
            string url = "https://us.tamrieltradecentre.com/pc/Trade/SearchResult?SearchType=Sell";
            url += $"&ItemID={ItemId}&PriceMax={MaxPricePerUnit}&AmountMin={MinStackSize}";
            return url;
        }

        private void ProcessResults(IEnumerable<Result> results)
        {
            var filteredResults = from r in results where r.LastSeen > DateTime.Now.AddMinutes(-LastSeenThresholdMinutes) && r.Trader == "Community" orderby r.PricePerUnit select r; // limit search results to lastSeenThresholdMinutes

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
                if (!_alertsSent.Contains(result.TradeId)) // alert if new
                {
                    new VoiceSynth().Speak($"New {Description} found!");
                    _alertsSent.Add(result.TradeId);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("   NEW! ");
                    Console.ResetColor();
                    Console.Write(result.Name);
                    Console.WriteLine(result.ToString());
                }
            }
        }

        private void ProcessNonStackableResults(IEnumerable<Result> results)
        {
            var timeFilteredResults = from r in results where r.LastSeen > DateTime.Now.AddMinutes(-LastSeenThresholdMinutes) && r.Trader == "Community" orderby r.PricePerUnit select r; // limit search results to lastSeenThresholdMinutes

            List<Result> tempResultsHolder = new List<Result>();
            foreach(var result in timeFilteredResults)
            {
                var resultsInSameCity = from r in timeFilteredResults where r.Location.City == result.Location.City orderby r.Location.Province select r;
                if (resultsInSameCity.Count() >= MinItemCount)
                {
                    tempResultsHolder.AddRange(resultsInSameCity);
                }
            }
            var filteredResults = (from r in tempResultsHolder orderby r.Trader, r.Location.Province select r).Distinct();

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

            List<string> citiesSent = new List<string>();
            // print results to console
            foreach (var result in filteredResults)
            {
                if (!citiesSent.Contains(result.Location.City))
                {
                    if (!_alertsSent.Contains(result.TradeId)) // alert if new
                    {
                        new VoiceSynth().Speak($"New {Description} found!");                        
                        citiesSent.Add(result.Location.City);
                        foreach(var r in filteredResults.Where(x => x.Location.City == result.Location.City))
                        {
                            Console.Write("   ");
                            if (!_alertsSent.Contains(r.TradeId))
                            {
                                _alertsSent.Add(r.TradeId);
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.Write("NEW! ");
                                Console.ResetColor();
                                Console.Write(r.Name);
                                Console.WriteLine(r.ToString());
                            }
                            else
                            {
                                Console.Write(r.Name);
                                Console.WriteLine(r.ToString());
                            }
                        }                        
                    }
                }
                
            }
        }
    }
}
