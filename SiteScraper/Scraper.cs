using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SiteScraper
{
    public class Scraper
    {
        public static List<KeyValuePair<string, string>> Scrape(Uri uri, IEnumerable<string> nodes)
        {
            List<KeyValuePair<string, string>> returnValues = new List<KeyValuePair<string, string>>();

            HtmlWeb web = new HtmlWeb();
            var htmlDoc = web.Load(uri);

            foreach(string xPath in nodes)
            {
                try
                {
                    var value = htmlDoc.DocumentNode.SelectSingleNode(xPath).InnerText;
                    KeyValuePair<string, string> kvp = new KeyValuePair<string, string>(xPath, value);
                    returnValues.Add(kvp);
                }
                catch
                {
                    Console.WriteLine($"Nothing found at {xPath}");
                }
            }

            return returnValues;
        }
    }
}
