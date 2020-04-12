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
        public void CallSiteScraper()
        {
            TimeSpan searchInterval = TimeSpan.FromSeconds(60);

            while(true)
            {
                Stopwatch searchTime = Stopwatch.StartNew();
                var crawlers200Under5 = SearchResultsScraper.Scrape("https://us.tamrieltradecentre.com/pc/Trade/SearchResult?ItemID=4909&SearchType=Sell&ItemNamePattern=Crawlers%2C+Foul+Bait&ItemCategory1ID=&ItemTraitID=&ItemQualityID=&IsChampionPoint=false&LevelMin=&LevelMax=&MasterWritVoucherMin=&MasterWritVoucherMax=&AmountMin=200&AmountMax=&PriceMin=&PriceMax=5&SortBy=Price&Order=asc");
                var perfectRoe1Under5000 = SearchResultsScraper.Scrape("https://us.tamrieltradecentre.com/pc/Trade/SearchResult?ItemID=6132&SearchType=Sell&ItemNamePattern=Perfect+Roe&ItemCategory1ID=&ItemTraitID=&ItemQualityID=&IsChampionPoint=false&LevelMin=&LevelMax=&MasterWritVoucherMin=&MasterWritVoucherMax=&AmountMin=&AmountMax=&PriceMin=&PriceMax=5000&SortBy=Price&Order=asc");

                Console.WriteLine("Crawlers (Stacks of 200 under 5g each):");
                foreach(var result in crawlers200Under5)
                {
                    Console.WriteLine(result.ToString());
                }

                Console.WriteLine("Perfect Roe (Under 5000g each):");
                foreach (var result in perfectRoe1Under5000)
                {
                    Console.WriteLine(result.ToString());
                }

                searchTime.Stop();
                Thread.Sleep(searchInterval - searchTime.Elapsed);
            }

        }
        
    }
}
