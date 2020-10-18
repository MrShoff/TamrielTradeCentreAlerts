using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SiteScraper
{
    public class GetProxies
    {
        public static IEnumerable<WebProxy> FromFreeProxyListNet()
        {
            List<WebProxy> workingProxies = new List<WebProxy>();

            List<WebProxy> proxiesToTest = new List<WebProxy>();
            string listXpath = "//tbody";
            var scraper = new Scraper();
            var doc = scraper.TryLoadHtmlDocument(new Uri("https://free-proxy-list.net/"));
            var proxyTable = scraper.GetChildNodes(listXpath);
            foreach(var node in proxyTable)
            {
                if (node.HasChildNodes)
                {
                    string ip = node.ChildNodes[0].InnerText;
                    string port = node.ChildNodes[1].InnerText;
                    if (node.ChildNodes[6].InnerText == "Yes")
                    {
                        proxiesToTest.Add(new WebProxy($"https://{ip}:{port}/"));
                    }
                    else
                    {
                        proxiesToTest.Add(new WebProxy($"http://{ip}:{port}/"));
                    }
                }
            }

            Dictionary<Task<bool>, WebProxy> testIds = new Dictionary<Task<bool>, WebProxy>();

            foreach(WebProxy proxy in proxiesToTest)
            {
                TaskFactory factory = new TaskFactory();
                var proxyTest = factory.StartNew(() => TestProxy(proxy));
                testIds.Add(proxyTest, proxy);
            }

            Task.WhenAll(from p in testIds select p.Key).Wait(); // wait for all tests to finish

            foreach(var item in testIds)
            {
                if (item.Key.Result == true)
                {
                    workingProxies.Add(item.Value);
                }
            }

            return workingProxies;
        }

        private static bool TestProxy(WebProxy proxy)
        {
            try
            {
                var request = WebRequest.Create(proxy.Address);
                request.Timeout = 5000;
                var response = request.GetResponse();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
