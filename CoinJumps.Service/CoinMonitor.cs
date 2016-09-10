using System;
using System.Reactive.Linq;
using CoinJumps.Service.Models;
using log4net;

namespace CoinJumps.Service
{
    public class CoinMonitor : IDisposable
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CoinMonitor));


        public string Coin { get; }
        public TimeSpan Window { get; }
        public decimal PercentageThreshold { get; }


        private IDisposable _subscription;

        public CoinMonitor(ITradeObserver tradeObserver, string coin, TimeSpan window, decimal percentageThreshold)
        {
            Coin = coin;
            Window = window;
            PercentageThreshold = percentageThreshold;
            // Get the first price update  (no need to dispose as First() completes automatically)
            tradeObserver.TradeStream
                .Where(t => t.Coin.Equals(coin))
                .FirstAsync()
                .Subscribe(first =>
                {
                    // Note the price
                    LastPrice = new PricePoint {Coin = first.Coin, Price = first.Msg.Price};

                    // Subscribe to the relevant window
                    _subscription = tradeObserver.TradeStream
                        .Where(t => t.Coin.Equals(coin))
                        .Sample(window)
                        .Subscribe(Compare);
                });
        }

        public PricePoint LastPrice { get; set; }

        private void Compare(CoinCapTradeEvent tradeEvent)
        {
            var last = LastPrice.Price;
            var curr = tradeEvent.Msg.Price;
            var move = (curr/last) - 1m;
            var perc = move*100;
            var mesg = $"{tradeEvent.Msg.Long} moved by {perc:N2}% to ${tradeEvent.Msg.Price:N6}";
            if (perc > PercentageThreshold || perc < -PercentageThreshold)
                Logger.Warn(mesg);
            else
                Logger.Debug(mesg);

            LastPrice = new PricePoint {Coin = Coin, Price = curr};
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }
    }
}