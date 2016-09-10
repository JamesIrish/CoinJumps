namespace CoinJumps.Service.Models
{
    public class CoinCapTradeMessage
    {
        public string Position24 { get; set; }
        public string Position { get; set; }
        public string Short { get; set; }
        public string Long { get; set; }
        public long Time { get; set; }
        public decimal Price { get; set; }
        public string Perc { get; set; }
        public string Volume { get; set; }
        public string UsdVolume { get; set; }
        public string Cap24hrChange { get; set; }
        public decimal Mktcap { get; set; }
        public string Supply { get; set; }
        public bool Published { get; set; }
    }
}
