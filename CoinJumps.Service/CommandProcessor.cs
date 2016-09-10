using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CoinJumps.Service.Utils;
using Humanizer;
using Slack.Webhooks;

namespace CoinJumps.Service
{
    public interface ICommandProcessor
    {
        SlackMessage ProcessCommandText(string text);
    }

    public class CommandProcessor : ICommandProcessor
    {
        public const string Prefix = "CJ";

        private readonly ITradeObserver _tradeObserver;
        private readonly ITradeMonitor _tradeMonitor;

        public CommandProcessor(ITradeObserver tradeObserver, ITradeMonitor tradeMonitor)
        {
            _tradeObserver = tradeObserver;
            _tradeMonitor = tradeMonitor;
        }

        public SlackMessage ProcessCommandText(string text)
        {
            // Strip CJ: if it exists
            var command = text.Replace(Prefix, "").Trim();

            // Split by :
            var parts = command.Split(new[] {'~'}, StringSplitOptions.RemoveEmptyEntries);

            // Part 1 is the command name
            var cmdStr = parts[0];
            CjCommands cmd;
            if (!Enum.TryParse(cmdStr, out cmd))
                return new SlackMessage
                {
                    Text = "Unknown command, expected one of..",
                    Attachments = new List<SlackAttachment>
                    {
                        new SlackAttachment
                        {
                            Fields = Enum.GetNames(typeof(CjCommands)).Select(n => new SlackField {Title = n}).ToList(),
                            Color = "warning"
                        }
                    }
                };

            string coin;

            // Switch on the command
            switch (cmd)
            {
                case CjCommands.Status:
                    return new SlackMessage
                    {
                        Text = $"Current status",
                        Attachments = new List<SlackAttachment>
                        {
                            new SlackAttachment
                            {
                                Fields = new List<SlackField>
                                {
                                    new SlackField {Title = "CoinCap Socket", Value = _tradeObserver.Status.Status, Short = true},
                                    new SlackField {Title = "Memory Usage", Value = Process.GetCurrentProcess().PrivateMemorySize64.Bytes().Humanize("#.#"), Short = true}
                                },
                                Color = "#439FE0"
                            }
                        }
                    };
                case CjCommands.Monitor:
                    if (parts.Length != 4)
                        return new SlackMessage {Text = "Incorrect format for Monitor command", Attachments = new List<SlackAttachment> {new SlackAttachment {Fields = new List<SlackField>{ 
                                new SlackField {Title = "Format", Value = "CJ~Monitor~[CCY]~[TimeSpan]~[%Threshold]"},
                                new SlackField {Title = "Example", Value = "CJ~Monitor~XMR~1m~0.5"}
                            },
                            Color = "danger"
                        }}};

                    coin = parts[1].ToUpper();
                    var ts = parts[2];
                    var pt = parts[3];

                    TimeSpan window;
                    if (!ts.ToTimeSpan(out window))
                        return new SlackMessage {Text = $"Could not parse {ts} to TimeSpan", Attachments  = new List<SlackAttachment> {new SlackAttachment {Fields = new List<SlackField>
                            {
                                new SlackField {Title = "Examples", Value = "1d, 2h, 5m, 60s"}
                            },
                            Color = "danger"
                        }}};

                    decimal percentageThreshold;
                    if (!decimal.TryParse(pt, out percentageThreshold))
                        return new SlackMessage {Text = $"Could not parse {pt} to Decimal", Attachments  = new List<SlackAttachment> {new SlackAttachment {Fields = new List<SlackField>
                            {
                                new SlackField {Title = "Expected", Value = "0.5 (i.e. half a percent)"}
                            },
                            Color = "danger"
                        }}};

                    _tradeMonitor.Monitor(coin, window, percentageThreshold);

                    return new SlackMessage {Text = "Monitoring configured", Attachments = new List<SlackAttachment> {new SlackAttachment {Fields = new List<SlackField>
                        {
                            new SlackField {Title = "Coin", Value = coin, Short = true},
                            new SlackField {Title = "Window", Value = $"{percentageThreshold:N1}% change after {window.Humanize()}", Short = true}
                        },
                        Color = "good"
                    }}};
                case CjCommands.Clear:
                    coin = string.Empty;

                    if (parts.Length > 1)
                        coin = parts[1].ToUpper();

                    _tradeMonitor.Clear(coin);

                    return new SlackMessage
                    {
                        Text = string.IsNullOrWhiteSpace(coin) ? "Monitoring cleared" : $"{coin} monitoring cleared"
                    };
                case CjCommands.List:
                    var attachments = _tradeMonitor.List().Select(m => new SlackAttachment
                    {
                        Color = "good",
                        Fields = new List<SlackField>
                        {
                            new SlackField {Title = "Coin", Value = m.Coin, Short = true},
                            new SlackField {Title = "Window", Value = $"{m.PercentageThreshold:N1}% change after {m.Window.Humanize()}", Short = true}
                        }
                    }).ToList();
                    return new SlackMessage
                    {
                        Text = attachments.Count == 0 ? "Nothing configured!  Start by issuing a 'Monitor' command." : "Configured coin monitors:",
                        Attachments = attachments
                    };
                default:
                    return new SlackMessage {Text = $"{cmd} is not currently implemented :cry:"};
            }
        }
    }
}