using Docker.DotNet;
using Docker.DotNet.Models;
using HomeSite.Entities;
using HomeSite.Generated;
using HomeSite.Helpers;
using HomeSite.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HomeSite.Managers
{
    public class MinecraftServerManager : IMinecraftServerManager
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public static readonly string folder = Path.Combine(Environment.CurrentDirectory, "servers");
        private static readonly string creatingsPath = Path.Combine(Environment.CurrentDirectory, "servers", "creatings.json");

        private readonly LogConnectionManager _logConnectionManager;
        private readonly IDbContextFactory<ServerDBContext> _contextFactory;
        private readonly IDockerClient _dockerClient;
        private readonly ILogger<MinecraftServerManager> _logger;

        public List<MinecraftServer> ServersOnline { get; }
        public Dictionary<string, ServerCreation> InCreation { get; }

        public MinecraftServerManager(
            LogConnectionManager logConnectionManager,
            IDbContextFactory<ServerDBContext> contextFactory,
            IDockerClient dockerClient,
            ILogger<MinecraftServerManager> logger,
            IHttpClientFactory httpClientFactory)
        {
            ServersOnline = new List<MinecraftServer>();
            InCreation = new Dictionary<string, ServerCreation>();
            _dockerClient = dockerClient;
            _logConnectionManager = logConnectionManager;
            _contextFactory = contextFactory;
            _logger = logger;
            _httpClientFactory = httpClientFactory;

            Initialize();
        }

        private void Initialize()
        {
            Task.Run(LoadServersInCreationAsync);
            Task.Run(StartGlobalEventMonitoring);
            Task.Run(RecoverServersAsync);
        }

        private async Task LoadServersInCreationAsync()
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            if (!File.Exists(creatingsPath))
            {
                InCreation.Clear();
                return;
            }

            var content = await File.ReadAllTextAsync(creatingsPath);
            var data = JsonConvert.DeserializeObject<Dictionary<string, ServerCreation>>(content)
                      ?? new Dictionary<string, ServerCreation>();
            foreach (var kv in data)
                InCreation[kv.Key] = kv.Value;
        }


        public async Task<string> CreateServer(
            string name,
            string ownerName,
            string serverCore,
            string? version,
            string? curseforgePackURL,
            string? description = null)
        {
            string genId = Guid.NewGuid().ToString();
            InCreation[genId] = ServerCreation.AddingMods;
            await SaveServersInCreation();

            string core = serverCore.ToUpperInvariant();
            string resolvedVersion = version ?? "LATEST";
            string? resolvedCurseforgePack = string.IsNullOrWhiteSpace(curseforgePackURL) ? null : curseforgePackURL.Trim();

            if (core == "CURSEFORGE")
            {
                if (string.IsNullOrWhiteSpace(resolvedCurseforgePack))
                    throw new InvalidOperationException("Не указан ID сборки CurseForge");

                var packInfo = await GetInfoFromUrlOrId(resolvedCurseforgePack);
                core = packInfo.Core;
                resolvedVersion = packInfo.Version;
            }

            var serverSpecs = new Server
            {
                Id = genId,
                Description = description,
                Name = name,
                Version = resolvedVersion,
                CurseforgePack = resolvedCurseforgePack,
                PortMappings = new List<PortMapping>
                {
                    new PortMapping { Port = 8080, Path = null }
                },
                ServerCore = core,
                DomainName = ownerName.ToLower()
            };

            await using var context = _contextFactory.CreateDbContext();
            context.Servers.Add(serverSpecs);
            await context.SaveChangesAsync();

            string serverPath = Path.Combine(folder, genId);
            if (!Directory.Exists(serverPath))
            {
                Directory.CreateDirectory(serverPath);
            }

            File.WriteAllText(
                Path.Combine(serverPath, "server.properties"),
                ServerPropertiesManager.DefaultServerProperties(description ?? "A Minecraft server"));

            return genId;
        }

        
        public async Task<bool> DeleteServer(string id)
        {
            if (ServersOnline.Any(x => x.Id == id))
                return false;

            try
            {
                await using var context = _contextFactory.CreateDbContext();
                var server = await context.Servers.FirstOrDefaultAsync(x => x.Id == id);
                if (server == null)
                    return false;

                context.Servers.Remove(server);
                await context.SaveChangesAsync();

                Directory.Delete(Path.Combine(folder, id), true);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return false;
            }
            return true;
        }

        public async Task SetServerDomain(string id, string newDomain)
        {
            await using var context = _contextFactory.CreateDbContext();
            var server = await context.Servers.FirstOrDefaultAsync(x => x.Id == id);
            if (server == null) return;
            server.DomainName = newDomain;
            await context.SaveChangesAsync();
        }

        public async Task SetServerMappings(string id, List<PortMapping> portMappings)
        {
            await using var context = _contextFactory.CreateDbContext();
            var server = await context.Servers.FirstOrDefaultAsync(x => x.Id == id);
            if (server == null) return;
            server.PortMappings = portMappings;
            await context.SaveChangesAsync();
        }

        public async Task SetServerDesc(string id, string newDesc)
        {
            var serverInMemory = ServersOnline.FirstOrDefault(x => x.Id == id);
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
            var serverInMemory = ServersOnline.FirstOrDefault(x => x.Id == id);
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

        private async Task StartGlobalEventMonitoring()
        {
            try
            {
                var progress = new Progress<Message>(async m =>
                {
                    if (m.Type == "container" && (m.Action == "die" || m.Action == "stop" || m.Action == "destroy"))
                    {
                        if (m.Actor.Attributes.TryGetValue("name", out string? fullContainerName))
                        {
                            string cleanName = fullContainerName.TrimStart('/');

                            var server = ServersOnline.FirstOrDefault(s => $"mc-{s.Id}" == cleanName);

                            if (server != null)
                            {
                                await server.OnContainerExited();
                                ServersOnline.Remove(server);
                                _logger.LogInformation($"Server {cleanName} removed from Online list.");
                            }
                        }
                    }
                });

                var eventsParams = new ContainerEventsParameters
                {
                    Filters = new Dictionary<string, IDictionary<string, bool>>
                    {
                        { "type", new Dictionary<string, bool> { { "container", true } } },
                        { "event", new Dictionary<string, bool>
                            {
                                { "die", true },
                                { "stop", true },
                                { "destroy", true } 
                            }
                        }
                    }
                };

                await _dockerClient.System.MonitorEventsAsync(eventsParams, progress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                await Task.Delay(5000);
                StartGlobalEventMonitoring();
            }
        }


        public async Task LaunchServer(string id)
        {
            var specs = await GetServerSpecs(id);
            if (specs == null) return;

            string hostRoot = Environment.GetEnvironmentVariable("HOST_SERVERS_PATH") ?? "/app/servers";
            string realPathOnDisk = Path.Combine(hostRoot, id);

            specs.PortMappings.Sort((x, y) =>
            {
                if (x.Path == y.Path) return 0;
                if (x.Path == null) return 1;
                if (y.Path == null) return -1;
                return string.Compare(x.Path, y.Path, StringComparison.Ordinal);
            });
            string filepath = Path.Combine(folder, id, "server.properties");
            await ServerPropertiesManager.EditProperty(filepath, "rcon.password", ConfigManager.RCONPassword!);
            await ServerPropertiesManager.EditProperty(filepath, "enable-rcon", true);

            var labels = new Dictionary<string, string>
            {
                    { "com.docker.compose.project", "mc-servers-farm" },
                    { "com.docker.compose.service", "minecraft-instance" },
                    { "com.docker.compose.version", "1.0.0" },
                    { "mc-router.port", "25565" },
                                
#if DEBUG
                    { "caddy", $"http://{specs.DomainName}.vcap.me" },
                    { "mc-router.host", $"{specs.DomainName}.vcap.me" }
#else
                    { "caddy", $"http://{specs.DomainName}.{ConfigManager.Domain}" },
                    { "mc-router.host", $"{specs.DomainName}.{ConfigManager.Domain}" }
#endif    
            };
            int i = 0;
            foreach(var portmapping in specs.PortMappings)
            {
                var cleanPath = portmapping.Path?.Trim('/', '*');
                if (portmapping.Path != null)
                {
                    labels.Add($"caddy.redir_"+ i, $"/{portmapping.Path} /{portmapping.Path}/ 308");
                    labels.Add("caddy.handle_path_" + i, '/' + portmapping.Path + '*');
                    labels.Add($"caddy.handle_path_{i}.reverse_proxy", "{{upstreams " + portmapping.Port + "}}");
                    i++;
                }
                else
                {
                    labels.Add("caddy.handle", "*" );
                    labels.Add($"caddy.handle.reverse_proxy", "{{upstreams " + portmapping.Port + "}}");
                }
            }

            string imageTag = Helper.GetDockerImageTag(specs.Version);

            var env = new List<string>
            {
                "EULA=TRUE",
                "MEMORY=4G",
                "ENABLE_RCON=false",
                "OVERRIDE_SERVER_PROPERTIES=false"
            };

            if (specs.CurseforgePack != null)
            {
                if (string.IsNullOrWhiteSpace(ConfigManager.CurseforgeApiKey))
                    throw new InvalidOperationException("invalid CurseforgeApiKey");

                env.Add("TYPE=AUTO_CURSEFORGE");
                env.Add($"CF_PAGE_URL={specs.CurseforgePack}");
                env.Add($"CF_API_KEY={ConfigManager.CurseforgeApiKey}");
            }
            else
            {
                env.Add($"TYPE={specs.ServerCore.ToUpper()}");
                env.Add($"VERSION={specs.Version ?? "latest"}");
            }

            var createParams = new CreateContainerParameters
            {
                Image = $"itzg/minecraft-server:{imageTag}",
                Name = $"mc-{id}",
                User = "root",
                Labels = labels,
                Env = env,
                ExposedPorts = new Dictionary<string, EmptyStruct>
                {
                    { "19132/udp", new EmptyStruct() }
                },
                HostConfig = new HostConfig
                {
                    AutoRemove = true,
                    NetworkMode = "mc_network",
                    Binds = new List<string> { $"{realPathOnDisk}:/data" },
                    RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.No },
                    Memory = 4831838208,
                    PortBindings = specs.BedrockPort == 0 ? null :
                    new Dictionary<string, IList<PortBinding>>
                    {
                        {
                            "19132/udp",
                            new List<PortBinding>
                            {
                                new()
                                { 
                                    HostPort = specs.BedrockPort.ToString()
                                }
                            }
                        }
                    }
                }
            };

            try
            {
                try
                {
                    var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });
                    var container = containers.FirstOrDefault(c => c.Names.Any(n => n.Equals($"/mc-{id}", StringComparison.OrdinalIgnoreCase)));

                    if (container != null)
                    {
                        string dockerId = container.ID;

                        await _dockerClient.Containers.RemoveContainerAsync(dockerId, new ContainerRemoveParameters { Force = true });
                    }
                }
                catch (DockerContainerNotFoundException)
                { } 
                var response = await _dockerClient.Containers.CreateContainerAsync(createParams);
                await _dockerClient.Containers.StartContainerAsync(response.ID, null);

                var minecraftServer = new MinecraftServer(specs, _logConnectionManager, _dockerClient);
                ServersOnline.Add(minecraftServer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }

        private async Task RecoverServersAsync()
        {
            var allContainers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });

            var containers = allContainers
                .Where(c => c.Labels.TryGetValue("com.docker.compose.project", out var project)
                            && project == "mc-servers-farm")
                .ToList();

            foreach (var container in containers)
            {
                string serverId = container.Names.First()[4..];

                Server? specs = await GetServerSpecs(serverId);
                MinecraftServer recoveredServer = new MinecraftServer(specs!, _logConnectionManager, _dockerClient);

                ServersOnline.Add(recoveredServer);
                _logger.LogInformation($"Recovered server {specs?.Name} with ID {serverId}");
            }
        }

        //private async Task ServerEnded(MinecraftServer server)
        //{

        //}

        private async Task SaveServersInCreation()
        {
            string servers = JsonConvert.SerializeObject(InCreation, Formatting.Indented);
            await File.WriteAllTextAsync(creatingsPath, servers);
        }

        public async Task<bool> FinishServerCreation(string Id)
        {
            InCreation.Remove(Id);
            await SaveServersInCreation();
            return true;
        }

        public ServerCreation GetServerCreation(string Id)
        {
            if (!InCreation.ContainsKey(Id))
            {
                return ServerCreation.Created;
            }
            return InCreation[Id];

        }

        private async Task<Dictionary<string, ServerCreation>> GetServersInCreation()
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
            if (!File.Exists(Path.Combine(folder, Id, "logs", "latest.log")))
                return "логов нет";
            Queue<string> recentLines = new Queue<string>(10);

            using (StreamReader reader = new StreamReader(Path.Combine(folder, Id, "logs", "latest.log")))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (recentLines.Count >= 10)
                    {
                        recentLines.Dequeue();
                    }
                    recentLines.Enqueue(line);
                }
            }

            return string.Join("\n", recentLines);
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

        private async Task<(string Core, string Version)> GetInfoFromUrlOrId(string input)
        {
            var fileIdMatch = Regex.Match(input, @"/files/(\d+)($|\?)");
            if (fileIdMatch.Success)
            {
                return await GetCurseforgePackInfo(fileIdMatch.Groups[1].Value);
            }

            var slugMatch = Regex.Match(input, @"/modpacks/([^/\?]+)");
            if (slugMatch.Success)
            {
                string slug = slugMatch.Groups[1].Value;
                string latestFileId = await GetLatestFileIdBySlug(slug);
                return await GetCurseforgePackInfo(latestFileId);
            }

            if (Regex.IsMatch(input, @"^\d+$"))
            {
                return await GetCurseforgePackInfo(input);
            }

            throw new ArgumentException("Invalid link format or ID CurseForge");
        }

        private async Task<string> GetLatestFileIdBySlug(string slug)
        {
            var client = _httpClientFactory.CreateClient();
            string url = $"https://api.curseforge.com/v1/mods/search?gameId=432&slug={slug}&classId=4471";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("x-api-key", ConfigManager.CurseforgeApiKey);

            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException("Error searching Slug");

            var json = await response.Content.ReadAsStringAsync();
            var root = JObject.Parse(json);

            var modData = root["data"]?[0];
            if (modData == null)
                throw new InvalidOperationException("Pack not found");

            return modData["latestFiles"]?[0]?["id"]?.ToString()
                   ?? throw new InvalidOperationException("Pack does not have public files");
        }

        private async Task<(string Core, string Version)> GetCurseforgePackInfo(string fileId)
        {
            if (string.IsNullOrWhiteSpace(ConfigManager.CurseforgeApiKey))
                throw new InvalidOperationException("invalid CurseforgeApiKey");

            var client = _httpClientFactory.CreateClient();

            var url = "https://api.curseforge.com/v1/mods/files";

            var requestBody = new { fileIds = new[] { int.Parse(fileId) } };

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-api-key", ConfigManager.CurseforgeApiKey);
            request.Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            using var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"CF API Error {response.StatusCode}: {errorBody}");
            }

            var rawJson = await response.Content.ReadAsStringAsync();
            var root = JObject.Parse(rawJson);

            var data = root["data"]?[0] as JObject
                       ?? throw new InvalidOperationException("File not found");

            var gameVersions = data["gameVersions"]?.Values<string>()
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList() ?? new List<string>();

            string mcVersion = gameVersions.FirstOrDefault(v => Regex.IsMatch(v, @"^\d+\.\d+(\.\d+)?$")) ?? "LATEST";
            string core = DetectCore(gameVersions);

            return (core, mcVersion);
        }

        private static string DetectCore(IEnumerable<string> gameVersions)
        {
            if (gameVersions.Any(v => v.Contains("NeoForge", StringComparison.OrdinalIgnoreCase))) return "NEOFORGE";
            if (gameVersions.Any(v => v.Contains("Forge", StringComparison.OrdinalIgnoreCase))) return "FORGE";
            if (gameVersions.Any(v => v.Contains("Fabric", StringComparison.OrdinalIgnoreCase))) return "FABRIC";
            if (gameVersions.Any(v => v.Contains("Quilt", StringComparison.OrdinalIgnoreCase))) return "QUILT";
            return "VANILLA";
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
