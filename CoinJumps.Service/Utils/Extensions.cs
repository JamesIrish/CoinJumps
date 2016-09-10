using System;

namespace CoinJumps.Service.Utils
{
    public static class Extensions
    {
        public static bool ToTimeSpan(this string text, out TimeSpan timeSpan)
        {
            timeSpan = TimeSpan.Zero;

            try
            {
                var l = text.Length - 1;
                var value = text.Substring(0, l);
                var type = text.Substring(l, 1);

                switch (type)
                {
                    case "d":
                        timeSpan = TimeSpan.FromDays(double.Parse(value));
                        return true;
                    case "h":
                        timeSpan = TimeSpan.FromHours(double.Parse(value));
                        return true;
                    case "m":
                        timeSpan = TimeSpan.FromMinutes(double.Parse(value));
                        return true;
                    case "s":
                        timeSpan = TimeSpan.FromSeconds(double.Parse(value));
                        return true;
                    case "f":
                        timeSpan = TimeSpan.FromMilliseconds(double.Parse(value));
                        return true;
                    case "z":
                        timeSpan = TimeSpan.FromTicks(long.Parse(value));
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}