using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuaSettings.LuaExtensionPackages
{
    public static class ConsolePackage
    {
        public static void log(string text)
        {
	        WriteAndResetColor(text, ConsoleColor.Green);
        }

        public static void debug(string text)
        {
	        WriteAndResetColor(text, ConsoleColor.DarkCyan);
        }

        public static void info(string text)
        {
	        WriteAndResetColor(text, ConsoleColor.Cyan);
        }

        public static void warning(string text)
        {
	        WriteAndResetColor(text, ConsoleColor.Yellow);
        }

        public static void error(string text)
        {
	        WriteAndResetColor(text, ConsoleColor.Red);
        }

        public static void success(string text)
        {
	        WriteAndResetColor(text, ConsoleColor.Green);
        }

        private static void WriteAndResetColor(string text, ConsoleColor newColor)
        {
	        var prevColor = Console.ForegroundColor;
	        Console.ForegroundColor = newColor;
            Console.WriteLine(text);
            Console.ForegroundColor = prevColor;
        }
    }
}
