using HtmlAgilityPack;
using LibAlerts;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using static SiteScraper.HtmlNodeField;

namespace SiteScraper
{
    public class Scraper
    {
        public HtmlDocument HtmlDoc { get; private set; }
        public CaptchaElement Captcha { get; set;}

        public bool TryLoadHtmlDocument(Uri uri)//, WebProxy proxy = null)
        {
            //var proxy2 = new WebProxy(@"http://ttc-cors.herokuapp.com/");
            //var proxy = new WebProxy("http://88.198.50.103:8080/");
            //HttpClientHandler handler = new HttpClientHandler() { Proxy = proxy };

            //WebRequest.DefaultWebProxy = proxy;

            try
            {
                HtmlWeb web = new HtmlWeb();
                web.PreRequest += (request) =>
                {
                    request.Headers.Add("x-requested-with", "https://us.tamrieltradecentre.com");
                    return true;
                };
                var doc = web.Load($"{uri.AbsoluteUri}");
                HtmlDoc = doc;
                return true;
            }
            catch
            {
                return false;
            }
            

            //HttpClient client = new HttpClient(handler);       

            //var response = client.GetAsync(uri).Result;
            //if (response.IsSuccessStatusCode)
            //{
            //    var htmlContent = response.Content.ReadAsStringAsync().Result;
            //    var htmlDoc = new HtmlDocument();
            //    htmlDoc.LoadHtml(htmlContent);
            //    return htmlDoc;
            //}
            //else
            //{
            //    return null;
            //}
        }

        public HtmlNodeCollection GetChildNodes(string xPath)
        {
            try
            {
                string nodePath = xPath.Replace("/tbody", "");
                var childNodes = HtmlDoc.DocumentNode.SelectSingleNode(nodePath).ChildNodes;
                return childNodes;
            }
            catch
            {
                //Console.WriteLine($"Nothing found at {xPath}");
            }
            return null;
        }

        public HtmlNode GetNode(string xPath)
        {
            try
            {
                string nodePath = xPath.Replace("/tbody", "");
                var selectedNode = HtmlDoc.DocumentNode.SelectSingleNode(nodePath);
                return selectedNode;
            }
            catch
            {
                //Console.WriteLine($"Nothing found at {xPath}");
            }
            return null;
        }

        public static HtmlNode GetNode(string xPath, HtmlNode node)
        {
            try
            {
                string nodePath = xPath.Replace("/tbody", "");
                var selectedNode = node.SelectSingleNode(nodePath);
                return selectedNode;
            }
            catch
            {
                //Console.WriteLine($"Nothing found at {xPath}");
            }
            return null;
        }

        public static IEnumerable<HtmlNodeField> ParseNodeFields(HtmlNode parentNode, IEnumerable<HtmlNodeField> fields)
        {
            _ = parentNode ?? throw new ArgumentNullException(nameof(parentNode));
            _ = fields ?? throw new ArgumentNullException(nameof(fields));

            foreach(var field in fields)
            {
                if (string.IsNullOrEmpty(field.xPath)) throw new ArgumentException("xPath cannot be null or empty");
                int childNodeIndex = field.xPath.IndexOf(@"/", 1);
                if (childNodeIndex == -1)
                {
                    field.Node = parentNode;
                }
                else
                {
                    string xPath = field.xPath.Substring(childNodeIndex + 1);
                    field.Node = GetNode(xPath, parentNode);
                }

                if (field.Node == null)
                {
                    Console.WriteLine($"Could not find field {field.Description} at {field.xPath}");
                }
                else
                {
                    switch (field.DataLocation)
                    {
                        case DataLocationType.InnerText:
                            field.Value = field.Node.InnerText;
                            break;
                        case DataLocationType.Attribute:
                            if (string.IsNullOrEmpty(field.AttributeName)) { throw new ArgumentException("Attribute Name cannot be null or empty when DataLocation = Attribute"); }
                            var attributeValue = field.Node.Attributes[field.AttributeName].Value;
                            field.Value = attributeValue;
                            break;
                        default:
                            throw new NotImplementedException($"No parsing implemented for: {field.DataLocation}");
                    }
                }                
            }            

            return fields;
        }

        private void AttemptRoboCaptcha(string url)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{DateTime.Now} Opening browser for CAPTCHA");
            Console.ResetColor();
            Console.WriteLine("Attempting CAPTCHA...");
            using (var driver = new ChromeDriver(@"C:\Program Files (x86)\ChromeDriver"))
            {
                Uri captchaUri = new Uri(url);
                driver.Navigate().GoToUrl(captchaUri);
                Thread.Sleep(2000);
                var iFrame = driver.FindElement(By.XPath(Captcha.iFrameXPath));
                driver.SwitchTo().Frame(iFrame);

                // Now can click on checkbox of reCaptcha now.

                var captchaCheckBox = driver.FindElement(By.XPath(Captcha.XPath));
                captchaCheckBox.Click();

                // #recaptcha-anchor > div.recaptcha-checkbox-border
                Thread.Sleep(3000);
                driver.SwitchTo().ParentFrame();
                var submitElement = driver.FindElementByCssSelector(Captcha.SubmitXPath); // submit
                submitElement.Click();
            }
            
        }
    }
}