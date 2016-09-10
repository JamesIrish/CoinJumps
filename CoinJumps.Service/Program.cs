using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using Nancy.Hosting.Self;
using Quobject.SocketIoClientDotNet.Client;
using ServiceStack.Text;
using StructureMap;

namespace CoinJumps.Service
{
    public class Program
    {
        public const string ServiceName = "CoinJumps.Service";

        #region Nested classes to support running as service

        public class Service : ServiceBase
        {
            private readonly Program _instance;

            public Service()
            {
                ServiceName = ServiceName;
                _instance = new Program();
            }

            protected override void OnStart(string[] args)
            {
                _instance.Start(args);
            }

            protected override void OnStop()
            {
                _instance.Stop();
            }
        }

        #endregion

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main(string[] args)
        {
            if (!Environment.UserInteractive)
                using (var service = new Service())
                    ServiceBase.Run(service);
            else
            {
                var p = new Program();
                Console.Title = ServiceName;
                p.Start(args);
                Console.WriteLine("Press any key to stop...");
                Console.WriteLine();
                Console.ReadKey(true);
                p.Stop();
            }
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));

        private static IContainer _container;
        private NancyHost _webHost;
        private CompositeDisposable _subscriptions;

        public static IContainer Container => _container;

        private void Start(string[] args)
        {
            XmlConfigurator.Configure();
            Logger.Debug($"{ServiceName} starting");

            // Cookup StructureMap
            _container = new Container(new SmRegistry());

            // Create a comp disposer to ensure we cleanup any subscriptions to the TradeStream observer
            _subscriptions = new CompositeDisposable();

            // New instance of the TradeStream observer (note it's 'cold' so it does nothing at this point)
            var tradeObserver = new TradeObserver();

            // Configure Nancy
            JsConfig.EmitLowercaseUnderscoreNames = true;
            JsConfig.IncludeNullValues = false;
            JsConfig.PropertyConvention = JsonPropertyConvention.Lenient;

            // Start listening (you'll need to open the relevant port to the big bad internet for this to work)
            // http://nerdfury.redrabbits.co.uk/2015/04/07/slack-outgoing-webhooks/
            // ************************************************************************************************
            var port = ConfigurationManager.AppSettings["NancyHostPort"];
            var url = $"http://localhost:{port}";
            _webHost = new NancyHost(new Uri(url), new Bootstrapper(), new HostConfiguration
            {
                RewriteLocalhost = true,
                UrlReservations = new UrlReservations
                {
                    CreateAutomatically = true
                },
                UnhandledExceptionCallback = ex => Logger.Error("Nancy Error", ex)
            });
            _webHost.Start();

            Logger.Info($"{ServiceName} running.  Listening on port {port}.");
            
            //var moneroWindow = new MonitorWindow
            //{
            //    Coin = "XMR",
            //    WindowDuration = TimeSpan.FromMinutes(1),
            //    PercentThreshold = 0.5m
            //};

            

            //_subscriptions.Add(tradeObserver.TradeStream.Buffer(TimeSpan.FromSeconds(1)).Subscribe(trades => Logger.DebugFormat("{0:N0} trades/sec", trades.Count)));
            //_subscriptions.Add(tradeObserver.TradeStream.Buffer(TimeSpan.FromSeconds(60)).Subscribe(trades => Logger.DebugFormat("{0:N0} trades/min", trades.Count)));

            //var xmr = tradeObserver.TradeStream.Where(t => t.Coin == moneroWindow.Coin);

            //PricePoint lastXmr = null;

            //_subscriptions.Add(xmr.Sample(moneroWindow.WindowDuration).Subscribe(t =>
            //{
            //    if (lastXmr == null)
            //        lastXmr = new PricePoint {Coin = t.Coin, Price = t.Msg.Price};
            //    else
            //    {
            //        var last = lastXmr.Price;
            //        var curr = t.Msg.Price;
            //        var move = (curr/last) - 1m;
            //        var perc = move*100;
            //        var mesg = $"{t.Msg.Long} moved by {perc:N2}% to {t.Msg.Price:N6}";
            //        if (perc > moneroWindow.PercentThreshold || perc < -moneroWindow.PercentThreshold)
            //            Logger.Warn(mesg);
            //        else
            //            Logger.Debug(mesg);
            //    }
            //}));
        }

        private void Stop()
        {
            // Close the Nancy web host
            _webHost.Dispose();

            // Clean up subscriptions
            _subscriptions.Dispose();

            Logger.Debug($"{ServiceName} stopped");
        }
    }
}
