using System;
using CoinJumps.Service.Models;
using log4net;
using Nancy;
using Nancy.ModelBinding;

namespace CoinJumps.Service
{
    public class SlackWebhookModule : NancyModule
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SlackWebhookModule));

        public SlackWebhookModule(ICommandProcessor commandProcessor)
        {
            Get["/"] = _ =>
            {
                return "Hello";
            };
            Post["/"] = _ =>
            {
                try
                {
                    var model = this.Bind<SlackHookMessage>();
                    if (model.Text.ToUpper().StartsWith(CommandProcessor.Prefix))
                        return commandProcessor.ProcessCommandText(model.UserName, model.Text);

                    return null;
                }
                catch (Exception ex)
                {
                    Logger.Error("Error processing Slack message", ex);
                    return null;
                }
            };
        }
    }
}