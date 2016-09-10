using System;

namespace CoinJumps.Service.Models
{
    public class CoinCapTradeEvent
    {
        public CoinCapTradeEvent()
        {
            Timestamp = DateTimeOffset.UtcNow;
        }

        public DateTimeOffset Timestamp { get; }

        public string Coin { get; set; }
        
        public CoinCapTradeMessage Msg { get; set; }
    }
}