using HtmlAgilityPack;
using LibAlerts;
using SiteScraper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using static SiteScraper.HtmlNodeField;

namespace TamrielTradeCentreScraper
{
    public static class SearchResultsScraper
    {
        public static IEnumerable<Result> Scrape(string url, int maxResultCount = 40, int scrapeDelayMs = 0)
        {
            List<Result> results = new List<Result>();

            // get top results from both directions, since you can only order by max price
            /*
            string urlAsc, urlDesc;
            if (url.Contains("&SortBy=Price&Order="))
            {
                if (url.Contains("&SortBy=Price&Order=asc"))
                {
                    urlAsc = url;
                    urlDesc = url.Replace("&SortBy=Price&Order=asc", "&SortBy=Price&Order=desc");
                }
                else if (url.Contains("&SortBy=Price&Order=desc"))
                {

                    urlAsc = url.Replace("&SortBy=Price&Order=desc", "&SortBy=Price&Order=asc");
                    urlDesc = url;
                }
                else
                {
                    throw new Exception("wtf?");
                }
            }
            else
            {
                urlAsc = $"{url}&SortBy=Price&Order=asc";
                urlDesc = $"{url}&SortBy=Price&Order=desc";
            }
            */

            if (!url.Contains("&SortBy=Price&Order=asc")) { url = $"{url}&SortBy=Price&Order=asc"; }

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri searchUriAsc))
            {
                var searchResultsNodesXPath = "/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/table/tbody";
                int delay = 0;
                if (scrapeDelayMs > 0)
                {
                    int pagesToScrape = maxResultCount / 10;
                    delay = pagesToScrape > 0 ? scrapeDelayMs / pagesToScrape : scrapeDelayMs;
                    //Console.ForegroundColor = ConsoleColor.DarkGray;
                    //Console.WriteLine($"{pagesToScrape} pages to scrape. Delaying each call in this search by {delay.ToString("#,##0")}ms.");
                    //Console.ResetColor();
                }
                Thread.Sleep(delay);
                var docAsc = Scraper.GetHtmlDoc(searchUriAsc);
                var searchResultNodesAsc = Scraper.GetChildNodes(docAsc, searchResultsNodesXPath);

                if (searchResultNodesAsc == null)
                {
                    string captchaXpath = "/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/form/div/div";
                    var captchaNode = Scraper.GetNode(docAsc, captchaXpath);
                    if (captchaNode != null)
                    {
                        if (AttemptRoboCaptcha(captchaNode, url))
                        {
                            return Scrape(url, maxResultCount, scrapeDelayMs);
                        }
                    }
                    return results;
                }

                List<HtmlNode> nodesToParse = new List<HtmlNode>();
                nodesToParse.AddRange(from srn in searchResultNodesAsc where srn.GetClasses().Contains("cursor-pointer") select srn);

                /*
                if (maxResultCount > 10 && nodesToParse.Count == 10)
                {
                    Thread.Sleep(delay);
                    var docDesc = Scraper.GetHtmlDoc(searchUriDesc);
                    var searchResultNodesDesc = Scraper.GetChildNodes(docAsc, searchResultsNodesXPath);
                    nodesToParse.AddRange(from srn in searchResultNodesDesc where srn.GetClasses().Contains("cursor-pointer") select srn);
                }
                */

                if (nodesToParse.Count > maxResultCount) 
                { 
                    nodesToParse.RemoveRange((nodesToParse.Count / 2) - ((nodesToParse.Count - maxResultCount) / 2), nodesToParse.Count - maxResultCount); 
                }

                var fieldsWereInterestedIn = GetFieldSet();
                foreach (var field in fieldsWereInterestedIn) // remove parent node path from field path
                {
                    string updatedPath = field.xPath.Replace(searchResultsNodesXPath, "");
                    field.xPath = updatedPath;
                }
                foreach (var node in nodesToParse)
                {
                    if (node != null)
                    {                        
                        var parsedNodeFields = Scraper.ParseNodeFields(node, fieldsWereInterestedIn);
                        results.Add(ReadFieldSet(parsedNodeFields));
                    }
                }

                if (results.Count < maxResultCount && results.Count == 10)
                {
                    // check for more pages of results
                    var paginationNodes = Scraper.GetChildNodes(docAsc, "/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/div[3]/ul");
                    foreach (var lineItemNode in paginationNodes) // look at all LIs
                    {
                        if (lineItemNode.HasChildNodes)
                        {
                            foreach (var childNode in lineItemNode.ChildNodes)
                            {
                                if (!childNode.HasClass("disabled") && childNode.InnerText == "&gt;")
                                {
                                    if (childNode.Attributes.Contains("href"))
                                    {
                                        string nextPageUrl = childNode.Attributes["href"].Value.Replace("amp;", "");
                                        results.AddRange(Scrape(nextPageUrl, maxResultCount - results.Count, scrapeDelayMs - delay));
                                    }
                                }
                            }
                        }
                    }
                }
                
                return results;
            }
            else
            {
                throw new ArgumentException($"Could not create Uri from {url}", nameof(url));
            }
        }

        private static bool AttemptRoboCaptcha(HtmlNode captchaNode, string url)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{DateTime.Now} Opening browser for CAPTCHA");
            Console.ResetColor();
            Process.Start(url);
            new VoiceSynth().Speak("CAPTCHA time");
            Thread.Sleep(10000);
            Console.WriteLine($"{DateTime.Now} CAPTCHA done");
            //var firefoxProcess = Process.GetProcessesByName("firefox").FirstOrDefault();
            //WindowsAPI.SwitchWindow(Process.GetCurrentProcess().MainWindowHandle);
            //MouseHelper.SendMouseClickForeground(firefoxProcess.MainWindowHandle, 3348, 328);// 791, 333); 
            //Thread.Sleep(2000);
            //MouseHelper.SendMouseClickForeground(firefoxProcess.MainWindowHandle, 3354, 400);// 798, 408);
            //MouseHelper.SendMouseClick(firefoxProcess.MainWindowHandle, 3348, 328);// 791, 333); 
            //Thread.Sleep(3000);
            //MouseHelper.SendMouseClick(firefoxProcess.MainWindowHandle, 3354, 400);// 798, 408);
            //Thread.Sleep(1000);
            return true;
        }

        private static Result ReadFieldSet(IEnumerable<HtmlNodeField> fieldSet)
        {
            Result ttcResult = new Result();
            foreach (var field in fieldSet)
            {
                string[] textStrings = null;
                if (field.DataLocation == DataLocationType.InnerText)
                {
                    string lineSeparator = Environment.NewLine;
                    if (field.Value.Contains(lineSeparator))
                    {
                        var strings = field.Value.Split(new string[1] { lineSeparator }, StringSplitOptions.RemoveEmptyEntries);
                        textStrings = (from s in strings where s.Trim() != "" select s.Trim()).ToArray();
                    }
                }
                switch (field.Description)
                {
                    case "Item Name":
                        if (textStrings != null)
                        {
                            ttcResult.Name = textStrings.Length > 0 ? System.Web.HttpUtility.HtmlDecode(textStrings[0]) : string.Empty;
                        }
                        break;
                    case "Trader":
                        if (textStrings != null)
                        {
                            ttcResult.Trader = textStrings.Length > 0 ? System.Web.HttpUtility.HtmlDecode(textStrings[0]) : string.Empty;
                        }
                        break;
                    case "Location":
                        TamrielLocation loc = new TamrielLocation();
                        if (textStrings != null)
                        {
                            if (textStrings.Length >= 2)
                            {
                                if (textStrings[0].Contains(":"))
                                {
                                    int indexOfColon = textStrings[0].IndexOf(":");
                                    loc.Province = textStrings[0].Substring(0, indexOfColon); 
                                    loc.City = textStrings[0].Substring(indexOfColon + 1).Trim();
                                    loc.Province = System.Web.HttpUtility.HtmlDecode(loc.Province);
                                    loc.City = System.Web.HttpUtility.HtmlDecode(loc.City);
                                }
                                ttcResult.Location = loc;
                                ttcResult.ShopOwner = textStrings[1];
                            }
                        }                                             
                        break;
                    case "Price":
                        if (textStrings != null)
                        {
                            ttcResult.Trader = textStrings.Length > 0 ? textStrings[0].Trim() : string.Empty;
                            // TODO: parse price
                            string ppu = textStrings.Length > 0 ? textStrings[0] : string.Empty;
                            string stack = textStrings.Length > 3 ? textStrings[2] : string.Empty;
                            ttcResult.PricePerUnit = decimal.Parse(ppu);
                            ttcResult.StackSize = int.Parse(stack);
                        }
                        break;
                    case "Last Seen":
                        if (!string.IsNullOrEmpty(field.Value))
                        {
                            int minutesOld = int.Parse(field.Value);
                            ttcResult.LastSeen = DateTime.Now.Subtract(TimeSpan.FromMinutes(minutesOld));
                        }
                        break;
                    case "Trade ID":
                        if (!string.IsNullOrEmpty(field.Value))
                        {
                            int lastIndexOfForwardSlash = field.Value.LastIndexOf(@"/");
                            string id = field.Value.Substring(lastIndexOfForwardSlash + 1);
                            ttcResult.TradeId = long.Parse(id);
                        }
                        break;
                    case "Icon PNG":
                        if (!string.IsNullOrEmpty(field.Value))
                        {
                            string pngLocation = "https://us.tamrieltradecentre.com" + field.Value;
                            if (Uri.TryCreate(pngLocation, UriKind.Absolute, out Uri iconUri))
                            {
                                ttcResult.IconUri = iconUri;
                            }
                        }
                        break;
                    default:
                        throw new NotImplementedException($"No parsing implemented for: {field.Description}");
                }
            }

            return ttcResult;
        }

        private static IEnumerable<HtmlNodeField> GetFieldSet()
        {
            return new HtmlNodeField[]
                {
                    new HtmlNodeField()
                    {
                        Description = "Item Name",
                        DataLocation = DataLocationType.InnerText,
                        xPath = "/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/table/tbody/tr[1]/td[1]/div[1]"
                    },
                    new HtmlNodeField()
                    {
                        Description = "Trader",
                        DataLocation = DataLocationType.InnerText,
                        xPath = "/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/table/tbody/tr[1]/td[2]/div"
                    },
                    new HtmlNodeField()
                    {
                        Description = "Location",
                        DataLocation = DataLocationType.InnerText,
                        xPath = "/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/table/tbody/tr[1]/td[3]"
                    },
                    new HtmlNodeField()
                    {
                        Description = "Price",
                        DataLocation = DataLocationType.InnerText,
                        xPath = "/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/table/tbody/tr[1]/td[4]"
                    },
                    new HtmlNodeField()
                    {
                        Description = "Last Seen",
                        DataLocation = DataLocationType.Attribute,
                        AttributeName = "data-mins-elapsed",
                        xPath = "/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/table/tbody/tr[1]/td[5]"
                    },
                    new HtmlNodeField()
                    {
                        Description = "Trade ID",
                        DataLocation = DataLocationType.Attribute,
                        AttributeName = "data-on-click-link",
                        xPath = "/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/table/tbody/tr[1]"
                    },
                    new HtmlNodeField()
                    {
                        Description = "Icon PNG",
                        DataLocation = DataLocationType.Attribute,
                        AttributeName = "src",
                        xPath = "/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/table/tbody/tr[1]/td[1]/img"
                    }
                };        
        }
    }
}
