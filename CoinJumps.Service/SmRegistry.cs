using CoinJumps.Service.Utils;
using Nancy.ModelBinding;
using StructureMap;

namespace CoinJumps.Service
{
    public class SmRegistry : Registry
    {
        public SmRegistry()
        {
            For<ICommandProcessor>().Use<CommandProcessor>();
            For<ITradeObserver>().Singleton().Use<TradeObserver>();
            For<ITradeMonitor>().Singleton().Use<TradeMonitor>();
            For<ISlackMessenger>().Singleton().Use<SlackMessenger>();
        }
    }
}