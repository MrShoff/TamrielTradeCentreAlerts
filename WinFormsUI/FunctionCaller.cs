using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TamrielTradeCentreScraper;

namespace WinFormsUI
{
    class FunctionCaller
    {
        public void CallSiteScraper()
        {
            TimeSpan searchInterval = TimeSpan.FromSeconds(60);

            while(true)
            {
                Stopwatch searchTime = Stopwatch.StartNew();
                var crawlers200Under5 = SearchResultsScraper.Scrape(UrlBuilder("Crawlers, Foul Bait", 10, 200));
                var perfectRoe1Under5000 = SearchResultsScraper.Scrape(UrlBuilder("Perfect Roe", 8000));

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Crawlers under 4g:");
                Console.ResetColor();
                foreach (var result in from r in crawlers200Under5 where r.LastSeen > DateTime.Now.AddMinutes(-60) select r)
                {
                    Console.WriteLine(result.ToString());
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Perfect Roe under 5000g:");
                Console.ResetColor();
                foreach (var result in from r in perfectRoe1Under5000 where r.LastSeen > DateTime.Now.AddMinutes(-60) select r)
                {
                    Console.WriteLine(result.ToString());
                }

                searchTime.Stop();
                Thread.Sleep(searchInterval - searchTime.Elapsed);
            }
        }

        private static string UrlBuilder(string name, int maxPpu, int minStackSize = 1)
        {
            //string url = $"https://us.tamrieltradecentre.com/pc/Trade/SearchResult?ItemID=4909&SearchType=Sell&ItemNamePattern={name}&ItemCategory1ID=&ItemTraitID=&ItemQualityID=&IsChampionPoint=false&LevelMin=&LevelMax=&MasterWritVoucherMin=&MasterWritVoucherMax=&AmountMin=200&AmountMax=&PriceMin=&PriceMax={maxPpu}&SortBy=Price&Order=asc";
            string url = "https://us.tamrieltradecentre.com/pc/Trade/SearchResult?SearchType=Sell";
            url += $"&ItemNamePattern={name}&PriceMax={maxPpu}&AmountMin={minStackSize}&SortBy=Price&Order=asc"; 
            return url;
        }
        
    }
}
