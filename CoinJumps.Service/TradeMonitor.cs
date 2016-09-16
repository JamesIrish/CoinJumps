using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;
using Newtonsoft.Json;

namespace CoinJumps.Service
{
    public interface ITradeMonitor
    {
        void Monitor(string user, string coin, TimeSpan window, decimal? percentageThreshold);
        void Pause(string user);
        void Resume(string user);
        void Clear(string user, string coin = "");
        IEnumerable<CoinMonitor> List(string user);
        void Load();
        void Dispose();
    }

    public class TradeMonitor : IDisposable, ITradeMonitor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));

        private readonly Dictionary<string, CoinMonitor> _subscriptions;

        private readonly ITradeObserver _tradeObserver;
        private readonly ISlackMessenger _slackMessenger;

        private Lazy<string> ConfigurationsFile => new Lazy<string>(() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Program.ServiceName, "configurations.json"));

        public TradeMonitor(ITradeObserver tradeObserver, ISlackMessenger slackMessenger)
        {
            _tradeObserver = tradeObserver;
            _slackMessenger = slackMessenger;
            _subscriptions = new Dictionary<string, CoinMonitor>();
        }

        private string GenerateKey(string user, string coin, TimeSpan window)
        {
            return $"{user}:{coin}:{window.TotalSeconds}";
        }

        public IEnumerable<CoinMonitor> List(string user)
        {
            return _subscriptions.Values.Where(cm => cm.User.Equals(user)).ToList();
        }

        public void Monitor(string user, string coin, TimeSpan window, decimal? percentageThreshold)
        {
            lock (_subscriptions)
            {
                var key = GenerateKey(user, coin, window);
                if (_subscriptions.ContainsKey(key))
                {
                    _subscriptions[key].Dispose();
                    _subscriptions.Remove(key);
                }

                if (percentageThreshold.HasValue)
                {
                    var coinMonitor = new CoinMonitor
                    {
                        User = user,
                        Coin = coin,
                        Window = window,
                        PercentageThreshold = percentageThreshold.Value,
                        IsPaused = false
                    };
                    _subscriptions.Add(key, coinMonitor);
                    coinMonitor.Initialise(_tradeObserver, _slackMessenger);
                }
            }

            Save();
        }

        public void Pause(string user)
        {
            lock (_subscriptions)
                foreach (var cm in _subscriptions.Where(kvp => kvp.Value.User.Equals(user)).Select(kvp => kvp.Value))
                    cm.IsPaused = true;

            Save();
        }
        public void Resume(string user)
        {
            lock(_subscriptions)
                foreach (var cm in _subscriptions.Where(kvp => kvp.Value.User.Equals(user)).Select(kvp => kvp.Value))
                    cm.IsPaused = false;

            Save();
        }

        public void Clear(string user, string coin = "")
        {
            lock (_subscriptions)
            {
                var monitors = _subscriptions.Where(kvp => kvp.Value.User.Equals(user)).ToList();

                if (!string.IsNullOrWhiteSpace(coin))
                    monitors = monitors.Where(kvp => kvp.Value.Coin.Equals(coin)).ToList();

                foreach (var key in monitors.Select(kvp => kvp.Key))
                {
                    _subscriptions[key].Dispose();
                    _subscriptions.Remove(key);
                }
            }

            Save();
        }

        public void Load()
        {
            lock (_subscriptions)
            {
                var file = ConfigurationsFile.Value;

                // If the directory is missing create it
                var path = Path.GetDirectoryName(file);
                if (!string.IsNullOrWhiteSpace(path) && !Directory.Exists(path)) Directory.CreateDirectory(path); 

                using (var fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                using (var sr = new StreamReader(fs))
                {
                    var json = sr.ReadToEnd();
                    if (string.IsNullOrWhiteSpace(json)) return;
                    var coinMonitors = JsonConvert.DeserializeObject<IList<CoinMonitor>>(json);
                    foreach (var coinMonitor in coinMonitors)
                    {
                        var key = GenerateKey(coinMonitor.User, coinMonitor.Coin, coinMonitor.Window);
                        _subscriptions.Add(key, coinMonitor);

                        coinMonitor.Initialise(_tradeObserver, _slackMessenger);
                    }
                }
            }
        }

        private void Save()
        {
            lock (_subscriptions)
            {
                using (var fs = new FileStream(ConfigurationsFile.Value, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                using (var sw = new StreamWriter(fs))
                {
                    var json = JsonConvert.SerializeObject(_subscriptions.Select(kvp => kvp.Value).ToList());
                    sw.Write(json);
                    sw.Flush();
                }
            }
        }
        
        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Value.Dispose();
            _subscriptions.Clear();
        }
    }
}