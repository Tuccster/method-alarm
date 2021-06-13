using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TuccstersMethodAlarm
{
    class Program
    {
        public static bool debugMode = true;

        [STAThread]
        private static void Main(string[] args)
        {
            // Initialization
            try
            {
                if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                    Info.Write(typeof(Program), MessageType.Warning, "Running on untested OS which may cause crashes or unexpected behaviour!");

                // Config must be initialized first, as other classes read from the values
                // Yes, this isn't sustainable. I'm also the only person working on this.
                Config.Init();

                EditorManager.Init();
            }
            catch (Exception e)
            {
                Info.WriteException(e, debugMode);
            }

            // Continously get user input, parse it, execute it, and display what happened.
            while (true)
            {
                try
                {
                    NewUserPrompt();
                }
                catch (Exception e)
                {
                    Info.WriteException(e, debugMode);
                }
            }
        }

        public static void NewUserPrompt()
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("MethodAlarm> ");
            Console.ForegroundColor = ConsoleColor.White;
            CommandParser.Auto(Console.ReadLine());
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        [Command("debug", "Toggles Debug Mode, which shows the stack trace of thrown exceptions.")]
        public static void SetDebugMode()
        {
            debugMode = !debugMode;
            Console.WriteLine($"[DebugMode]: Debug mode now {(debugMode ? "on" : "off")}.");
        }

        [Command("exit", "Closes program.")]
        public static void Exit() => Environment.Exit(0);

        [Command("restart", "Replaces current instance of program with a new instance.")]
        public static void Restart() 
        {
            Process.Start(Config.exePath);
            Environment.Exit(0);
        }

        private static string OpenSelectFileDialog(string openTo = @"c:\", string title = "Choose file")
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = title;
            openFile.InitialDirectory = openTo;
            if (openFile.ShowDialog() == DialogResult.OK)
                return openFile.FileName;
            return string.Empty;
        }

        private static void WriteInfoAllProcesses()
        {
            Process[] processes = Process.GetProcesses();
            foreach (Process process in processes)
            {
                Console.WriteLine($"ProcessName => {process.ProcessName}");
                Console.WriteLine($"MachineName => {process.MachineName}");
                Console.WriteLine($"Id          => {process.Id}");
                Console.WriteLine($"------------------------------------");
            }
        }

        private static void OnApplicationExit(object sender, EventArgs e)
        {
            Console.Beep();
        }
    }
}
