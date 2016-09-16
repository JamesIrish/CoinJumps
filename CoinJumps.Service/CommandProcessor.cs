using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CoinJumps.Service.Utils;
using Humanizer;
using log4net;
using Slack.Webhooks;

namespace CoinJumps.Service
{
    public interface ICommandProcessor
    {
        SlackMessage ProcessCommandText(string user, string text);
    }

    public class CommandProcessor : ICommandProcessor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));

        public const string Prefix = "CJ ";

        private readonly ITradeObserver _tradeObserver;
        private readonly ITradeMonitor _tradeMonitor;

        public CommandProcessor(ITradeObserver tradeObserver, ITradeMonitor tradeMonitor)
        {
            _tradeObserver = tradeObserver;
            _tradeMonitor = tradeMonitor;
        }

        public SlackMessage ProcessCommandText(string user, string text)
        {
            Logger.DebugFormat("{0} sent: {1}", user, text);

            // Strip CJ if it exists
            var command = ReplaceString(text, Prefix, "", StringComparison.OrdinalIgnoreCase).Trim();

            // Split by (space)
            var parts = command.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

            // Part 1 is the command name
            var cmdStr = parts[0];
            CjCommands cmd;
            if (!Enum.TryParse(cmdStr, true, out cmd))
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
                                new SlackField {Title = "Format", Value = "CJ Monitor [CCY] [TimeSpan] [%Threshold]"},
                                new SlackField {Title = "Example", Value = "CJ Monitor XMR 1m 0.5"}
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

                    if (pt.Equals("x", StringComparison.OrdinalIgnoreCase))
                    {
                        _tradeMonitor.Monitor(user, coin, window, null);
                        return new SlackMessage
                        {
                            Text = $"Monitoring disabled for {coin} change over {window.Humanize()}"
                        };
                    }

                    decimal percentageThreshold;
                    if (!decimal.TryParse(pt, out percentageThreshold))
                        return new SlackMessage {Text = $"Could not parse {pt} to Decimal", Attachments  = new List<SlackAttachment> {new SlackAttachment {Fields = new List<SlackField>
                            {
                                new SlackField {Title = "Expected", Value = "0.5 (i.e. half a percent)"}
                            },
                            Color = "danger"
                        }}};

                    _tradeMonitor.Monitor(user, coin, window, percentageThreshold);

                    return new SlackMessage {Text = "Monitoring configured", IconEmoji = ":bell:", Attachments = new List<SlackAttachment> {new SlackAttachment {Fields = new List<SlackField>
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

                    _tradeMonitor.Clear(user, coin);

                    return new SlackMessage
                    {
                        Text = string.IsNullOrWhiteSpace(coin) ? "Monitoring cleared" : $"{coin} monitoring cleared", IconEmoji = ":no_bell:"
                    };

                case CjCommands.List:

                    var attachments = _tradeMonitor.List(user).Select(m => new SlackAttachment
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
                        IconEmoji = attachments.Count == 0 ? ":thinking_face:" : ":+1:",
                        Attachments = attachments
                    };

                case CjCommands.Restart:

                    if (Environment.UserInteractive)
                        return new SlackMessage {Text = "Not currently running as a Windows Service - command N/A."};

                    var psi = new ProcessStartInfo
                    {
                        CreateNoWindow = true,
                        FileName = "cmd.exe",
                        Arguments = $"/C net stop {Program.ServiceName} && net start {Program.ServiceName}",
                        LoadUserProfile = false,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    var proc = new Process {StartInfo = psi};
                    proc.Start();

                    return new SlackMessage {Text = "Restarting..."};

                case CjCommands.Pause:
                    _tradeMonitor.Pause(user);
                    return new SlackMessage {Text = "Your notifications are now paused"};
                case CjCommands.Resume:
                    _tradeMonitor.Resume(user);
                    return new SlackMessage {Text = "Notifications resumed"};

                default:
                    return new SlackMessage {Text = $"{cmd} is not currently implemented :cry:"};
            }
        }

        private static string ReplaceString(string str, string oldValue, string newValue, StringComparison comparison)
        {
            var sb = new StringBuilder();

            var previousIndex = 0;
            var index = str.IndexOf(oldValue, comparison);
            while (index != -1)
            {
                sb.Append(str.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                index += oldValue.Length;

                previousIndex = index;
                index = str.IndexOf(oldValue, index, comparison);
            }
            sb.Append(str.Substring(previousIndex));

            return sb.ToString();
        }
    }
}