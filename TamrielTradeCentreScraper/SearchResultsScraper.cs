using SiteScraper;
using System;
using System.Collections.Generic;

namespace TamrielTradeCentreScraper
{
    public class SearchResultsScraper
    {
        public IEnumerable<Result> Scrape(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri searchUri))
            {
                string[] xPaths = new string[6]
                {
                    "/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/table/tbody/tr[1]/td[1]/div[1]" // Item Name
                    ,"/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/table/tbody/tr[1]/td[2]/div" // Trader
                    ,"/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/table/tbody/tr[1]/td[3]" // Location
                    ,"/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/table/tbody/tr[1]/td[4]" // Price
                    ,"/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/table/tbody/tr[1]/td[5]" // Last Seen
                    ,"/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/table/tbody/tr[1]" // Result ID
                };
                var scrapedValues = Scraper.Scrape(searchUri, xPaths);
                return ParseSiteScraperResults(scrapedValues);
            }
            else
            {
                throw new ArgumentException($"Could not create Uri from {url}", nameof(url));
            }
        }

        private List<Result> ParseSiteScraperResults(List<KeyValuePair<string, string>> scrapedValues)
        {
            List<Result> results = new List<Result>();

            foreach (var returnedValue in scrapedValues)
            {
                switch (returnedValue.Key)
                {
                    case "/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/table/tbody/tr[1]/td[1]/div[1]": // Item Name:
                        break;
                    case "/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/table/tbody/tr[1]/td[2]/div": // Trader:
                        break;
                    case "/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/table/tbody/tr[1]/td[3]": // Location:
                        break;
                    case "/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/table/tbody/tr[1]/td[4]": // Price:
                        break;
                    case "/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/table/tbody/tr[1]/td[5]": // Last Seen:
                        break;
                    case "/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/table/tbody/tr[1]": // Result ID:
                        break;
                    default:
                        throw new NotImplementedException($"No parsing implemented for: {returnedValue.Key}");
                }
            }

            return results;
        }
    }
}
