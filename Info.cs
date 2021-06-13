using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace TuccstersMethodAlarm
{
    public enum MessageType { Generic, Warning, Error, Status };

    // This class is a for displaying data to the console in a user-friendly way.
    public static class Info
    {
        private static ConsoleColor[] messageTypeColor = { ConsoleColor.White, ConsoleColor.Yellow, ConsoleColor.Red, ConsoleColor.DarkCyan };

        /// <summary>
        /// Write a message to the console, showing what class called it and what kind of message it is.
        /// </summary>
        public static void Write(Type writer, MessageType messageType, string message, bool forceNewUserPrompt = false)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(forceNewUserPrompt ? "<forced_new_user_prompt>\n" : string.Empty);
            Console.ForegroundColor = messageTypeColor[(int)messageType];
            Console.Write($"[{writer.Name}]");
            Console.Write($"[{messageType}]: ");
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;

            if (forceNewUserPrompt)
                Program.NewUserPrompt();
        }

        /// <summary>
        /// Writes a formatted Exception to the console.
        /// In order for a complete call stack to be created through a MethodBase.Invoke() call
        /// 'Debug > Options > General > Enable Just My Code' must be turned off.
        /// </summary>
        public static void WriteException(Exception e, bool writeStackTrace)
        {
            Console.ForegroundColor = ConsoleColor.Red;

            if (writeStackTrace)
            {
                if (e.GetType() == typeof(TargetInvocationException))
                    Console.WriteLine($"[StackTrace]: {e.InnerException.StackTrace}");
                else
                    Console.WriteLine($"[StackTrace]: {e.StackTrace}");
            }

            // TargetInvocationException is thrown when a method called through Invoke() throws an exception.
            // Instead of that, we want the InnerException because it tells us what happened in the invoked method.
            if (e.GetType() == typeof(TargetInvocationException))
            {
                Console.Write($"[{e.InnerException.GetType().Name}]: ");
                Console.WriteLine(e.InnerException.Message);
            }
            else
            {
                Console.Write($"[{e.GetType().Name}]: ");
                Console.WriteLine(e.Message);
            }

            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
