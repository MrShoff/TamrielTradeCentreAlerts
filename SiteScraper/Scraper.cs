using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using static SiteScraper.HtmlNodeField;

namespace SiteScraper
{
    public class Scraper
    {
        public static HtmlDocument GetHtmlDoc(Uri uri)
        {
            HtmlWeb web = new HtmlWeb();
            var htmlDoc = web.Load(uri);
            return htmlDoc;
        }

        public static HtmlNodeCollection GetChildNodes(HtmlDocument doc, string xPath)
        {
            try
            {
                string nodePath = xPath.Replace("/tbody", "");
                var childNodes = doc.DocumentNode.SelectSingleNode(nodePath).ChildNodes;
                return childNodes;
            }
            catch
            {
                //Console.WriteLine($"Nothing found at {xPath}");
            }
            return null;
        }

        public static HtmlNode GetNode(HtmlDocument doc, string xPath)
        {
            try
            {
                string nodePath = xPath.Replace("/tbody", "");
                var selectedNode = doc.DocumentNode.SelectSingleNode(nodePath);
                return selectedNode;
            }
            catch
            {
                //Console.WriteLine($"Nothing found at {xPath}");
            }
            return null;
        }

        public static HtmlNode GetNode(HtmlNode node, string xPath)
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
                    field.Node = GetNode(parentNode, xPath);
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
    }
}