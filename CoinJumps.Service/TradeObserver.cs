using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CoinJumps.Service.Models;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quobject.SocketIoClientDotNet.Client;

namespace CoinJumps.Service
{
    public interface ITradeObserver
    {
        IObservable<SocketEvent> StatusStream { get; }
        SocketEvent Status { get; }
        IObservable<CoinCapTradeEvent> TradeStream { get; }
    }

    public class TradeObserver : ITradeObserver
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));

        public TradeObserver()
        {
            // Create the StatusStream observable (cold) as soon as someone subscribes to the StatusStream 
            StatusStream = Observable.Create<SocketEvent>(statusObserver =>
                {
                    Task.Delay(500).Wait();

                    var socket = IO.Socket("http://socket.coincap.io");

                    socket.On(Socket.EVENT_CONNECT, data => statusObserver.OnNext(new SocketEvent {Status = "Connected", Exception = data as Exception}));
                    socket.On(Socket.EVENT_CONNECT_ERROR, data => statusObserver.OnNext(new SocketEvent { Status = "Socket Connect Error", Exception = data as Exception }));
                    socket.On(Socket.EVENT_CONNECT_TIMEOUT, data => statusObserver.OnNext(new SocketEvent { Status = "Socket Connect Timeout", Exception = data as Exception }));
                    socket.On(Socket.EVENT_DISCONNECT, data => statusObserver.OnNext(new SocketEvent { Status = "Socket Disconnected", Exception = data as Exception }));
                    socket.On(Socket.EVENT_ERROR, data => statusObserver.OnNext(new SocketEvent { Status = "Socket Error", Exception = data as Exception }));
                    socket.On(Socket.EVENT_RECONNECT, data => statusObserver.OnNext(new SocketEvent { Status = "Socket Reconnected", Exception = data as Exception }));
                    socket.On(Socket.EVENT_RECONNECT_ATTEMPT, data => statusObserver.OnNext(new SocketEvent { Status = "Socket Reconnect Attempt", Exception = data as Exception }));
                    socket.On(Socket.EVENT_RECONNECTING, data => statusObserver.OnNext(new SocketEvent { Status = "Socket Reconnecting", Exception = data as Exception }));
                    socket.On(Socket.EVENT_RECONNECT_ERROR, data => statusObserver.OnNext(new SocketEvent { Status = "Socket Reconnect Error", Exception = data as Exception }));
                    socket.On(Socket.EVENT_RECONNECT_FAILED, data => statusObserver.OnNext(new SocketEvent { Status = "Socket Reconnect Failed", Exception = data as Exception }));
                    
                    socket.Open();

                    TradeStream = Observable.Create<CoinCapTradeEvent>(tradesObserver =>
                        {
                            socket.On("trades", data =>
                            {
                                var tradeObject = data as JObject;
                                if (tradeObject == null) return;
                                var message = tradeObject["message"] as JObject;
                                if (message == null) return;
                                var messageJson = message.ToString(Formatting.None);
                                var te = JsonConvert.DeserializeObject<CoinCapTradeEvent>(messageJson);
                                tradesObserver.OnNext(te);
                            });

                            return Disposable.Create(() =>
                            {
                                socket.Off("trades");
                            });

                        })
                        .Publish()
                        .RefCount();

                    return Disposable.Create(() =>
                    {
                        socket.Close();
                    });

                })
                .Do(e => Logger.Debug(e.Status, e.Exception))
                .Publish()
                .RefCount();

            StatusStream.Subscribe(ss => Status = ss);
        }

        public IObservable<SocketEvent> StatusStream { get; }

        public SocketEvent Status { get; private set; }

        public IObservable<CoinCapTradeEvent> TradeStream { get; private set; }
    }
}