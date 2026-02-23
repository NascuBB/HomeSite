//using CoreRCON;
//using Docker.DotNet;
//using Docker.DotNet.Models;
//using HomeSite.Managers;
//using HomeSite.Models;
//using System.Net;
//using System.Text;

//namespace HomeSite.Entities
//{
//    public class MinecraftServer : IDisposable
//    {
//        // Свойства для фронтенда оставляем те же
//        public string Id { get; }
//        public string Name { get; set; }
//        public ServerState ServerState { get; private set; }
//        public string ConsoleLogs { get => _consoleLogs.ToString(); }
//        public int Players { get; private set; }
//        public float RamUsage { get; private set; }

//        private readonly DockerClient _dockerClient;
//        private readonly string _containerName;
//        private readonly CancellationTokenSource _cts = new();
//        private StringBuilder _consoleLogs = new();
//        private RCON? _rcon;

//        public MinecraftServer(string id, Server specs)
//        {
//            Id = id;
//            Name = specs.Name;
//            _containerName = $"mc-server-{id}";

//            // Подключаемся к локальному сокету Docker
//            _dockerClient = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();

//            ServerState = ServerState.starting;

//            // Запускаем фоновые задачи мониторинга
//            Task.Run(() => MonitorContainerAsync(_cts.Token));
//        }

//        private async Task MonitorContainerAsync(CancellationToken token)
//        {
//            try
//            {
//                // 1. Подписываемся на логи Docker вместо чтения файла
//                var logParams = new ContainerLogsParameters
//                {
//                    ShowStdout = true,
//                    ShowStderr = true,
//                    Follow = true,
//                    Tail = "100"
//                };

//                using var logStream = await _dockerClient.Containers.GetContainerLogsAsync(_containerName, false, logParams, token);

//                // 2. В фоновом режиме читаем логи и ищем сообщение о запуске RCON
//                _ = Task.Run(async () => {
//                    using var reader = new StreamReader(logStream);
//                    while (!reader.EndOfStream && !token.IsCancellationRequested)
//                    {
//                        var line = await reader.ReadLineAsync();
//                        if (line != null)
//                        {
//                            _consoleLogs.AppendLine(line);
//                            // Логика определения готовности сервера (как у тебя была с RconStartedMessage)
//                            if (line.Contains("RCON running on"))
//                            {
//                                await OnServerStarted();
//                            }
//                        }
//                    }
//                });

//                // 3. Цикл обновления статистики (RAM / Players)
//                while (!token.IsCancellationRequested)
//                {
//                    await UpdateStatsAsync();
//                    await Task.Delay(5000, token);
//                }
//            }
//            catch (Exception ex) { Console.WriteLine($"Docker Monitor Error: {ex.Message}"); }
//        }

//        private async Task OnServerStarted()
//        {
//            ServerState = ServerState.started;

//            _rcon = new RCON(new IPEndPoint(IPAddress.Parse(_containerName), 5015), ConfigManager.RCONPassword);
//        }

//        private async Task UpdateStatsAsync()
//        {
//            // Получаем загрузку памяти из Docker Stats
//            var stats = await _dockerClient.Containers.GetContainerStatsAsync(_containerName, new ContainerStatsParameters { Stream = false }, _cts.Token);
//            // Тут нужно распарсить JSON из stats, чтобы вытащить MemoryUsage

//            // Получаем игроков через RCON
//            if (_rcon != null)
//            {
//                string plRaw = await _rcon.SendCommandAsync("list");
//                // Твоя логика парсинга игроков из строки "list"
//            }
//        }

//        public async Task StopServer()
//        {
//            if (_rcon != null) await _rcon.SendCommandAsync("stop");
//            await _dockerClient.Containers.StopContainerAsync(_containerName, new ContainerStopParameters());
//            _cts.Cancel();
//        }

//        public void Dispose() => _cts.Cancel();
//    }
//}



using CoreRCON;
using Docker.DotNet;
using Docker.DotNet.Models;
using HomeSite.Controllers;
using HomeSite.Generated;
using HomeSite.Managers;
using HomeSite.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using NuGet.Common;
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

        public bool IsRunning { get => isRunning; }
        public string ConsoleLogs { get => _consoleLogs.ToString(); }
        public int Players { get => players; }
        public float RamUsage { get => ramUsage; }
        public int RemainingTime { get => remainingTime; }

        //private Process? ServerProcess { get; set; }
        //private Process? ServerConsoleProcess { get; set; }

        private int players = 0;
        private float ramUsage = 0;
        private int remainingTime = 5 * 60;
        private bool isRunning = false;

        private readonly IDockerClient _dockerClient;
        private readonly string _containerName;
        private readonly CancellationTokenSource _cts = new();
        private StringBuilder _consoleLogs = new();
        private RCON? _rcon = null;
        private Timer? shutdownTimer;
        //private Timer reconnectTimer;

        private readonly string RconStartedMessage;

        private readonly LogConnectionManager _logConnectionManager;
        private readonly IDbContextFactory<ServerDBContext> _contextFactory;

        //public event Action<string> OnServerShutdown; // Событие для уведомления об остановке сервера
        //public event Action<string, int> OnTimerUpdate; // Отправка оставшегося времени на клиент

        public string Id { get; }
        public string Version { get; }
        public string ServerCore { get; }
        //public string ServerPath { get; }
        //public string LogPath { get; }
        //public string TempLogPath { get; }
        public int PublicPort { get; }
        public int RCONPort { get; }
        public string DomainName { get; }
        public ServerCreation ServerCreation { get; }

        public MinecraftServer(Server specs, LogConnectionManager manager, IDockerClient dockerClient)
        {
            _logConnectionManager = manager;
            _cts = new CancellationTokenSource();

            Id = specs.Id;
            Name = specs.Name;
            Description = specs.Description;
            Version = specs.Version;
            ServerCore = specs.ServerCore;
            PublicPort = specs.PublicPort;
            RCONPort = specs.RCONPort;
            DomainName = specs.DomainName;
            //OwnerUsername = specs.OwnerName;

            _containerName = $"mc-{specs.Id}";
            _dockerClient = dockerClient;

            ServerState = ServerState.starting;
            //ServerPath = Path.Combine(Directory.GetCurrentDirectory(), "servers", Id);
            //LogPath = Path.Combine(ServerPath, "logs", "latest.log");
            //TempLogPath = Path.Combine(ServerPath, "logs", "temp.log");


            RconStartedMessage = "RCON";
            Task.Run(() => MonitorContainerLogAsync(_cts.Token));
            //Task.Run(async () =>
            //{
            //    // 3. Цикл обновления статистики (RAM / Players)
            //    while (!_cts.Token.IsCancellationRequested)
            //    {
            //        await UpdateStatsAsync();
            //        await Task.Delay(5000, _cts.Token);
            //    }
            //});
            //if (ServerCore == ServerCore.Forge)
            //{
            //    switch (Version)
            //    {
            //        case MinecraftVersion._1_12_2:
            //            RconStartedMessage = "RCON running on";
            //            break;
            //        case MinecraftVersion._1_16_5:
            //            RconStartedMessage = "empty";
            //            break;
            //        case MinecraftVersion._1_19_2:
            //            RconStartedMessage = "Thread RCON Listener started";
            //            break;
            //        default:
            //            RconStartedMessage = "RCON running on";
            //            break;
            //    }
            //}
            //else
            //{
            //    RconStartedMessage = "RCON running on";
            //}

        }


        //public async void StartServer()
        //{
        //    try
        //    {
        //        //ServerState = ServerState.starting;
        //        if (ServerConsoleProcess != null)
        //        {
        //            throw new Exception("Сервер уже запущен");
        //        }
        //        var process = new Process
        //        {
        //            StartInfo = new ProcessStartInfo
        //            {
        //                FileName = Path.Combine(ServerPath, "run.bat"),
        //                //Arguments = "-Xmx1024M -Xms1024M -jar forge-server.jar nogui",
        //                WorkingDirectory = ServerPath,
        //                RedirectStandardOutput = false,
        //                RedirectStandardError = false,
        //                UseShellExecute = true,
        //                CreateNoWindow = false,
        //            },
        //            EnableRaisingEvents = true
        //        };

        //        //process.OutputDataReceived += Process_OutputDataReceived;
        //        File.WriteAllText(LogPath, string.Empty);
        //        ServerConsoleProcess = process;
        //        process.Exited += ServerConsoleProcess_Exited;
        //        process.Start();

        //        //Task.Run(() =>
        //        //{
        //        //    HookConsoleLog.Iniciate(process.Id);
        //        //});
        //        await Task.Delay(1000);
        //        Thread t = new Thread(async () => await MonitorLogAsync(Id, LogPath, cts.Token));
        //        t.Start();
        //        //Task.Run(CheckStartedServer);

        //        //Task.Run(() =>
        //        //{
        //        //    while (!process.HasExited)
        //        //    {
        //        //        var output = process.StandardOutput.ReadLine();
        //        //        if (!string.IsNullOrEmpty(output))
        //        //        {
        //        //            //Console.WriteLine("Синхронный вывод: " + output);
        //        //            OutputDataReceived(output);
        //        //        }
        //        //    }
        //        //});
        //        await Task.CompletedTask;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString());
        //    }
        //}

        //private async void ServerConsoleProcess_Exited(object? sender, EventArgs e)
        //{
        //    cts.Cancel();
        //    if (ServerState == ServerState.starting)
        //        await ServerController.NotifyServerCrashed(Id);
        //    await MinecraftServerManager.ServerEnded(this);
        //}


        private async Task MonitorContainerLogAsync(CancellationToken token)
        {
            try
            {
                bool rconStarted = false;
                // 1. Подписываемся на логи Docker вместо чтения файла
                var logParams = new ContainerLogsParameters
                {
                    ShowStdout = true,
                    ShowStderr = true,
                    Follow = true,
                    Tail = "100"
                };

                var multiplexedStream = await _dockerClient.Containers.GetContainerLogsAsync(
                _containerName,
                false, // tty
                logParams,
                token);

                // 2. В фоновом режиме читаем кадры
                // 1. Создаем буфер заранее
                byte[] buffer = new byte[8192];

                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (!token.IsCancellationRequested)
                        {
                            // Вот правильная сигнатура: буфер, отступ, длина, токен
                            var readResult = await multiplexedStream.ReadOutputAsync(buffer, 0, buffer.Length, token);

                            if (readResult.EOF) break;

                            // Конвертируем только то количество байт, которое реально прочитали
                            // readResult.Count — это сколько байт Docker положил в буфер (уже без заголовков)
                            string line = Encoding.UTF8.GetString(buffer, 0, readResult.Count);

                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                await _logConnectionManager.BroadcastLogAsync(Id, line);
                                _consoleLogs.Append(line);

                                if (line.Contains("RCON", StringComparison.OrdinalIgnoreCase) && !rconStarted)
                                {
                                    rconStarted = true;

                                    await OnServerStarted();
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка: {ex.Message}");
                    }
                }, token);


            }
            catch (Exception ex) { Console.WriteLine($"Docker Monitor Error: {ex.Message}"); }
        }

        //        async Task MonitorLogAsync(string Id, string logPath, CancellationToken token)
        //        {

        //            if (!File.Exists(logPath))
        //            {
        //                Console.WriteLine($"Файл логов не найден: {logPath}");
        //                return;
        //            }

        //            //Console.WriteLine($"Следим за логами: {logPath}");

        //            using FileStream fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        //            using StreamReader reader = new StreamReader(fs, Encoding.UTF8);

        //            reader.BaseStream.Seek(0, SeekOrigin.End); // Пропускаем старые строки
        //            try
        //            {
        //                while (!token.IsCancellationRequested)
        //                {
        //                    string? line = await reader.ReadLineAsync();
        //                    if (line != null)
        //                    {

        //                        if (ServerState != ServerState.started && line.Contains(RconStartedMessage))
        //                        {
        //#if DEBUG
        //                            Task.Run(async () =>
        //                            {
        //                                await ServerController.NotifyServerStarted(Id);
        //                                ServerState = ServerState.started;
        //                            });
        //                            Task.Run(() => StartClock(token));
        //#else
        //                            Task.Run(() => CheckStartedServer(token));
        //#endif
        //                        }
        //                        await _logConnectionManager.BroadcastLogAsync(Id, line);
        //                        consoleLogs += "\n" + line;
        //                    }
        //                    else
        //                    {
        //                        await Task.Delay(100, token); // Ждём, если новых строк нет
        //                    }
        //                }
        //            }
        //            catch (Exception ex) //when (ex is not TaskCanceledException)
        //            {
        //                if (ex is not TaskCanceledException)
        //                    Console.WriteLine($"Error: {ex}");
        //            }
        //        }

        private async void TimerCallback(object? state)
        {
            if (_cts.Token.IsCancellationRequested)
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

        private async Task<float> GetDockerRamUsage(CancellationToken token)
        {
            try
            {
                // Нам нужно реализовать IProgress, чтобы метод сработал.
                // Так как Stream = false, прогресс вызовется всего один раз.
                var progress = new Progress<ContainerStatsResponse>();
                ContainerStatsResponse? stats = null;

                // Используем обертку, чтобы поймать результат из события Progress
                progress.ProgressChanged += (s, e) => stats = e;

                await _dockerClient.Containers.GetContainerStatsAsync(
                    _containerName,
                    new ContainerStatsParameters { Stream = false },
                    progress,
                    token);

                if (stats != null)
                {
                    // Формула для вычисления использования памяти в МБ
                    // Docker отдает использование в байтах (MemoryStats.Usage)
                    float usedBytes = stats.MemoryStats.Usage;
                    return usedBytes / 1024 / 1024; // Возвращаем в Мегабайтах
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения метрик Docker: {ex.Message}");
            }

            return 0;
        }

        private async Task OnServerStarted()
        {
            ServerState = ServerState.started;
            shutdownTimer = new Timer(TimerCallback, null, 1000, 1000);
            await ServerController.NotifyServerStarted(Id);
            Task.Run(() => StartClock(_cts.Token));
            var addresses = await Dns.GetHostAddressesAsync(_containerName);
            var ipAddress = addresses.First();

            _rcon = new RCON(new IPEndPoint(ipAddress, 25575), ConfigManager.RCONPassword);
        }

        private async Task UpdateStatsAsync(CancellationToken token)
        {
            if (ServerState != ServerState.started) return;

            // Обновляем память
            ramUsage = await GetDockerRamUsage(token);

            // Обновляем игроков (через твой RCON)
            if (_rcon != null)
            {
                try
                {
                    var response = await _rcon.SendCommandAsync("list");
                    int.TryParse(new string(response
                        .SkipWhile(x => !char.IsDigit(x))
                        .TakeWhile(x => char.IsDigit(x))
                        .ToArray()), out players);
                }
                catch { /* RCON может быть временно недоступен при лагах */ }
            }
        }

        public async Task StopServer()
        {
            try
            {
                if (_rcon != null)
                {
                    await _rcon.SendCommandAsync("stop");
                    await Task.Delay(5000);
                }
                _cts.Cancel();
                await _dockerClient.Containers.StopContainerAsync(_containerName, new ContainerStopParameters
                {
                    WaitBeforeKillSeconds = 10
                });
                await _dockerClient.Containers.RemoveContainerAsync(_containerName, new ContainerRemoveParameters
                {
                    Force = true
                });
                //if (ServerState == ServerState.starting)
                //    await ServerController.NotifyServerCrashed(Id);
                //await MinecraftServerManager.ServerEnded(this);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении контейнера: {ex.Message}");
            }
        }

        public async void OnContainerExited()
        {
            _cts.Cancel();
            if (ServerState == ServerState.starting)
                await ServerController.NotifyServerCrashed(Id);
            await MinecraftServerManager.ServerEnded(this);
        }

        //private async void CheckStartedServer(CancellationToken token)
        //{
        //    //await Task.Delay(7000);
        //    if (ServerProcess == null)
        //    {
        //        //           var processes = Process.GetProcessesByName("java");
        //        //           while (processes.Length < MinecraftServerManager.serversOnline.Count && !token.IsCancellationRequested)
        //        //           {
        //        //               try
        //        //               {
        //        //                   await Task.Delay(100, token);
        //        //                   processes = Process.GetProcessesByName("java");
        //        //               }
        //        //               catch(Exception ex)
        //        //               {
        //        //                   if (ex is not TaskCanceledException)
        //        //                       Console.WriteLine($"Error: {ex}");
        //        //                   return;
        //        //}
        //        //               Console.WriteLine("ВСЕ ЕЩЕ ИЩУ СЕРВЕР ЖАВАВ");
        //        //           }
        //        var processes = GetChildProcesses(ServerConsoleProcess.Id);
        //        while (processes.Length == 1)
        //        {
        //            try
        //            {
        //                await Task.Delay(100, token);
        //                processes = GetChildProcesses(ServerConsoleProcess.Id);
        //            }
        //            catch (Exception ex)
        //            {
        //                if (ex is not TaskCanceledException)
        //                    Console.WriteLine($"Error: {ex}");
        //                return;
        //            }
        //        }

        //        ServerProcess = processes.FirstOrDefault(x => x.MainModule.ModuleName == "java.exe");
        //        //if(Version == MinecraftVersion._1_19_2)
        //        //    ServerProcess = GetChildProcesses(ServerProcess.Id).FirstOrDefault(x => x.MainModule.ModuleName == "java.exe");
        //        Console.WriteLine(ServerProcess);
        //        if (processes.Length == MinecraftServerManager.serversOnline.Count)
        //            ServerProcess = processes[MinecraftServerManager.serversOnline.Count - 1];
        //        ServerState = ServerState.started;
        //        rcon = new RCON(new IPEndPoint(IPAddress.Parse(ConfigManager.LocalAddress!), RCONPort), ConfigManager.RCONPassword);
        //        Task.Run(() => StartClock(cts.Token));
        //        shutdownTimer = new Timer(TimerCallback, null, 1000, 1000);
        //        await ServerController.NotifyServerStarted(Id);
        //        //ServerController.Sendtype = SendType.Server;

        //    }
        //}
        //        static Process[] GetChildProcesses(int parentId)
        //        {
        //            //Я знаю что только на шиндовс
        //#pragma warning disable CA1416 // Проверка совместимости платформы
        //            var searcher = new ManagementObjectSearcher(
        //                $"SELECT ProcessId FROM Win32_Process WHERE ParentProcessId={parentId}");
        //            return searcher.Get().Cast<ManagementObject>()
        //                .Select(mo => Process.GetProcessById(Convert.ToInt32(mo["ProcessId"])))
        //                .ToArray();
        //#pragma warning restore CA1416 // Проверка совместимости платформы
        //        }

        //public async Task StopServer()
        //{
        //    try
        //    {
        //        if (rcon == null) { return; }

        //        await rcon.SendCommandAsync("stop");
        //        rcon = null;
        //        cts.Cancel();
        //        //cts.Dispose();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString());
        //    }
        //}
        public async Task<string> SendCommandAsync(string command)
        {
            if (_rcon == null) { return "сервер еще запускается"; }
            return await _rcon.SendCommandAsync(command);
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
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await UpdateStatsAsync(token);
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
                catch (Exception ex) when (ex is not TaskCanceledException)
                {
                    if (ex is not TaskCanceledException)
                        Console.WriteLine(ex.ToString());
                    return;
                }
            }
        }
    }
}
