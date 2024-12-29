using CoreRCON;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace HomeSite.Helpers
{
    public class MinecraftServerManager
    {
        private static MinecraftServerManager? _instance;
        //public bool IsRunning { get { return ServerConsoleProcess != null; } }
        public bool IsRunning { 
            get
            {
                return ServerConsoleProcess != null;
            }
        }
        private const string logPath = @"C:\Users\nonam\AppData\Roaming\.minecraft\logs\latest.log";
        private const string tempLogPath = @"C:\Users\nonam\AppData\Roaming\.minecraft\logs\temp.log";
        private RCON rcon;
        public Process? ServerConsoleProcess { get; private set; }
        public Process? ServerProcess { get; private set; }
        private MinecraftServerManager() { }
        public string ConsoleLogs { get { return consoleLogs; } }
        private string consoleLogs = "Логи сервера появятся здесь...";

        public static MinecraftServerManager GetInstance()
        {
            _instance ??= new();
            return _instance;
        }
        public async Task StopServer()
        {
            try
            {
                MinecraftServerManager.GetInstance().SendCommand("stop");
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        public async Task LaunchServer()
        {
            if(ServerConsoleProcess != null)
            {
                throw new Exception("Сервер уже запущен");
            }
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"C:\Users\nonam\AppData\Roaming\.minecraft\run.bat",
                    //Arguments = "-Xmx1024M -Xms1024M -jar forge-server.jar nogui",
                    WorkingDirectory = @"C:\Users\nonam\AppData\Roaming\.minecraft",
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                },
                EnableRaisingEvents = true
            };

            //process.OutputDataReceived += Process_OutputDataReceived;

            ServerConsoleProcess = process;
            process.Start();

            //Task.Run(() =>
            //{
            //    HookConsoleLog.Iniciate(process.Id);
            //});

            Thread t = new Thread(() => ReadLogInTime());
            t.Start();

            //Task.Run(() =>
            //{
            //    while (!process.HasExited)
            //    {
            //        var output = process.StandardOutput.ReadLine();
            //        if (!string.IsNullOrEmpty(output))
            //        {
            //            //Console.WriteLine("Синхронный вывод: " + output);
            //            OutputDataReceived(output);
            //        }
            //    }
            //});
            await Task.CompletedTask;
        }

        public async void OutputDataReceived(string? msg)
        {
            if (!string.IsNullOrEmpty(msg))
            {
                if (ServerProcess == null)
                {
                    if (msg.Contains("Starting minecraft server version"))
                    {
                        //rcon = new RCON(new IPEndPoint(IPAddress.Parse("192.168.31.204"), 25575), "gamemode1");
                        //ServerProcess = Process.GetProcessesByName("Minecraft server")[0];
                    }
                }
                var hubContext = Helper.thisApp.Services.GetRequiredService<IHubContext<MinecraftLogHub>>();
                await hubContext.Clients.All.SendAsync("ReceiveLog", msg);
                consoleLogs += "\n" + msg;
            }
        }

        public async void SendCommand(string command)
        {
            if (string.IsNullOrEmpty(command)) { return; }
            if (rcon == null) { return; }

            await rcon.SendCommandAsync(command);
        }

        private async void ReadLogInTime()
        {
            try
            {
                if (!File.Exists(logPath))
                {
                    Console.WriteLine("Файл логов не найден.");
                    return;
                }

                if(File.Exists(tempLogPath))
                {
                    File.WriteAllText(tempLogPath, "");
                }
                consoleLogs = "Логи появяться здесь...";

                Timer timer = new Timer(_ =>
                {
                    try
                    {
                        File.Copy(logPath, tempLogPath, true); // Копирование файла
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ошибка копирования файла: " + ex.Message);
                    }
                }, null, 0, 1000); // Обновление копии каждую секунду

                using (var fileStream = new FileStream(tempLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    Console.WriteLine("Чтение копии логов...");

                    // Переместить указатель на конец файла
                    fileStream.Seek(0, SeekOrigin.End);

                    while (true)
                    {
                        string? line = reader.ReadLine();
                        if (!string.IsNullOrEmpty(line))
                        {
                            OutputDataReceived(line);
                        }
                        else
                        {
                            Thread.Sleep(100); // Пауза, если новых строк нет
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
        }
    }



    public class MinecraftLogHub : Hub
    {
        public async Task SendLog(string message)
        {
            await Clients.All.SendAsync("ReceiveLog", message);
        }
    }
}
