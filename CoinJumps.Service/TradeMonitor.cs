using System;
using System.Collections.Generic;
using System.Linq;

namespace CoinJumps.Service
{
    public interface ITradeMonitor
    {
        bool Monitor(string coin, TimeSpan window, decimal percentageThreshold);
        int Clear(string coin = "");
        IList<CoinMonitor> List();
        void Dispose();
    }

    public class TradeMonitor : IDisposable, ITradeMonitor
    {
        private readonly Dictionary<string, CoinMonitor> _subscriptions;

        private readonly ITradeObserver _tradeObserver;

        public TradeMonitor(ITradeObserver tradeObserver)
        {
            _tradeObserver = tradeObserver;
            _subscriptions = new Dictionary<string, CoinMonitor>();
        }

        private string GenerateKey(string coin, TimeSpan window, decimal percentageThreshold)
        {
            return $"{coin}:{window.TotalSeconds}:{percentageThreshold}";
        }

        public IList<CoinMonitor> List()
        {
            return _subscriptions.Values.ToList();
        }

        public bool Monitor(string coin, TimeSpan window, decimal percentageThreshold)
        {
            var key = GenerateKey(coin, window, percentageThreshold);
            if (_subscriptions.ContainsKey(key))
            {
                _subscriptions[key].Dispose();
                _subscriptions.Remove(key);
            }

            _subscriptions.Add(key, new CoinMonitor(_tradeObserver, coin, window, percentageThreshold));

            return true;
        }

        public int Clear(string coin = "")
        {
            if (string.IsNullOrWhiteSpace(coin))
                return ClearAll();

            var keys = _subscriptions.Where(kvp => kvp.Value.Coin.Equals(coin)).Select(kvp => kvp.Key).ToList();
            foreach (var key in keys)
            {
                _subscriptions[key].Dispose();
                _subscriptions.Remove(key);
            }
            return keys.Count;
        }

        private int ClearAll()
        {
            var count = _subscriptions.Count;
            foreach (var subscription in _subscriptions)
                subscription.Value.Dispose();
            _subscriptions.Clear();
            return count;
        }

        public void Dispose()
        {
            ClearAll();
        }
    }
}