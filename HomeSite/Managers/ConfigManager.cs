using Newtonsoft.Json;

namespace HomeSite.Managers
{
    public class ConfigManager
    {
        public static string? SMTPKey { get; private set; }
        public static string? Domain { get; private set; }
        public static string? RealEmail { get; private set; }
        public static string? RCONPassword { get; private set; }
        public static string? LocalAddress { get; private set; }
        public static string? CurseforgeApiKey { get; private set; }

        private static readonly string configPath = Path.Combine(Directory.GetCurrentDirectory(), "Config");
        private static readonly string path = Path.Combine(configPath, "config.json");

        public static void GetConfiguration()
        {
            if (!Directory.Exists(configPath))
                Directory.CreateDirectory(configPath);

            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "SMTPkey", "key" },
                { "Domain", "domain" },
                { "RealEmail", "mail" },
                { "RCONPassword", "youshallnotpass" },
                { "LocalAddress", "192.168.31.204" },
                { "CurseforgeApiKey", "put-your-curseforge-api-key-here" }
            };

            if (!File.Exists(path))
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(path, json);

                SMTPKey = data["SMTPkey"];
                Domain = data["Domain"];
                RealEmail = data["RealEmail"];
                RCONPassword = data["RCONPassword"];
                LocalAddress = data["LocalAddress"];
                CurseforgeApiKey = data["CurseforgeApiKey"];
                return;
            }

            string fileContent = File.ReadAllText(path);
            data = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileContent) ?? new Dictionary<string, string>();

            SMTPKey = data.TryGetValue("SMTPkey", out var smtp) ? smtp : "key";
            Domain = data.TryGetValue("Domain", out var domain) ? domain : "domain";
            RealEmail = data.TryGetValue("RealEmail", out var email) ? email : "mail";
            RCONPassword = data.TryGetValue("RCONPassword", out var rcon) ? rcon : "youshallnotpass";
            LocalAddress = data.TryGetValue("LocalAddress", out var local) ? local : "192.168.31.204";
            CurseforgeApiKey = data.TryGetValue("CurseforgeApiKey", out var cfKey) ? cfKey : null;
        }
    }
}
