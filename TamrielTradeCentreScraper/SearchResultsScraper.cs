using HtmlAgilityPack;
using LibAlerts;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
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
        static DateTime lastCaptcha = new DateTime();
        
        public static IEnumerable<Result> Scrape(string url, int ageLimitMinutes, int maxResultCount, int scrapeDelayMs)
        {
            List<Result> results = new List<Result>();

            if (!url.Contains("&SortBy=LastSeen&Order=desc"))
            {
                url = $"{url}&SortBy=LastSeen&Order=desc";
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri searchUriAsc))
            {
                var searchResultsNodesXPath = "/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/table/tbody";
                int delay = 0;
                if (scrapeDelayMs > 0)
                {
                    int pagesToScrape = maxResultCount / 10;
                    delay = pagesToScrape > 0 ? scrapeDelayMs / pagesToScrape : scrapeDelayMs;
                }
                Thread.Sleep(delay);

                //GetProxies.FromFreeProxyListNet();
                var siteScraper = new Scraper();
                var docAsc = siteScraper.TryLoadHtmlDocument(searchUriAsc);
                var searchResultNodesAsc = siteScraper.GetChildNodes(searchResultsNodesXPath);

                if (searchResultNodesAsc == null)
                {
                    string captchaXpath = "/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/form/div/div";
                    var captchaNode = siteScraper.GetNode(captchaXpath);
                    if (captchaNode != null)
                    {
                        if (PromptUserForCaptcha(url))
                        {
                            return Scrape(url, ageLimitMinutes, maxResultCount, scrapeDelayMs);
                        }
                    }
                    return results;
                }

                List<HtmlNode> nodesToParse = new List<HtmlNode>();
                nodesToParse.AddRange(from srn in searchResultNodesAsc where srn.GetClasses().Contains("cursor-pointer") select srn);

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
                    if (url.Contains("&SortBy=LastSeen&Order=desc"))
                    {
                        if ((from r in results where DateTime.Now.Subtract(r.LastSeen).TotalMinutes >= ageLimitMinutes select r).Count() > 0)
                        {
                            return results;
                        }
                    }
                    // check for more pages of results
                    var paginationNodes = siteScraper.GetChildNodes("/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/div[3]/ul");
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
                                        results.AddRange(Scrape(nextPageUrl, ageLimitMinutes, maxResultCount - results.Count, scrapeDelayMs - delay));
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

        private static bool PromptUserForCaptcha(string url)
        {
            if (lastCaptcha > DateTime.Now.Subtract(TimeSpan.FromSeconds(15)))
            {
                Console.WriteLine($"Waiting for user to do CAPTCHA");
                new VoiceSynth().Speak("CAPTCHA time");
                Thread.Sleep(10000);
            }
            else
            {

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{DateTime.Now} Opening browser for CAPTCHA");
                Console.ResetColor();            
                Process.Start(url);
                new VoiceSynth().Speak("CAPTCHA time");
                Thread.Sleep(10000);
            }

            lastCaptcha = DateTime.Now;
            return true;
        }

        private static void AttemptRoboCaptcha(string url)
        {            
            using (var driver = new ChromeDriver(@"C:\Program Files (x86)\ChromeDriver"))
            {
                // navigate to the page with the captcha
                Uri captchaUri = new Uri(url);
                driver.Navigate().GoToUrl(captchaUri);

                // wait for captcha to load
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);

                // get captcha frame
                var iFrame = driver.FindElement(By.XPath("/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/form/div/div/div/div/iframe"));
                driver.SwitchTo().Frame(iFrame);

                // Now can click on checkbox of reCaptcha now.
                Thread.Sleep(1300);
                var captchaCheckBox = driver.FindElement(By.XPath("/html/body/div[2]/div[3]/div[1]/div/div/span/div[1]"));

                CaptchaElement.DoMouseClick(149, 704);
                Thread.Sleep(20000); // wait for click on captcha to validate

                try
                {
                    // click confirm
                    driver.SwitchTo().ParentFrame();
                    var submitElement = driver.FindElementByCssSelector("/html/body/div[2]/table/tbody/tr[3]/td[2]/section/div/form/input[20]"); // submit
                    submitElement.Click();
                }
                catch (InvalidSelectorException ex)
                {
                    // solve further challenges
                    var imageSelectoriFrame = driver.FindElement(By.XPath("/html/body/div[3]/div[4]/iframe"));
                    driver.SwitchTo().Frame(imageSelectoriFrame);
                    var challengeText = driver.FindElement(By.XPath("/html/body/div/div/div[2]/div[1]/div[1]/div")).Text.Split(Environment.NewLine.ToCharArray());
                    Console.WriteLine(challengeText);
                    if (challengeText.Contains("Select all squares with") || challengeText.Contains("Select all images with"))
                    {
                        string selectType = challengeText[2];
                        // review images, select the appropriate ones
                    }
                    else
                    {
                        throw new NotImplementedException($"Handling of challenge '{challengeText}' not yet implemented.");
                    }
                }
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
