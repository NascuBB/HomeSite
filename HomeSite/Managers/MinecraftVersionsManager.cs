using HomeSite.Entities;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using static System.Formats.Asn1.AsnWriter;

namespace HomeSite.Managers
{
    public class MinecraftVersionsManager
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Timer _updateTimer;
        private readonly HttpClient _httpClient;
        private readonly ILogger<MinecraftVersionsManager> _logger;

        private Dictionary<string, List<string>> _versions = new();

        public MinecraftVersionsManager(IServiceScopeFactory scopeFactory, ILogger<MinecraftVersionsManager> logger, HttpClient httpClient)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _httpClient = httpClient;
            _updateTimer = new Timer(_ => UpdateVersions(), null, TimeSpan.Zero, TimeSpan.FromHours(24));
        }

        public async void UpdateVersions()
        {
            _logger.LogInformation("Updating versions from APIs...");
            var newCache = new Dictionary<string, List<string>>();
            string[] types = { "VANILLA", "PAPER", "PURPUR", "FABRIC", "FORGE" };

            foreach (var type in types)
            {
                try
                {
                    List<string> versions = type switch
                    {
                        "VANILLA" => await GetVanillaVersions(),
                        "PAPER" => await GetPaperVersions(),
                        "PURPUR" => await GetPurpurVersions(),
                        "FABRIC" => await GetFabricVersions(),
                        "FORGE" => await GetForgeVersions(),
                        _ => new List<string>()
                    };

                    if (versions.Any())
                    {
                        newCache[type] = versions;
                        await SaveToDb(type, versions);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"API error for core {type}, getting versions from db. Error: {ex.Message}");

                    var dbVersions = await GetVersionsFromDb(type);
                    if (dbVersions.Any())
                    {
                        newCache[type] = dbVersions;
                    }
                    else
                    {
                        newCache[type] = new List<string> { "LATEST" };
                    }
                }
            }

            _versions = newCache;
        }

        private async Task SaveToDb(string type, List<string> versions)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MinecraftVersionDBContext>();

                var existing = await db.Versions
                    .Where(v => v.Core == type)
                    .Select(v => v.VersionNumber)
                    .ToListAsync();

                var newVersions = versions.Except(existing).Select(name => new MinecraftVersion
                {
                    Core = type,
                    VersionNumber = name
                });

                if (newVersions.Any())
                {
                    db.Versions.AddRange(newVersions);
                    await db.SaveChangesAsync();
                }
            }
        }

        private async Task<List<string>> GetVersionsFromDb(string type)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MinecraftVersionDBContext>();
                return await db.Versions
                    .Where(v => v.Core == type.ToUpper())
                    .OrderByDescending(v => v.VersionNumber)
                    .Select(v => v.VersionNumber)
                    .ToListAsync();
            }
        }

        public List<string> GetVersions(string type)
        => _versions.TryGetValue(type.ToUpper(), out var versions) ? versions : new List<string>();

        private async Task<List<string>> GetVanillaVersions()
        {
            var res = await _httpClient.GetFromJsonAsync<MojangManifest>("https://piston-meta.mojang.com/mc/game/version_manifest.json");
            return res.Versions.Where(v => v.Type == "release").Select(v => v.Id).ToList();
        }

        private async Task<List<string>> GetPaperVersions()
        {
            var res = await _httpClient.GetFromJsonAsync<PaperProject>("https://api.papermc.io/v2/projects/paper");
            res.Versions.Reverse();
            return res.Versions;
        }

        private async Task<List<string>> GetPurpurVersions()
        {
            var res = await _httpClient.GetFromJsonAsync<PurpurProject>("https://api.purpurmc.org/v2/purpur");
            res.Versions.Reverse();
            return res.Versions;
        }
        private async Task<List<string>> GetForgeVersions()
        {
            var res = await _httpClient.GetFromJsonAsync<ForgeManifest>("https://files.minecraftforge.net/net/minecraftforge/forge/promotions_slim.json");

            var r2 = res.Promos.Keys
                .Select(k => k.Split('-')[0])
                .Distinct()
                .ToList();

            r2.Reverse();
            return r2;
        }

        private async Task<List<string>> GetFabricVersions()
        {
            var res = await _httpClient.GetFromJsonAsync<List<FabricGameVersion>>("https://meta.fabricmc.net/v2/versions/game");

            return res.Where(v => v.Stable).Select(v => v.Version).ToList();
        }

        public record FabricGameVersion(string Version, bool Stable);
        public record ForgeManifest(Dictionary<string, string> Promos);
        public record MojangManifest(List<MojangVersion> Versions);
        public record MojangVersion(string Id, string Type);
        public record PaperProject(List<string> Versions);
        public record PurpurProject(List<string> Versions);
    }
}
