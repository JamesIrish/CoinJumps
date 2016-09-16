using System;
using System.Configuration;
using System.ServiceProcess;
using log4net;
using log4net.Config;
using Nancy.Hosting.Self;
using ServiceStack.Text;
using Slack.Webhooks;
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

        public static IContainer Container => _container;

        private void Start(string[] args)
        {
            XmlConfigurator.Configure();
            Logger.Debug($"{ServiceName} starting");

            // Cookup StructureMap
            _container = new Container(new SmRegistry());

            // New instance of the TradeStream observer (note it's 'cold' so it does nothing at this point)
            // ReSharper disable once UnusedVariable
            var tradeObserver = _container.GetInstance<ITradeObserver>();

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
            
            // Load existing monitor configurations
            _container.GetInstance<ITradeMonitor>().Load();

            Logger.Info($"{ServiceName} running.  Listening on port {port}.");
            Container.GetInstance<ISlackMessenger>().Post(new SlackMessage {Username = "Service", Text = "Service running"});
        }

        private void Stop()
        {
            Container.GetInstance<ISlackMessenger>().Post(new SlackMessage {Username = "Service", Text = "Service stopped"});

            // Close the Nancy web host
            _webHost.Dispose();

            Logger.Debug($"{ServiceName} stopped");
        }
    }
}
