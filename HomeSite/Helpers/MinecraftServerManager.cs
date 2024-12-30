using CoreRCON;
using HomeSite.Controllers;
using HomeSite.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace HomeSite.Helpers
{
    public class MinecraftServerManager
    {
        private static MinecraftServerManager? _instance;
        //public bool IsRunning { get { return ServerConsoleProcess != null; } }
        public bool IsRunning 
        { 
            get
            {
                if(ServerConsoleProcess == null)
                {
                    return false;
                }
                else
                {
                    if(ServerConsoleProcess.HasExited == false)
                    {
                        return true;
                    }
                    else
                    {
                        ServerConsoleProcess = null;
                        ServerController.Sendtype = SendType.Skip;
                        return false;
                    }
                }
            }
        }
        public ServerState ServerState { get; set; }
        private const string logPath = @"C:\Users\nonam\AppData\Roaming\.minecraft\logs\latest.log";
        private const string tempLogPath = @"C:\Users\nonam\AppData\Roaming\.minecraft\logs\temp.log";
        private RCON? rcon;
        public Process? ServerConsoleProcess { get; private set; }
        public Process? ServerProcess { get; private set; }
        private MinecraftServerManager()
        {
            var processes = Process.GetProcessesByName("cmd");
            if(processes.Length > 1)
            {
                ServerConsoleProcess = processes[0];
                Thread t = new Thread(() => ReadLogInTime(cts.Token));
                t.Start();
                Task.Run(CheckStartedServer);
                ServerController.Sendtype = SendType.Skip;
            }

        }
        public string ConsoleLogs { get { return consoleLogs; } }
        private CancellationTokenSource cts = new();
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
                await SendCommand("stop");
                //while(!ServerConsoleProcess.HasExited)
                //{
                //    await Task.Delay(1000);
                //}
                //ServerConsoleProcess = null;
                rcon = null;
                cts.Cancel();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        public async Task LaunchServer()
        {
            try 
            {
                ServerState = ServerState.starting;
                if(cts.IsCancellationRequested)
                {
                    cts = new CancellationTokenSource();
                }
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
                File.WriteAllText(logPath, string.Empty);
                ServerConsoleProcess = process;
                process.Start();

                //Task.Run(() =>
                //{
                //    HookConsoleLog.Iniciate(process.Id);
                //});

                Thread t = new Thread(() => ReadLogInTime(cts.Token));
                t.Start();
                //Task.Run(CheckStartedServer);

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
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private async void CheckStartedServer()
        {
            //await Task.Delay(7000);
            if (ServerProcess == null)
            {
                var processes = Process.GetProcessesByName("java");
                while (processes.Length < 1)
                {
                    await Task.Delay(1000);
                    processes = Process.GetProcessesByName("java");
                }
                ServerProcess = processes[1];
                ServerState = ServerState.started;
                rcon = new RCON(new IPEndPoint(IPAddress.Parse("192.168.31.204"), 25575), "gamemode1");
                ServerController.Sendtype = SendType.Server;
                MinecraftServer.CreateInstance(ServerProcess);
            }
        }

        public async void OutputDataReceived(string? msg)
        {
            try
            {
                if (!string.IsNullOrEmpty(msg))
                {
                    if (ServerProcess == null)
                    {
                        if (msg.Contains("Thread RCON Listener started"))
                        {
                            CheckStartedServer();
                        }
                    }
                    var hubContext = Helper.thisApp.Services.GetRequiredService<IHubContext<MinecraftLogHub>>();
                    await hubContext.Clients.All.SendAsync("ReceiveLog", msg);
                    consoleLogs += "\n" + msg;
                    return;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public async Task<string> SendCommand(string command)
        {
            if (string.IsNullOrEmpty(command) || command.Contains("stop")) { return "ага, фигушки"; }
            if (rcon == null) { return "сервер еще запускается"; }

            return await rcon.SendCommandAsync(command);
        }

        private async void ReadLogInTime(CancellationToken token)
        {
            try
            {
                if (!File.Exists(logPath))
                {
                    Console.WriteLine("Файл логов не найден.");
                    return;
                }
                await Task.Delay(2000);
                File.WriteAllText(tempLogPath, string.Empty);
                consoleLogs = "Логи появяться здесь...";
                CloneCycle(token);
                using (var fileStream = new FileStream(tempLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    //Catched:
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
                            await Task.Delay(100); // Пауза, если новых строк нет
                        }
                        if(token.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
                //goto Catched;
            }
        }

        private async Task CloneCycle(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                    File.Copy(logPath, tempLogPath, true); // Копирование файла
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка копирования файла: " + ex.Message);
                }
                await Task.Delay(1000, token);
            }
        }
    }

    public class MinecraftServer
    {
        private static MinecraftServer? _instance;
        private Process ServerProcess { get; }
        private CancellationTokenSource cts;
        public int Players { get; }
        public float RamUsage { get; }
        private int players = 0;
        private float ramUsage = 0;
        private MinecraftServer(Process serverProcess)
        {
            ServerProcess = serverProcess;
            cts = new CancellationTokenSource();
            Task.Run(() => StartClock(cts.Token));
        }

        public static void CreateInstance(Process serverProcess)
        {
            if (_instance != null) return;
            _instance = new (serverProcess);
        }

        public static MinecraftServer? GetInstance()
        {
            return _instance;
        }

        public void StopClock()
        {
            cts.Cancel();
        }

        private async Task StartClock(CancellationToken token)
        {
            while(!token.IsCancellationRequested)
            {
                await Task.Delay(5000, token);
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
