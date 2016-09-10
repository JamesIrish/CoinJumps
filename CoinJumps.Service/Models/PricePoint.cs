using System;

namespace CoinJumps.Service.Models
{
    public class PricePoint
    {
        public PricePoint()
        {
            TimeStamp = DateTimeOffset.UtcNow;
        }
        public DateTimeOffset TimeStamp { get; }
        public string Coin { get; set; }
        public decimal Price { get; set; }
    }
}