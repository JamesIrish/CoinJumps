using System;
using System.Reactive.Linq;
using CoinJumps.Service.Models;
using Humanizer;
using log4net;
using Slack.Webhooks;

namespace CoinJumps.Service
{
    public class CoinMonitor : IDisposable
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CoinMonitor));

        private ISlackMessenger _slackMessenger;
        private IDisposable _subscription;

        public string User { get; set; }
        public string Coin { get; set; }
        public TimeSpan Window { get; set; }
        public decimal PercentageThreshold { get; set; }
        public bool IsPaused { get; set; }

        public void Initialise(ITradeObserver tradeObserver, ISlackMessenger slackMessenger)
        {
            _slackMessenger = slackMessenger;

            // Get the first price update  (no need to dispose as First() completes automatically)
            tradeObserver.TradeStream
                .Where(t => t.Coin.Equals(Coin))
                .FirstAsync()
                .Subscribe(first =>
                {
                    // Note the price
                    LastPrice = new PricePoint { Coin = first.Coin, Price = first.Msg.Price };

                    // Subscribe to the relevant window
                    _subscription = tradeObserver.TradeStream
                        .Where(t => t.Coin.Equals(Coin))
                        .Sample(Window)
                        .Subscribe(Compare);
                });
        }

        private PricePoint LastPrice { get; set; }

        private void Compare(CoinCapTradeEvent tradeEvent)
        {
            var last = LastPrice.Price;
            var curr = tradeEvent.Msg.Price;
            var move = (curr/last) - 1m;
            var perc = move*100;
            var mesg = $"{tradeEvent.Msg.Long} moved by {perc:N2}% to ${tradeEvent.Msg.Price:N4} over {Window.Humanize()}";
            if (perc >= PercentageThreshold || perc < -PercentageThreshold)
            {
                if (!IsPaused)
                {
                    var alertsChannel = $"#alerts-{User}";
                    if (alertsChannel.Length > 22) alertsChannel = alertsChannel.Substring(0, 22);
                    var sm = new SlackMessage {Channel = alertsChannel, Text = mesg, Mrkdwn = false, Username = "CoinJumps"};
                    _slackMessenger.Post(sm);
                }
                Logger.Warn(mesg);
            }
            else
                Logger.Debug(mesg);

            LastPrice = new PricePoint {Coin = Coin, Price = curr};
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }
    }
}