using HomeSite.Helpers;
using HomeSite.Models;
using Newtonsoft.Json;
using HomeSite.Entities;
using HomeSite.Generated;

namespace HomeSite.Managers
{
    public class MinecraftServerManager : IMinecraftServerManager
    {
        public static readonly string folder = Path.Combine(Environment.CurrentDirectory, "servers");
        private static readonly string versionsFolder = Path.Combine(Environment.CurrentDirectory, "versions");
        private static readonly string creatingsPath = Path.Combine(Environment.CurrentDirectory, "servers", "creatings.json");

        public static List<MinecraftServer> serversOnline = new List<MinecraftServer>();
        public static Dictionary<string, ServerCreation> inCreation = new Dictionary<string, ServerCreation>();

        private static Dictionary<int, int> availablePorts = new Dictionary<int, int>{
            { 25550, 5000 }, { 25551, 5001 }, { 25552, 5002 }, { 25553, 5003 }, { 25554, 5004 },
            { 25555, 5005 }, { 25556, 5006 }, { 25557, 5007 }, { 25558, 5008 }, { 25559, 5009 },
            { 25560, 5010 }, { 25561, 5011 }, { 25562, 5012 }, { 25563, 5013 }, { 25564, 5014 },
            { 25565, 5015 }, { 25566, 5016 }, { 25567, 5017 }, { 25568, 5018 }, { 25569, 5019 },
            { 25570, 5020 }
        };

        private readonly LogConnectionManager _logConnectionManager;
        private readonly ServerDBContext _serverContext;

        //private static Dictionary<string, MinecraftServerSpecifications> serverMainSpecs = new Dictionary<string, MinecraftServerSpecifications>();

        public MinecraftServerManager(LogConnectionManager logConnectionManager, ServerDBContext serverContext)
        {
            _logConnectionManager = logConnectionManager;
            _serverContext = serverContext;
            Prepare();
        }

        private void Prepare()
        {
            Task.Run(UpdateAvailablePorts);
            Task.Run(async () => { inCreation = await GetServersInCreation(); });
        }

        void UpdateAvailablePorts()
        {
            var servers = _serverContext.Servers.ToList();
            
            foreach (var serverSpec in servers)
            {
                if(availablePorts.ContainsKey(serverSpec.PublicPort))
                {
                    availablePorts.Remove(serverSpec.PublicPort);
                }
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

        /// <summary>
        /// creates server folder and returns id of new created server
        /// </summary>
        /// <param name="name">name of server</param>
        /// <param name="ownerName">username of owner</param>
        /// <param name="version">version of server</param>
        /// <param name="description">description to server</param>
        /// <returns></returns>
        public async Task<string> CreateServer(string name, string ownerName, ServerCore serverCore, MinecraftVersion version, string? description = null)
        {
            string genId = Guid.NewGuid().ToString();
            inCreation.Add(genId, ServerCreation.AddingMods);
            await SaveServersInCreation();
            Random r = new();
            int port = availablePorts.Keys.ElementAt(r.Next(availablePorts.Count));
            int rconP = availablePorts[port];
            availablePorts.Remove(port);
            Server serverSpecs = new Server
            {
                Id = genId,
                Description = description,
                Name = name,
                //OwnerName = ownerName,
                Version = version,
                PublicPort = port,
                RCONPort = rconP,
                ServerCore = serverCore
            };
            _serverContext.Servers.Add(serverSpecs);
            _serverContext.SaveChanges();
            //serverMainSpecs.Add(genId, serverSpecs);
            //TODO
            //await SaveServersSpecs();
            Helper.Copy(Path.Combine(versionsFolder, serverCore.ToString(), VersionHelperGenerated.GetVersion(version)), Path.Combine(folder, genId));
            File.WriteAllText(Path.Combine(folder, genId, "server.properties"), ServerPropertiesManager.DefaultServerProperties(port, rconP, description ?? "A Minecraft server"));
            return genId;
        }
        /// <summary>
        /// Deletes minecraft server
        /// </summary>
        /// <param name="Id">Id of server</param>
        /// <returns></returns>
        public async Task<bool> DeleteServer(string Id)
        {
            if (serversOnline.Any(x => x.Id == Id))
            {
                return false;
            }

            try
            {
                //serverMainSpecs.Remove(Id);
                //TODO
                //await SaveServersSpecs();
                Server server = new() { Id = Id };
                _serverContext.Servers.Attach(server);
                _serverContext.Servers.Remove(server);
                await _serverContext.SaveChangesAsync();
                Directory.Delete(Path.Combine(folder, Id), true);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
            return true;
        }
        /// <summary>
        /// Sets new description to server
        /// </summary>
        /// <param name="Id">Id of server</param>
        /// <param name="newValue">new description</param>
        /// <returns></returns>
        public async Task SetServerDesc(string Id, string newValue)
        {
            //serverMainSpecs[Id].Description = newValue;
            if(serversOnline.Any(x => x.Id == Id))
            {
                serversOnline.First(x => x.Id == Id).Description = newValue;
            }
            //TODO
            Server? server = _serverContext.Servers.Find(Id);
            if (server == null) { return; }
            server.Description = newValue;
            await _serverContext.SaveChangesAsync();
        }
        /// <summary>
        /// Sets new name to server
        /// </summary>
        /// <param name="Id">Id of server</param>
        /// <param name="newValue">new name</param>
        /// <returns></returns>
        public async Task SetServerName(string Id, string newValue)
		{
			//serverMainSpecs[Id].Name = newValue;
			if (serversOnline.Any(x => x.Id == Id))
			{
				serversOnline.First(x => x.Id == Id).Name = newValue;
			}
            //TODO
            Server? server = _serverContext.Servers.Find(Id);
            if (server == null) { return; }
            server.Name = newValue;
            await _serverContext.SaveChangesAsync();
        }

        /// <summary>
        /// Get server specifications
        /// </summary>
        /// <param name="id">Id of server</param>
        /// <returns><see cref="Server"/> entity of requested minecraft server</returns>
        public Server GetServerSpecs(string id)
        {
            return _serverContext.Servers.First(x => x.Id == id);
        }

        //public static bool IsOwner(string serverId, string username)
        //{
        //    using (var serverContext = new ServerDBContext())
        //    {
        //        using (var userContext = new UserDBContext())
        //        {
        //            return serverContext.servers.Any(x => x.userid == userContext.useraccounts.First(x => x.username == username).id);
        //        }
        //    }
        //}
        /// <summary>
        /// Check if server exists
        /// </summary>
        /// <param name="Id">Id of server</param>
        /// <returns><see cref="bool"/> true if server exists, oterwise false</returns>
        public bool ServerExists(string Id)
        {
            return _serverContext.Servers.Any(x => x.Id == Id);
        }

        public void LaunchServer(string Id)
        {
            MinecraftServer minecraftServer = new MinecraftServer(Id, _logConnectionManager, _serverContext);
            serversOnline.Add(minecraftServer);
            minecraftServer.StartServer();
        }
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
