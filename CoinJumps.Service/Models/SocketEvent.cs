using System;

namespace CoinJumps.Service.Models
{
    public class SocketEvent
    {
        public SocketEvent()
        {
            TimeStamp = DateTimeOffset.UtcNow;
        }

        public DateTimeOffset TimeStamp { get; }

        public string Status { get; set; }

        public Exception Exception { get; set; }
    }
}