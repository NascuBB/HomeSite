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
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using NuGet.Common;
using System;
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

        public bool IsRunning { get => isRunning; }
        public string ConsoleLogs { get => _consoleLogs.ToString(); }
        public int Players { get => players; }
        public float RamUsage { get => ramUsage; }
        public int RemainingTime { get => remainingTime; }

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

        private readonly string RconStartedMessage;

        private readonly LogConnectionManager _logConnectionManager;
        public string Id { get; }
        public string Version { get; }
        public string ServerCore { get; }
        public int BedrockPort { get; }
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
            BedrockPort = specs.BedrockPort;

            DomainName = specs.DomainName;

            _containerName = $"mc-{specs.Id}";
            _dockerClient = dockerClient;

            ServerState = ServerState.starting;

            RconStartedMessage = "RCON";
            Task.Run(() => MonitorContainerLogAsync(_cts.Token));

        }

        private async Task MonitorContainerLogAsync(CancellationToken token)
        {
            try
            {
                bool rconStarted = false;
                var logParams = new ContainerLogsParameters
                {
                    ShowStdout = true,
                    ShowStderr = true,
                    Follow = true,
                    Tail = "100"
                };

                var multiplexedStream = await _dockerClient.Containers.GetContainerLogsAsync(
                _containerName,
                false,
                logParams,
                token);

                byte[] buffer = new byte[8192];

                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (!token.IsCancellationRequested)
                        {
                            var readResult = await multiplexedStream.ReadOutputAsync(buffer, 0, buffer.Length, token);

                            if (readResult.EOF) break;

                            string line = Encoding.UTF8.GetString(buffer, 0, readResult.Count);

                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                await _logConnectionManager.BroadcastLogAsync(Id, line);
                                _consoleLogs.Append(line);

                                if (line.Contains("RCON ", StringComparison.OrdinalIgnoreCase) && !rconStarted)
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
        }

        private async Task<float> GetDockerRamUsage(CancellationToken token)
        {
            try
            {
                var progress = new Progress<ContainerStatsResponse>();
                ContainerStatsResponse? stats = null;

                progress.ProgressChanged += (s, e) => stats = e;

                await _dockerClient.Containers.GetContainerStatsAsync(
                    _containerName,
                    new ContainerStatsParameters { Stream = false },
                    progress,
                    token);

                if (stats != null)
                {
                    float usedBytes = stats.MemoryStats.Usage;
                    return usedBytes / 1024 / 1024;
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

            ramUsage = await GetDockerRamUsage(token);

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
                catch { }
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
                var id = await GetContainerIdByName(_containerName);
                if (id is null) return;
                await _dockerClient.Containers.StopContainerAsync(id, new ContainerStopParameters
                {
                    WaitBeforeKillSeconds = 10
                });
                //await _dockerClient.Containers.RemoveContainerAsync(id,
                //    new ContainerRemoveParameters { Force = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}]Stop server error {ex.Message}");
            }
        }

        private async Task<string?> GetContainerIdByName(string containerName)
        {
            var filters = new ContainersListParameters
            {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    {
                        "name", new Dictionary<string, bool>
                        {
                            { containerName, true }
                        }
                    }
                }
            };

            var containers = await _dockerClient.Containers.ListContainersAsync(filters);

            var container = containers.FirstOrDefault(c =>
                c.Names.Any(n => n.Equals("/" + containerName) || n.Equals(containerName)));

            return container?.ID;
        }

        public async Task OnContainerExited()
        {
            _cts.Cancel();
            if (ServerState == ServerState.starting)
                await ServerController.NotifyServerCrashed(Id);
        }
        public async Task<string> SendCommandAsync(string command)
        {
            if (_rcon == null) { return "сервер еще запускается"; }
            return await _rcon.SendCommandAsync(command);
        }

        public void Dispose()
        {
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
                catch (Exception ex) //when (ex is not TaskCanceledException)
                {
                    if (ex is not TaskCanceledException)
                        Console.WriteLine(ex.ToString());
                    await Task.Delay(5000, token);
                    return;
                }
            }
        }
    }
}
