using log4net;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.StructureMap;
using Nancy.Diagnostics;
using StructureMap;

namespace CoinJumps.Service
{
    public class Bootstrapper : StructureMapNancyBootstrapper
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Bootstrapper));

        protected override void ConfigureApplicationContainer(IContainer existingContainer)
        {
            // Perform registration that should have an application lifetime
            base.ConfigureApplicationContainer(existingContainer);
        }

        protected override void ApplicationStartup(IContainer container, IPipelines pipelines)
        {
            // No registrations should be performed in here, however you may
            // resolve things that are needed during application startup.
            base.ApplicationStartup(container, pipelines);

            pipelines.OnError.AddItemToEndOfPipeline((context, exception) =>
            {
                Logger.Error("Unhandled error", exception);
                return null;
            });

            StaticConfiguration.EnableRequestTracing = true;
        }
        
        protected override void ConfigureRequestContainer(IContainer container, NancyContext context)
        {
            // Perform registrations that should have a request lifetime
            base.ConfigureRequestContainer(container, context);
        }

        protected override void RequestStartup(IContainer container, IPipelines pipelines, NancyContext context)
        {
            // No registrations should be performed in here, however you may
            // resolve things that are needed during request startup.
            base.RequestStartup(container, pipelines, context);

            pipelines.OnError.AddItemToEndOfPipeline((nancyContext, exception) =>
            {
                Logger.Error("Unhandled error", exception);
                return null;
            });
        }

        protected override IContainer GetApplicationContainer()
        {
            return Program.Container;
        }

        protected override DiagnosticsConfiguration DiagnosticsConfiguration
        {
            get { return new DiagnosticsConfiguration { Password = @"guest" }; }
        }
    }
}