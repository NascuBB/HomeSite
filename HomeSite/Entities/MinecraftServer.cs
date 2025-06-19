using CoreRCON;
using HomeSite.Controllers;
using HomeSite.Generated;
using HomeSite.Managers;
using HomeSite.Models;
using System.Diagnostics;
using System.Management;
using System.Net;
using System.Text;

namespace HomeSite.Entities
{
    public class MinecraftServer : IDisposable
    {
        public string? Description { get; set; }
        public string Name { get; set; }

        public ServerState ServerState { get; private set; }
        //public string OwnerUsername { get; private set; }

        public bool IsRunning
        {
            get
            {
                if (ServerConsoleProcess == null || ServerConsoleProcess.HasExited)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        public string ConsoleLogs { get => consoleLogs; }
        public int Players { get => players; }
        public float RamUsage { get => ramUsage; }
        public int RemainingTime { get => remainingTime; }

        private Process? ServerProcess { get; set; }
        private Process? ServerConsoleProcess { get; set; }

        private int players = 0;
        private float ramUsage = 0;
        private int remainingTime = 5 * 60;

        private string consoleLogs = "Логи сервера появятся здесь...";
        private RCON? rcon = null;
        private Timer shutdownTimer;
        //private Timer reconnectTimer;

        private readonly string RconStartedMessage;

        private readonly LogConnectionManager _logConnectionManager;
        private readonly ServerDBContext _serverContext;
        private readonly CancellationTokenSource cts;

        public event Action<string> OnServerShutdown; // Событие для уведомления об остановке сервера
        public event Action<string, int> OnTimerUpdate; // Отправка оставшегося времени на клиент

        public string Id { get; }
        public MinecraftVersion Version { get; }
        public ServerCore ServerCore { get; }
        public string ServerPath { get; }
        public string LogPath { get; }
        public string TempLogPath { get; }
        public int PublicPort { get; }
        public int RCONPort { get; }
        public ServerCreation ServerCreation { get; }

        public MinecraftServer(string id, LogConnectionManager manager, ServerDBContext serverContext)
        {
            _logConnectionManager = manager;
            _serverContext = serverContext;
            cts = new CancellationTokenSource();
            Id = id;

            Server specs;
            specs = _serverContext.Servers.First(x => x.Id == id);

            Name = specs.Name;
            Description = specs.Description;
            Version = specs.Version;
            ServerCore = specs.ServerCore;
            PublicPort = specs.PublicPort;
            RCONPort = specs.RCONPort;
            //OwnerUsername = specs.OwnerName;

            ServerState = ServerState.starting;
            ServerPath = Path.Combine(Directory.GetCurrentDirectory(), "servers", Id);
            LogPath = Path.Combine(ServerPath, "logs", "latest.log");
            TempLogPath = Path.Combine(ServerPath, "logs", "temp.log");

            if (ServerCore == ServerCore.Forge)
            {
                switch (Version)
                {
                    case MinecraftVersion._1_12_2:
                        RconStartedMessage = "RCON running on";
                        break;
                    case MinecraftVersion._1_16_5:
                        RconStartedMessage = "empty";
                        break;
                    case MinecraftVersion._1_19_2:
                        RconStartedMessage = "Thread RCON Listener started";
                        break;
                    default: 
                        RconStartedMessage = "RCON running on";
                        break;
                }
            }
            else
            {
                RconStartedMessage = "RCON running on";
            }

        }

        //~MinecraftServer()
        //{
        //    cts!.Cancel();
        //}


        public async void StartServer()
        {
            try
            {
                //ServerState = ServerState.starting;
                if (ServerConsoleProcess != null)
                {
                    throw new Exception("Сервер уже запущен");
                }
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Path.Combine(ServerPath, "run.bat"),
                        //Arguments = "-Xmx1024M -Xms1024M -jar forge-server.jar nogui",
                        WorkingDirectory = ServerPath,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false,
                        UseShellExecute = true,
                        CreateNoWindow = false,
                    },
                    EnableRaisingEvents = true
                };

                //process.OutputDataReceived += Process_OutputDataReceived;
                File.WriteAllText(LogPath, string.Empty);
                ServerConsoleProcess = process;
                process.Exited += ServerConsoleProcess_Exited;
                process.Start();

                //Task.Run(() =>
                //{
                //    HookConsoleLog.Iniciate(process.Id);
                //});
                await Task.Delay(1000);
                Thread t = new Thread(async () => await MonitorLogAsync(Id, LogPath, cts.Token));
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

        private async void ServerConsoleProcess_Exited(object? sender, EventArgs e)
        {
            cts.Cancel();
            if (ServerState == ServerState.starting)
                await ServerController.NotifyServerCrashed(Id);
            await MinecraftServerManager.ServerEnded(this);
        }

        async Task MonitorLogAsync(string Id, string logPath, CancellationToken token)
        {

            if (!File.Exists(logPath))
            {
                Console.WriteLine($"Файл логов не найден: {logPath}");
                return;
            }

            //Console.WriteLine($"Следим за логами: {logPath}");

            using FileStream fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader reader = new StreamReader(fs, Encoding.UTF8);

            reader.BaseStream.Seek(0, SeekOrigin.End); // Пропускаем старые строки
            try
            {
                while (!token.IsCancellationRequested)
                {
                    string? line = await reader.ReadLineAsync();
                    if (line != null)
                    {

                        if (ServerState != ServerState.started && line.Contains(RconStartedMessage))
                        {
#if DEBUG
                            Task.Run(async () =>
                            {
                                await ServerController.NotifyServerStarted(Id);
                                ServerState = ServerState.started;
                            });
                            Task.Run(() => StartClock(token));
#else
                            Task.Run(() => CheckStartedServer(token));
#endif
                        }
                        await _logConnectionManager.BroadcastLogAsync(Id, line);
                        consoleLogs += "\n" + line;
                    }
                    else
                    {
                        await Task.Delay(100, token); // Ждём, если новых строк нет
                    }
                }
            }
            catch (Exception ex) //when (ex is not TaskCanceledException)
            {
                if (ex is not TaskCanceledException)
                    Console.WriteLine($"Error: {ex}");
            }
        }

        private async void TimerCallback(object? state)
        {
            if (cts.Token.IsCancellationRequested)
            {
                shutdownTimer.Dispose();
                return;
            }
            if (remainingTime <= 0)
            {
                shutdownTimer.Dispose();
                await StopServer();
                return;
            }

            remainingTime--;
            //Console.WriteLine($"Оставшееся время: {remainingTime / 60}:{remainingTime % 60:D2}");
        }

        private async void CheckStartedServer(CancellationToken token)
        {
            //await Task.Delay(7000);
            if (ServerProcess == null)
            {
                //           var processes = Process.GetProcessesByName("java");
                //           while (processes.Length < MinecraftServerManager.serversOnline.Count && !token.IsCancellationRequested)
                //           {
                //               try
                //               {
                //                   await Task.Delay(100, token);
                //                   processes = Process.GetProcessesByName("java");
                //               }
                //               catch(Exception ex)
                //               {
                //                   if (ex is not TaskCanceledException)
                //                       Console.WriteLine($"Error: {ex}");
                //                   return;
                //}
                //               Console.WriteLine("ВСЕ ЕЩЕ ИЩУ СЕРВЕР ЖАВАВ");
                //           }
                var processes = GetChildProcesses(ServerConsoleProcess.Id);
                while (processes.Length == 1)
                {
                    try
                    {
                        await Task.Delay(100, token);
                        processes = GetChildProcesses(ServerConsoleProcess.Id);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not TaskCanceledException)
                            Console.WriteLine($"Error: {ex}");
                        return;
                    }
                }

                ServerProcess = processes.FirstOrDefault(x => x.MainModule.ModuleName == "java.exe");
                //if(Version == MinecraftVersion._1_19_2)
                //    ServerProcess = GetChildProcesses(ServerProcess.Id).FirstOrDefault(x => x.MainModule.ModuleName == "java.exe");
                Console.WriteLine(ServerProcess);
                if (processes.Length == MinecraftServerManager.serversOnline.Count)
                    ServerProcess = processes[MinecraftServerManager.serversOnline.Count - 1];
                ServerState = ServerState.started;
                rcon = new RCON(new IPEndPoint(IPAddress.Parse(ConfigManager.LocalAddress!), RCONPort), ConfigManager.RCONPassword);
                Task.Run(() => StartClock(cts.Token));
                shutdownTimer = new Timer(TimerCallback, null, 1000, 1000);
                await ServerController.NotifyServerStarted(Id);
                //ServerController.Sendtype = SendType.Server;

            }
        }
        static Process[] GetChildProcesses(int parentId)
        {
            //Я знаю что только на шиндовс
#pragma warning disable CA1416 // Проверка совместимости платформы
            var searcher = new ManagementObjectSearcher(
                $"SELECT ProcessId FROM Win32_Process WHERE ParentProcessId={parentId}");
            return searcher.Get().Cast<ManagementObject>()
                .Select(mo => Process.GetProcessById(Convert.ToInt32(mo["ProcessId"])))
                .ToArray();
#pragma warning restore CA1416 // Проверка совместимости платформы
        }

        public async Task StopServer()
        {
            try
            {
                if (rcon == null) { return; }

                await rcon.SendCommandAsync("stop");
                rcon = null;
                cts.Cancel();
                //cts.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        public async Task<string> SendCommandAsync(string command)
        {
            if (rcon == null) { return "сервер еще запускается"; }
            return await rcon.SendCommandAsync(command);
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            //Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        private async Task StartClock(CancellationToken token)
        {
#if DEBUG
            int c = 0;
#endif
            while (!token.IsCancellationRequested)
            {
                try
                {
#if !DEBUG
                    ServerProcess!.Refresh();
                    ramUsage = ServerProcess.WorkingSet64 / 1024 / 1024 - 78;
                    string plRaw = await SendCommandAsync("list");
                    int.TryParse(new string(plRaw
                        .SkipWhile(x => !char.IsDigit(x))
                        .TakeWhile(x => char.IsDigit(x))
                        .ToArray()), out players);
#else
                    if (c > 2)
                    {
                        if (c > 6)
                        {
                            players = 0;
                        }
                        else
                        {
                            players = 1;
                        }
                    }
                    c++;
#endif
                    if (players > 0)
                    {
                        if (shutdownTimer != null)
                        {
                            shutdownTimer.Dispose();
                            shutdownTimer = null;
                        }
                    }
                    else
                    {
                        if (shutdownTimer == null)
                        {
                            remainingTime = 120;
                            shutdownTimer = new Timer(TimerCallback, null, 1000, 1000);
                        }
                    }
                    await Task.Delay(5000, token);
                }
                catch (Exception ex) //when (ex is not TaskCanceledException)
                {
                    if (ex is not TaskCanceledException)
                        Console.WriteLine(ex.ToString());
                    return;
                }
            }
        }
    }
}
