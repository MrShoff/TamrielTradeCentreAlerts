using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteScraper
{
    public class HtmlNodeField
    {
        public HtmlNode Node { get; set; }
        public string xPath { get; set; }
        public string Description { get; set; }
        public DataLocationType DataLocation { get; set; }
        public string AttributeName { get; set; }
        public string Value { get; set; }

        public enum DataLocationType
        {
            InnerText = 1,
            Attribute = 2
        }
    }
}
