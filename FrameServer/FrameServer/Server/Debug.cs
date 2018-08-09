using System;
using System.Collections.Generic;

namespace FrameServer
{
    public static class Debug
    {
        public static bool ENABLE_ERROR = false;
        public static bool ENABLE_LOG = false;
        public static bool ENABLE_WARNING = false;

        public static void Log(object message, ConsoleColor color = ConsoleColor.White)
        {
            ConsoleColor origin = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = origin;
        }

        public static void Error(object message)
        {
            if (ENABLE_ERROR)
            {
                Log(message, ConsoleColor.Red);
            }
        }

        public static void Warning(object message)
        {
            if(ENABLE_WARNING)
            {
                Log(message, ConsoleColor.Yellow);
            }
        }
    }
}
