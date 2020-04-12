using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace TamrielTradeCentreScraper
{
    public class Result
    {
        public string Name { get; set; }
        public string Trader { get; set; }
        public TamrielLocation Location { get; set; }
        public string ShopOwner { get; set; }
        public decimal PricePerUnit { get; set; }
        public int StackSize { get; set; }
        public DateTime LastSeen { get; set; }
        public long TradeId { get; set; }
        public Uri IconUri { get; set; }

        public override string ToString()
        {
            return $"Price: {PricePerUnit}x{StackSize} Location: {Location.City} ({Location.Province}) Shop Owner: {ShopOwner}; Last Seen: {(DateTime.Now - LastSeen).TotalMinutes}";
        }
    }
}
