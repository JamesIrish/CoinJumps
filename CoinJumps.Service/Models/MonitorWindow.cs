using System;

namespace CoinJumps.Service.Models
{
    public class MonitorWindow
    {
        public string Coin { get; set; }
        public TimeSpan WindowDuration { get; set; }
        public decimal PercentThreshold { get; set; }
    }
}