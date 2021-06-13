using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TuccstersMethodAlarm
{
    public class WindowDocker
    {
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, int wFlags);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, ref Rect rectangle);

        [DllImport("User32")]
        private static extern int ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("kernel32.dll")]
        static extern int GetProcessId(IntPtr handle);

        public struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        private WindowDocker threadedInstance;
        private Thread windowDockThread;

        public void StartWindowDock(IntPtr windowHandle)
        {
            Rect windowRect = new Rect();
            GetWindowRect(windowHandle, ref windowRect);
            Console.WriteLine($"windowRect >> L{windowRect.Left} R{windowRect.Right} T{windowRect.Top} B{windowRect.Bottom}");

            threadedInstance = new WindowDocker();
            windowDockThread = new Thread(threadedInstance.UpdateDockedWindowPosition);
            windowDockThread.Start(windowHandle);

            Info.Write(GetType(), MessageType.Status, "Lanching windowDockThread...");
        }

        //
        // Docks a window to the console
        //
        private void UpdateDockedWindowPosition(object windowHandleObject)
        {
            IntPtr windowHandle = (IntPtr)windowHandleObject;
            IntPtr consoleHandle = Process.GetCurrentProcess().MainWindowHandle;
            bool moveContinuous = true;

            if (windowHandle == null)
                throw new Exception("TEST");

            Console.WriteLine($"0x{windowHandle:X}");

            //Console.WriteLine(GetProcessId(windowHandle));
            //Console.WriteLine(FindProcess(windowHandle).ProcessName);

            ShowWindow(windowHandle, 5);

            int consolePosX = 0;
            int consolePosY = 0;

            while (true)
            {
                Thread.Sleep(10);

                Rect consoleRect = new Rect();
                Rect windowRect = new Rect();
                GetWindowRect(consoleHandle, ref consoleRect);
                GetWindowRect(windowHandle, ref windowRect);
                //Console.WriteLine($"consoleRext >> L{consoleRect.Left} R{consoleRect.Right} T{consoleRect.Top} B{consoleRect.Bottom}");
                //Console.WriteLine($"windowRect >> L{windowRect.Left} R{windowRect.Right} T{windowRect.Top} B{windowRect.Bottom}");

                //MoveWindow(windowHandle, consoleRect.Left, consoleRect.Top - (windowRect.Bottom - windowRect.Top), consoleRect.Right - consoleRect.Left, 256, true);
                //if (moveContinuous || (consoleRect.Left == consolePosX && consoleRect.Top == consolePosY))
                //{
                //    MoveWindow(windowHandle, consoleRect.Left, consoleRect.Top - (windowRect.Bottom - windowRect.Top), consoleRect.Right - consoleRect.Left, 256, true);
                //    //MoveWindow(consoleHandle, aRect.Left, aRect.Top + (aRect.Bottom - aRect.Top), aRect.Right - aRect.Left, 640, true);
                //}

                consolePosX = consoleRect.Left;
                consolePosY = consoleRect.Top;
            }
        }

        public static Process FindProcess(IntPtr yourHandle)
        {
            foreach (Process p in Process.GetProcesses())
            {
                if (p.Handle == yourHandle)
                {
                    return p;
                }
            }

            return null;
        }
    }
}
