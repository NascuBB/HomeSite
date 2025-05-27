using Newtonsoft.Json;

namespace HomeSite.Managers
{
    public class ConfigManager
    {
        public static string? SMTPKey { get; private set; }
        public static string? Domain { get; private set; }
        public static string? RealEmail { get; private set; }
        private static readonly string configPath = Path.Combine(Directory.GetCurrentDirectory(), "Сonfig");
        private static readonly string path = Path.Combine(configPath, "config.json");

        public static void GetApiKeys()
        {
            if (!Directory.Exists(configPath))
                Directory.CreateDirectory(configPath);

            Dictionary<string, string> data;

            if (!File.Exists(path))
            {
                data = new Dictionary<string, string>
                {
                    { "SMTPkey", "key" },
                    { "Domain", "domain"},
                    { "RealEmail", "mail" },
                };
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(path, json);

                SMTPKey = "key";
                Domain = "domain";
                RealEmail = "mail";
                return;
            }
            string fileContent = File.ReadAllText(path);
            data = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileContent)!;

            SMTPKey = data["SMTPkey"];
            Domain = data["Domain"];
            RealEmail = data["RealEmail"];
        }
    }
}
