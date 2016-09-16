using System.Configuration;
using Slack.Webhooks;

namespace CoinJumps.Service
{
    public interface ISlackMessenger
    {
        void Post(SlackMessage message);
    }

    public class SlackMessenger : ISlackMessenger
    {
        private readonly SlackClient _slackClient;

        public SlackMessenger()
        {
            var slackWebhookUrl = ConfigurationManager.AppSettings["SlackWebhookUrl"];
            _slackClient = new SlackClient(slackWebhookUrl);
        }

        public void Post(SlackMessage message)
        {
            _slackClient.Post(message);
        }
    }
}