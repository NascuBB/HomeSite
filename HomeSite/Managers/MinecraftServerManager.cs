using HomeSite.Helpers;
using HomeSite.Models;
using Newtonsoft.Json;
using HomeSite.Entities;
using HomeSite.Generated;
using Microsoft.EntityFrameworkCore;

namespace HomeSite.Managers
{
    public class MinecraftServerManager : IMinecraftServerManager
    {
        public static readonly string folder = Path.Combine(Environment.CurrentDirectory, "servers");
        private static readonly string versionsFolder = Path.Combine(Environment.CurrentDirectory, "versions");
        private static readonly string creatingsPath = Path.Combine(Environment.CurrentDirectory, "servers", "creatings.json");

        private readonly LogConnectionManager _logConnectionManager;
        private readonly IDbContextFactory<ServerDBContext> _contextFactory;

        // Состояния без контекста
        public static List<MinecraftServer> serversOnline { get; } = new();
        public static Dictionary<string, ServerCreation> inCreation { get; } = new();
        private static Dictionary<int, int> availablePorts = new Dictionary<int, int>
        {
            { 25550, 5000 }, { 25551, 5001 }, { 25552, 5002 }, { 25553, 5003 }, { 25554, 5004 },
            { 25555, 5005 }, { 25556, 5006 }, { 25557, 5007 }, { 25558, 5008 }, { 25559, 5009 },
            { 25560, 5010 }, { 25561, 5011 }, { 25562, 5012 }, { 25563, 5013 }, { 25564, 5014 },
            { 25565, 5015 }, { 25566, 5016 }, { 25567, 5017 }, { 25568, 5018 }, { 25569, 5019 },
            { 25570, 5020 }
        };
        public static readonly string Folder = Path.Combine(Environment.CurrentDirectory, "servers");
        private static readonly string VersionsFolder = Path.Combine(Environment.CurrentDirectory, "versions");
        private static readonly string CreatingsPath = Path.Combine(Folder, "creatings.json");

        public MinecraftServerManager(
            LogConnectionManager logConnectionManager,
            IDbContextFactory<ServerDBContext> contextFactory)
        {
            _logConnectionManager = logConnectionManager;
            _contextFactory = contextFactory;

            // Загружаем порты без скоупа
            _ = Task.Run(UpdateAvailablePortsAsync);
            _ = Task.Run(LoadServersInCreationAsync);
        }

        private async Task UpdateAvailablePortsAsync()
        {
            await using var context = _contextFactory.CreateDbContext();
            var servers = await context.Servers.AsNoTracking().ToListAsync();
            foreach (var serverSpec in servers)
            {
                availablePorts.Remove(serverSpec.PublicPort);
            }
        }

        private async Task LoadServersInCreationAsync()
        {
            if (!Directory.Exists(Folder))
                Directory.CreateDirectory(Folder);

            if (!File.Exists(CreatingsPath))
            {
                inCreation.Clear();
                return;
            }

            var content = await File.ReadAllTextAsync(CreatingsPath);
            var data = JsonConvert.DeserializeObject<Dictionary<string, ServerCreation>>(content)
                      ?? new Dictionary<string, ServerCreation>();
            foreach (var kv in data)
                inCreation[kv.Key] = kv.Value;
        }

        public async Task<string> CreateServer(string name, string ownerName, ServerCore serverCore, MinecraftVersion version, string? description = null)
        {
            string genId = Guid.NewGuid().ToString();

            inCreation[genId] = ServerCreation.AddingMods;
            await SaveServersInCreationAsync();

            Random r = new();
            int port = availablePorts.Keys.ElementAt(r.Next(availablePorts.Count));
            int rconP = availablePorts[port];
            availablePorts.Remove(port);

            var serverSpecs = new Server
            {
                Id = genId,
                Description = description,
                Name = name,
                Version = version,
                PublicPort = port,
                RCONPort = rconP,
                ServerCore = serverCore
            };

            await using var context = _contextFactory.CreateDbContext();
            context.Servers.Add(serverSpecs);
            await context.SaveChangesAsync();

            Helper.Copy(
                Path.Combine(VersionsFolder, serverCore.ToString(), VersionHelperGenerated.GetVersion(version)),
                Path.Combine(Folder, genId));
            File.WriteAllText(
                Path.Combine(Folder, genId, "server.properties"),
                ServerPropertiesManager.DefaultServerProperties(port, rconP, description ?? "A Minecraft server"));

            return genId;
        }

        public async Task<bool> DeleteServer(string id)
        {
            if (serversOnline.Any(x => x.Id == id))
                return false;

            try
            {
                await using var context = _contextFactory.CreateDbContext();
                var server = await context.Servers.FirstOrDefaultAsync(x => x.Id == id);
                if (server == null)
                    return false;

                context.Servers.Remove(server);
                await context.SaveChangesAsync();

                Directory.Delete(Path.Combine(Folder, id), true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
            return true;
        }

        public async Task SetServerDesc(string id, string newDesc)
        {
            var serverInMemory = serversOnline.FirstOrDefault(x => x.Id == id);
            if (serverInMemory != null)
            {
                serverInMemory.Description = newDesc;
            }

            await using var context = _contextFactory.CreateDbContext();
            var server = await context.Servers.FirstOrDefaultAsync(x => x.Id == id);
            if (server == null) return;
            server.Description = newDesc;
            await context.SaveChangesAsync();
        }

        public async Task SetServerName(string id, string newName)
        {
            var serverInMemory = serversOnline.FirstOrDefault(x => x.Id == id);
            if (serverInMemory != null)
            {
                serverInMemory.Name = newName;
            }

            await using var context = _contextFactory.CreateDbContext();
            var server = await context.Servers.FirstOrDefaultAsync(x => x.Id == id);
            if (server == null) return;
            server.Name = newName;
            await context.SaveChangesAsync();
        }

        public async Task<bool> ServerExists(string id)
        {
            await using var context = _contextFactory.CreateDbContext();
            return await context.Servers.AnyAsync(x => x.Id == id);
        }

        public async Task<Server?> GetServerSpecs(string id)
        {
            await using var context = _contextFactory.CreateDbContext();
            return await context.Servers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        }

        public void LaunchServer(string id)
        {
            var minecraftServer = new MinecraftServer(id, _logConnectionManager, _contextFactory);
            // MinecraftServer больше не получает context — он сам обращается при надобности через менеджер
            serversOnline.Add(minecraftServer);
            minecraftServer.StartServer();
        }

        public static async Task ServerEnded(MinecraftServer server)
        {
            serversOnline.Remove(server);
            await Task.CompletedTask;
        }

        private static async Task SaveServersInCreation()
        {
            string servers = JsonConvert.SerializeObject(inCreation, Formatting.Indented);
            await File.WriteAllTextAsync(creatingsPath, servers);
        }

        public static async Task<bool> FinishServerCreation(string Id)
        {
            inCreation.Remove(Id);
            await SaveServersInCreation();
            return true;
        }

        public static ServerCreation GetServerCreation(string Id)
        {
            if (!inCreation.ContainsKey(Id))
            {
                return ServerCreation.Created;
            }
            return inCreation[Id];

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

        private static async Task SaveServersInCreationAsync()
        {
            var json = JsonConvert.SerializeObject(inCreation, Formatting.Indented);
            await File.WriteAllTextAsync(CreatingsPath, json);
        }

        public static string GetDifficulty(Difficulty difficulty)
        {
            switch (difficulty)
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
                case "easy": return Difficulty.easy;
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

        // Утилитарные методы для GetGameMode/GetDifficulty можно оставить static без изменений.
    }


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

    //public enum MinecraftVersion
    //{
    //    _1_12_2,
    //    _1_16_5,
    //    _1_19_2
    //}

    //public enum ServerCore
    //{
    //    paper,
    //    forge
    //}

    interface IMinecraftServer
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

    }

    public class PreferenceRequest
    {
        public string Preference { get; set; }
        public string Value { get; set; }
    }
}
