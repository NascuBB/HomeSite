using CoreRCON;
using HomeSite.Controllers;
using HomeSite.Helpers;
using HomeSite.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore.Query.Internal;
using HomeSite.Migrations;

namespace HomeSite.Managers
{
    public class MinecraftServerManager
    {
        private readonly LogConnectionManager _logConnectionManager;

        private static readonly string versionsFolder = Path.Combine(Directory.GetCurrentDirectory(), "versions");
        private static readonly string serversjsPath = Path.Combine(Environment.CurrentDirectory, "servers", "servers.json");
        private static readonly string creatingsPath = Path.Combine(Environment.CurrentDirectory, "servers", "creatings.json");

        public static readonly string folder = Path.Combine(Environment.CurrentDirectory, "servers");

        public static List<MinecraftServer> serversOnline = new List<MinecraftServer>();
        public static Dictionary<string, ServerCreation> inCreation = new Dictionary<string, ServerCreation>();

        private static Dictionary<string, MinecraftServerSpecifications> serverMainSpecs = new Dictionary<string, MinecraftServerSpecifications>();
        private static Dictionary<int, int> availablePorts = new Dictionary<int, int>{
            { 25550, 5000 }, { 25551, 5001 }, { 25552, 5002 }, { 25553, 5003 }, { 25554, 5004 },
            { 25555, 5005 }, { 25556, 5006 }, { 25557, 5007 }, { 25558, 5008 }, { 25559, 5009 },
            { 25560, 5010 }, { 25561, 5011 }, { 25562, 5012 }, { 25563, 5013 }, { 25564, 5014 },
            { 25565, 5015 }, { 25566, 5016 }, { 25567, 5017 }, { 25568, 5018 }, { 25569, 5019 },
            { 25570, 5020 }
        };
        //public bool IsRunning { get { return ServerConsoleProcess != null; } }


        public MinecraftServerManager(LogConnectionManager logConnectionManager)
        {
            _logConnectionManager = logConnectionManager;
        }

        public static void Prepare()
        {
            Task.Run(async () => {
                serverMainSpecs = await GetServersSpecs();
                await UpdateAvailablePorts();
            });
            Task.Run(async () => { inCreation = await GetServersInCreation(); });
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                File.Create(serversjsPath);
                File.Create(creatingsPath);
            }
        }

        static async Task UpdateAvailablePorts()
        {
            foreach(var serverSpec in serverMainSpecs)
            {
                if(availablePorts.ContainsKey(serverSpec.Value.PublicPort))
                {
                    availablePorts.Remove(serverSpec.Value.PublicPort);
                }
            }
        }

        private static string GetVersion(MinecraftVersion version)
        {
            switch(version)
            {
                case MinecraftVersion._1_12_2: return "_1_12_2";
                case MinecraftVersion._1_16_5: return "_1_16_5";
                case MinecraftVersion._1_19_2: return "_1_19_2";
                default: return "";
            }
        }

        public static string GetDifficulty(Difficulty difficulty)
        {
            switch(difficulty)
            {
                case Difficulty.peaceful: return "peaceful";
                case Difficulty.normal: return "normal";
                case Difficulty.easy: return "easy";
                case Difficulty.hard: return "hard";
                default: return "";
            }
        }
        public static Difficulty GetDifficulty(string difficulty)
        {
            switch (difficulty)
            {
                case "peaceful": return Difficulty.peaceful;
                case "normal": return Difficulty.normal;
                case "easy": return  Difficulty.easy;
                case "hard": return Difficulty.hard;
                default: return Difficulty.peaceful;
            }
        }

        public static string GetGameMode(GameMode gameMode)
        {
            switch (gameMode)
            {
                case GameMode.survival: return "survival";
                case GameMode.creative: return "creative";
                case GameMode.adventure: return "adventure";
                case GameMode.spectrator: return "spectrator";
                default: return "";
            }
        }

        public static GameMode GetGameMode(string gameMode)
        {
            switch (gameMode)
            {
                case "survival": return GameMode.survival;
                case "creative": return GameMode.creative;
                case "adventure": return GameMode.adventure;
                case "spectrator": return GameMode.spectrator;
                default: return GameMode.survival;
            }
        }

        /// <summary>
        /// creates server folder and returns id of new created server
        /// </summary>
        /// <param name="name">name of server</param>
        /// <param name="ownerName">username of owner</param>
        /// <param name="version">version of server</param>
        /// <param name="description">description to server</param>
        /// <returns></returns>
        public static async Task<string> CreateServer(string name, string ownerName, MinecraftVersion version, string? description = null)
        {
            string genId = Guid.NewGuid().ToString();
            inCreation.Add(genId, ServerCreation.AddingMods);
            await SaveServersInCreation();
            Random r = new();
            int port = availablePorts.Keys.ElementAt(r.Next(availablePorts.Count));
            int rconP = availablePorts[port];
            availablePorts.Remove(port);
            MinecraftServerSpecifications serverSpecs = new MinecraftServerSpecifications
            {
                Description = description,
                Name = name,
                OwnerName = ownerName,
                Version = version,
                PublicPort = port,
                RCONPort = rconP
            };
            serverMainSpecs.Add(genId, serverSpecs);
            await SaveServersSpecs();
            Helper.Copy(Path.Combine(versionsFolder, GetVersion(version)), Path.Combine(folder, genId));
            File.WriteAllText(Path.Combine(folder, genId, "server.properties"), ServerPropertiesManager.DefaultServerProperties(port, rconP, description ?? "A Minecraft server"));
            return genId;
        }

        public static async Task<bool> FinishServerCreation(string Id)
        {
            inCreation.Remove(Id);
            await SaveServersInCreation();
            return true;
        }

        private static async Task<Dictionary<string, MinecraftServerSpecifications>> GetServersSpecs()
        {
            try
            {
                if (!Path.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                if (!File.Exists(serversjsPath))
                {
                    return new Dictionary<string, MinecraftServerSpecifications>();
                }
                return JsonConvert.DeserializeObject<Dictionary<string, MinecraftServerSpecifications>>(await File.ReadAllTextAsync(serversjsPath)) ?? new Dictionary<string, MinecraftServerSpecifications>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return new Dictionary<string, MinecraftServerSpecifications>();
            }
        }

        public static async Task<ServerCreation> GetServerCreation(string Id)
        {
            if(!inCreation.ContainsKey(Id))
            {
                return ServerCreation.Created;
            }
            return inCreation[Id];

        }

        private static async Task SaveServersSpecs()
        {
            string servers = JsonConvert.SerializeObject(serverMainSpecs, Formatting.Indented);
            await File.WriteAllTextAsync(serversjsPath,servers);
        }

        private static async Task<Dictionary<string, ServerCreation>> GetServersInCreation()
        {
            try
            {
                if (!Path.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                if (!File.Exists(creatingsPath))
                {
                    return new Dictionary<string, ServerCreation>();
                }
                return JsonConvert.DeserializeObject<Dictionary<string, ServerCreation>>(await File.ReadAllTextAsync(creatingsPath)) ?? new Dictionary<string, ServerCreation>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return new Dictionary<string, ServerCreation>();
            }
        }

        private static async Task SaveServersInCreation()
        {
            string servers = JsonConvert.SerializeObject(inCreation, Formatting.Indented);
            await File.WriteAllTextAsync(creatingsPath, servers);
        }

        public static MinecraftServerSpecifications GetServerSpecs(string id)
        {
            return serverMainSpecs[id];
        }

        public void LaunchServer(string Id)
        {
            MinecraftServer minecraftServer = new MinecraftServer(Id, _logConnectionManager);
            serversOnline.Add(minecraftServer);
            minecraftServer.StartServer();
        }

        public static string GetLastLogs(string Id)
        {
            Queue<string> recentLines = new Queue<string>(10);

            using (StreamReader reader = new StreamReader(Path.Combine(folder, Id, "logs", "latest.log")))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (recentLines.Count >= 10)
                    {
                        recentLines.Dequeue(); // Удаляем старейшую строку
                    }
                    recentLines.Enqueue(line); // Добавляем новую строку
                }
            }

            return string.Join("\n", recentLines);
        }

        public static async Task ServerEnded(MinecraftServer server)
        {
            serversOnline.Remove(server);
            await Task.CompletedTask;
        }




        //private bool CheckStarted()
        //{
        //    if (ServerConsoleProcess != null)
        //    {
        //        if (ServerConsoleProcess.HasExited)
        //        {
        //            ServerConsoleProcess = null;
        //            ServerController.Sendtype = SendType.Skip;
        //            return false;
        //        }
        //        return true;
        //    }
        //    var processes = Process.GetProcessesByName("cmd");
        //    if (processes.Length > 1)
        //    {
        //        ServerConsoleProcess = processes[0];
        //        Thread t = new Thread(() => ReadLogInTime(cts.Token));
        //        t.Start();
        //        Task.Run(CheckStartedServer);
        //        ServerController.Sendtype = SendType.Skip;
        //        return true;
        //    }
        //    return false;
        //}












    }

    public class MinecraftServer : IDisposable
    {
        public ServerState ServerState { get; private set; }
        public string Description { get; private set; }
        public string Name { get; private set; }
        public string OwnerUsername { get; private set; }

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

        private Process? ServerProcess { get; set; }
        private Process? ServerConsoleProcess { get; set; }

        private string consoleLogs = "Логи сервера появятся здесь...";
        private int players = 0;
        private float ramUsage = 0;
        private RCON? rcon = null;

        private readonly LogConnectionManager _logConnectionManager;
        private readonly CancellationTokenSource cts;

        public string Id { get; }
        public MinecraftVersion Version { get; }
        public string ServerPath { get; }
        public string LogPath { get; }
        public string TempLogPath { get; }
        public int PublicPort { get; }
        public int RCONPort { get; }
        public ServerCreation ServerCreation { get; }

        public MinecraftServer(string id, LogConnectionManager manager)
        {
            _logConnectionManager = manager;
            cts = new CancellationTokenSource();
            Id = id;

            MinecraftServerSpecifications specs = MinecraftServerManager.GetServerSpecs(id);
            Name = specs.Name;
            Description = specs.Description;
            Version = specs.Version;
            PublicPort = specs.PublicPort;
            RCONPort = specs.RCONPort;
            OwnerUsername = specs.OwnerName;

            ServerState = ServerState.starting;
            ServerPath = Path.Combine(Directory.GetCurrentDirectory(), "servers", Id);
            LogPath = Path.Combine(ServerPath, "logs", "latest.log");
            TempLogPath = Path.Combine(ServerPath, "logs", "temp.log");

        }

        //~MinecraftServer()
        //{
        //    cts!.Cancel();
        //}


        public async Task StartServer()
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
                await Task.Delay(2000);
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
            await ServerController.NotifyServerCrashed(Id);
            MinecraftServerManager.ServerEnded(this);
		}

		async Task MonitorLogAsync(string Id, string logPath, CancellationToken token)
        {

            if (!File.Exists(logPath))
            {
                Console.WriteLine($"Файл логов не найден: {logPath}");
                return;
            }

            Console.WriteLine($"Следим за логами: {logPath}");

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
                        if (ServerState != ServerState.started && line.Contains("Thread RCON Listener started"))
                        {
#if DEBUG
                            Task.Run(async () => { 
                                await ServerController.NotifyServerStarted(Id);
                                ServerState = ServerState.started;
                            });
#else
                            Task.Run(() => CheckStartedServer(token));
#endif
                        }
                        Console.WriteLine(line);
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

        private async void CheckStartedServer(CancellationToken token)
        {
            //await Task.Delay(7000);
            if (ServerProcess == null)
            {
                var processes = Process.GetProcessesByName("java");
                while (processes.Length < MinecraftServerManager.serversOnline.Count && !token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(100, token);
                        processes = Process.GetProcessesByName("java");
                    }
                    catch(Exception ex)
                    {
                        if (ex is not TaskCanceledException)
                            Console.WriteLine($"Error: {ex}");
                        return;
					}
                }
                if (processes.Length == MinecraftServerManager.serversOnline.Count)
                    ServerProcess = processes[MinecraftServerManager.serversOnline.Count - 1];
                ServerState = ServerState.started;
                rcon = new RCON(new IPEndPoint(IPAddress.Parse("192.168.31.204"), RCONPort), "gamemode1");
                Task.Run(() => StartClock(cts.Token));
                await ServerController.NotifyServerStarted(Id);
                //ServerController.Sendtype = SendType.Server;

            }
        }

        //        public async void OutputDataReceived(string? msg)
        //        {
        //            try
        //            {
        //                if (!string.IsNullOrEmpty(msg))
        //                {
        //                    if (ServerProcess == null)
        //                    {
        //                        if (msg.Contains("Thread RCON Listener started"))
        //                        {
        //                            CheckStartedServer();
        //                        }
        //                    }
        //                    if (!msg.Contains("ERROR"))
        //                    {
        //                        var hubContext = Helper.thisApp.Services.GetRequiredService<IHubContext<MinecraftLogHub>>();
        //                        await hubContext.Clients.All.SendAsync("ReceiveLog", msg);
        //                    }
        //                    consoleLogs += "\n" + msg;
        //                    return;
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine(ex.ToString());
        //            }
        //        }

        public async Task StopServer()
        {
            try
            {
                if (rcon == null) { return; }

                await rcon.SendCommandAsync("stop");
                //while(!ServerConsoleProcess.HasExited)
                //{
                //    await Task.Delay(1000);
                //}
                //ServerConsoleProcess = null;
                rcon = null;
                cts.Cancel();
                cts.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        //        private async void ReadLogInTime(CancellationToken token)
        //        {
        //            try
        //            {
        //                if (!File.Exists(LogPath))
        //                {
        //                    Console.WriteLine("Файл логов не найден.");
        //                    return;
        //                }
        //                await Task.Delay(2000);
        //                File.WriteAllText(TempLogPath, string.Empty);
        //                consoleLogs = "Логи появяться здесь...";
        //                CloneCycle(token);
        //                using (var fileStream = new FileStream(TempLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        //                using (var reader = new StreamReader(fileStream, Encoding.UTF8))
        //                {
        //                    //Catched:
        //                    Console.WriteLine("Чтение копии логов...");

        //                    // Переместить указатель на конец файла
        //                    fileStream.Seek(0, SeekOrigin.End);

        //                    while (true)
        //                    {
        //                        string? line = reader.ReadLine();
        //                        if (!string.IsNullOrEmpty(line))
        //                        {
        //                            OutputDataReceived(line);
        //                        }
        //                        else
        //                        {
        //                            await Task.Delay(100); // Пауза, если новых строк нет
        //                        }
        //                        if (token.IsCancellationRequested)
        //                        {
        //                            break;
        //                        }
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"{ex.Message}");
        //                //goto Catched;
        //            }
        //        }

        //        private async Task CloneCycle(CancellationToken token)
        //        {
        //            while (!token.IsCancellationRequested)
        //            {
        //                try
        //                {
        //                    if (token.IsCancellationRequested)
        //                    {
        //                        return;
        //                    }
        //                    File.Copy(LogPath, TempLogPath, true); // Копирование файла
        //                }
        //                catch (Exception ex)
        //                {
        //                    Console.WriteLine("Ошибка копирования файла: " + ex.Message);
        //                }
        //                await Task.Delay(1000, token);
        //            }
        //        }

        public async Task<string> SendCommandAsync(string command)
        {
            if (rcon == null) { return "сервер еще запускается"; }
            if (string.IsNullOrEmpty(command) || command.Contains("stop") || command.Contains("op") || command.Contains("deop") || command.Contains("gamemode") || command.Contains("summon") || command.Contains("give")) { return "ага, фигушки"; }

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
            while (!token.IsCancellationRequested)
            {
                try
                {

                    ServerProcess!.Refresh();
                    ramUsage = ServerProcess.WorkingSet64 / 1024 / 1024 - 78;
#if !DEBUG
                            string plRaw = await SendCommandAsync("list");
                            int.TryParse(new string(plRaw
                             .SkipWhile(x => !char.IsDigit(x))
                             .TakeWhile(x => char.IsDigit(x))
                             .ToArray()), out players);
#endif
                    await Task.Delay(5000, token);
                }
                catch (Exception ex) //when (ex is not TaskCanceledException)
				{
					if (ex is not TaskCanceledException)
						Console.WriteLine(ex.ToString());
                }
            }
        }
    }


    public class MinecraftServerBuilder
    {
        internal string Id { get; private set; }
        internal string Name { get; private set; }
        internal string? Description { get; private set; }
        public MinecraftServerBuilder AddId(string id)
        {
            Id = id;
            return this;
        }
        public MinecraftServerBuilder AddName(string name)
        {
            Name = name;
            return this;
        }
        public MinecraftServerBuilder AddDescription(string desc)
        {
            Description = desc;
            return this;
        }
        //public MinecraftServerBuilder AddOwnerUsername(string username)
        //{
        //    _server.OwnerUsername = username;
        //    return this;
        //}
        //public MinecraftServer Build()
        //{
        //    return new MinecraftServer(this);
        //}
    }

    //public class MinecraftLogHub : Hub
    //{
    //    public async Task SendLog(string message)
    //    {
    //        await Clients.All.SendAsync("ReceiveLog", message);
    //    }
    //}

    public enum Difficulty
    {
        peaceful,
        easy,
        normal,
        hard
    }

    public enum GameMode
    {
        survival,
        creative,
        adventure,
        spectrator
    }

    public enum MinecraftVersion
    {
        _1_12_2,
        _1_16_5,
        _1_19_2
    }

    interface IMinecraftServer
    {
        public string Id { get; set; }
        // public string LogPath { get; private set; } //@"C:\Users\nonam\AppData\Roaming\.minecraft\logs\latest.log";
        // public string TempLogPath { get; private set; } //@"C:\Users\nonam\AppData\Roaming\.minecraft\logs\temp.log";
        //public string OwnerUsername { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

    }

    public class PreferenceRequest
    {
        public string Preference { get; set; }
        public string Value { get; set; }
    }
}
