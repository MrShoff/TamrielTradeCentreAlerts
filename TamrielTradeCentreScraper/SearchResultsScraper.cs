using HtmlAgilityPack;
using SiteScraper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static SiteScraper.HtmlNodeField;

namespace TamrielTradeCentreScraper
{
    public static class SearchResultsScraper
    {
        public static IEnumerable<Result> Scrape(string url, int maxResultCount = 25)
        {
            List<Result> results = new List<Result>();

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri searchUri))
            {
                var doc = Scraper.GetHtmlDoc(searchUri);
                var searchResultsNodesXPath = "/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/table/tbody";
                var searchResultNodes = Scraper.GetChildNodes(doc, searchResultsNodesXPath);

                var fieldsWereInterestedIn = GetFieldSet();
                foreach(var field in fieldsWereInterestedIn) // remove parent node path from field path
                {
                    string updatedPath = field.xPath.Replace(searchResultsNodesXPath, "");
                    field.xPath = updatedPath;
                }
                var nodesToParse = from srn in searchResultNodes where srn.GetClasses().Contains("cursor-pointer") select srn;
                if (nodesToParse.Count() > maxResultCount) { nodesToParse = nodesToParse.Take(maxResultCount); }
                foreach (var node in nodesToParse)
                {
                    if (node != null)
                    {                        
                        var parsedNodeFields = Scraper.ParseNodeFields(node, fieldsWereInterestedIn);
                        results.Add(ReadFieldSet(parsedNodeFields));
                    }
                }

                if (results.Count < maxResultCount)
                {
                    // check for more pages of results
                    var paginationNodes = Scraper.GetChildNodes(doc, "/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/div[3]/ul");
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
                                        results.AddRange(Scrape(childNode.Attributes["href"].Value, maxResultCount - results.Count));
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
                            ttcResult.Name = textStrings.Length > 0 ? textStrings[0] : string.Empty;
                        }
                        break;
                    case "Trader":
                        if (textStrings != null)
                        {
                            ttcResult.Trader = textStrings.Length > 0 ? textStrings[0] : string.Empty;
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
