using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace TuccstersMethodAlarm
{
    public static class EditorManager
    {
        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, ref Rect rectangle);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, int wFlags);

        private static string idePath = string.Empty;
        private static Process editorProcess = null;
        private static Thread updateDockWindowPosition;

        public static void Init()
        {
            idePath = Config.UserDefined.EditorPath;
        }

        [Command("setide", "Opens File Explorer for choosing a program to edit C# files with")]
        public static void SetEditor()
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Select IDE program";
            openFile.InitialDirectory = @"C:\Program Files (x86)";
            if (openFile.ShowDialog() == DialogResult.OK)
            {
                if (Path.GetExtension(openFile.FileName) != ".exe")
                {
                    Console.WriteLine($"IDE must have be an executable file (.exe).");
                    return;
                }

                Console.WriteLine($"IDE set to '{Path.GetFileName(openFile.FileName)}'");
                idePath = openFile.FileName;
                
                // Update the config with the new ide path
                Config.UserDefined.EditorPath = openFile.FileName;
                Config.UpdateFile();
            }
        }

        [Command("openide")]
        public static void TESTOPENIDE()
        {
            OpenFileInIDE(@"C:\Users\coles\source\repos\TuccstersMethodAlarm\TuccstersMethodAlarm\bin\Debug\Problems\addition.cs");
        }

        public struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        public static void OpenFileInIDE(string path)
        {
            if (editorProcess != null)
            {
                Info.Write(typeof(EditorManager), MessageType.Warning, "Editor already open");
                return;
            }

            string ideName = Path.GetFileNameWithoutExtension(idePath);
            string launchArgs = string.Empty;

            // Set launch args based on program selected
            switch (ideName)
            {
                case "notepad++": launchArgs = $"-l cs {path}"; break;
                default: launchArgs = string.Empty; break;
            }

            if (ideName == string.Empty)
            {
                throw new EditorNotSetException("Use 'setide' to set an editor");
            }

            Info.Write(typeof(EditorManager), MessageType.Status, $"Launching {Path.GetFileName(idePath)} with file '{Path.GetFileName(path)}'...");
            editorProcess = Process.Start(idePath, launchArgs);

            // We need to wait for the main window of our editor to opened before it's assigned a window handle
            for (int i = 0; i < 50; i++)
            {
                Thread.Sleep(100);
                if (editorProcess.MainWindowHandle.ToInt32() == 0)
                    continue;

                updateDockWindowPosition = new Thread(UpdateDockedWindowPosition);
                updateDockWindowPosition.Start(editorProcess);

                editorProcess.EnableRaisingEvents = true;
                editorProcess.Exited += (sender, e) => {
                    editorProcess = null;
                    updateDockWindowPosition.Abort();
                    Info.Write(typeof(EditorManager), MessageType.Status, "updateDockWindowPosition thread terminated.", true);
                };

                return;
            }

            throw new FailedWindowDockException("Unable to start dock editor to console.");
        }

        private static void UpdateDockedWindowPosition(object editorProcessObject)
        {
            Process editorProcess = (Process)editorProcessObject;
            IntPtr editorMainWindowHandle = editorProcess.MainWindowHandle;
            IntPtr consoleHandle = Process.GetCurrentProcess().MainWindowHandle;
            bool moveContinuous = true;

            //Info.Write(typeof(EditorManager), MessageType.Status, $"Aquired editor main window handle at 0x{windowHandle:X}");
            //Info.Write(typeof(EditorManager), MessageType.Status, $"Aquired console main window handle at 0x{consoleHandle:X}");

            //Console.WriteLine(GetProcessId(windowHandle));
            //Console.WriteLine(FindProcess(windowHandle).ProcessName);

            //ShowWindow(windowHandle, 5);

            int consolePosX = 0;
            int consolePosY = 0;

            while (true)
            {
                Thread.Sleep(10);

                Rect consoleRect = new Rect();
                Rect windowRect = new Rect();
                GetWindowRect(consoleHandle, ref consoleRect);
                GetWindowRect(editorMainWindowHandle, ref windowRect);
                //Console.WriteLine($"consoleRext >> L{consoleRect.Left} R{consoleRect.Right} T{consoleRect.Top} B{consoleRect.Bottom}");
                //Console.WriteLine($"windowRect >> L{windowRect.Left} R{windowRect.Right} T{windowRect.Top} B{windowRect.Bottom}");

                //MoveWindow(editorMainWindowHandle, consoleRect.Left, consoleRect.Top - (windowRect.Bottom - windowRect.Top), consoleRect.Right - consoleRect.Left, 256, true);
                if (moveContinuous || (consoleRect.Left == consolePosX && consoleRect.Top == consolePosY))
                {
                    MoveWindow(editorMainWindowHandle, consoleRect.Left, consoleRect.Top - (windowRect.Bottom - windowRect.Top), consoleRect.Right - consoleRect.Left, windowRect.Bottom - windowRect.Top, true);
                    //MoveWindow(consoleHandle, aRect.Left, aRect.Top + (aRect.Bottom - aRect.Top), aRect.Right - aRect.Left, 640, true);
                }

                consolePosX = consoleRect.Left;
                consolePosY = consoleRect.Top;
            }
        }
    }
}
