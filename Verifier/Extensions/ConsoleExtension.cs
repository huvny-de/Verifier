using System;

namespace Verifier.Extensions
{
    public static class ConsoleExtension
    {
        public static void LogWithColor(this string value, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(value);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void LogRunTime(this DateTime startTime)
        {
            var runTimes = DateTime.Now.Subtract(startTime);
            $"Total Run Time: {runTimes.Days}d {runTimes.Hours}hrs {runTimes.Minutes}m {runTimes.Seconds}s".LogWithColor(ConsoleColor.DarkGreen);
        }
    }
}
