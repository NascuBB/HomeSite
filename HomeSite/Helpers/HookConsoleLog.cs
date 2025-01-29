using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using HomeSite.Managers;

namespace HomeSite.Helpers
{
    class HookConsoleLog
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetDlgItem(IntPtr hWnd, int nIDDlgItem);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, StringBuilder lParam);

        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const int PROCESS_VM_READ = 0x0010;
        private const uint WM_GETTEXT = 0x000D;

        //public static void Iniciate(int processId)
        //{

        //    // Попробуем найти окно по ID процесса
        //    IntPtr hConsoleWindow = GetConsoleWindowByProcessId(processId);
        //    if (hConsoleWindow == IntPtr.Zero)
        //    {
        //        //Console.WriteLine("Не удалось найти консольное окно для указанного процесса.");
        //        return;
        //    }

        //    while (true)
        //    {
        //        // Чтение содержимого консоли
        //        string consoleOutput = ReadConsoleOutput(hConsoleWindow);
        //        if (!string.IsNullOrEmpty(consoleOutput))
        //        {
        //            //Console.WriteLine("[Console Output]: " + consoleOutput);
        //            // Здесь можно отправить данные на сайт, например через WebSocket или HTTP-запрос
        //            OutputDataReceived(consoleOutput);
        //        }
        //        Thread.Sleep(500); // Задержка между чтениями
        //    }
        //}

        private static IntPtr GetConsoleWindowByProcessId(int processId)
        {
            IntPtr hwnd = IntPtr.Zero;
            Process process = Process.GetProcessById(processId);
            hwnd = process.MainWindowHandle;
            return hwnd;
        }

        private static string ReadConsoleOutput(IntPtr hWnd)
        {
            // Находим текстовое поле консоли
            IntPtr hEdit = GetDlgItem(hWnd, 0x3F);
            if (hEdit == IntPtr.Zero)
            {
                return null;
            }

            StringBuilder text = new StringBuilder(1024);
            SendMessage(hEdit, WM_GETTEXT, text.Capacity, text);
            return text.ToString();
        }
    }
}
