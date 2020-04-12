using System;
using System.Collections.Generic;
using System.Text;

namespace TamrielTradeCentreScraper
{
    public class Result
    {
        public string Name { get; set; }
        public string Trader { get; set; }
        public TamrielLocation Location { get; set; }
        public decimal PricePerUnit { get; set; }
        public int StackSize { get; set; }
        public DateTime LastSeen { get; set; }
    }
}
