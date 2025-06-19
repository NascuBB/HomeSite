namespace HomeSite.Managers
{
    public class ServerPropertiesManager
    {
        public static string DefaultServerProperties(int port, int rconport, string description = "A Minecraft Server")
        {
            return $"enable-jmx-monitoring=false\r\n" +
                $"rcon.port={rconport}\r\n" +
                $"level-seed=\r\n" +
                $"gamemode=survival\r\n" +
                $"enable-command-block=false\r\n" +
                $"enable-query=false\r\n" +
                $"generator-settings={{}}\r\n" +
                $"enforce-secure-profile=true\r\n" +
                $"level-name=world\r\n" +
                $"motd={description}\r\n" +
                $"query.port={port}\r\n" +
                $"pvp=true\r\n" +
                $"generate-structures=true\r\n" +
                $"max-chained-neighbor-updates=1000000\r\n" +
                $"difficulty=normal\r\n" +
                $"network-compression-threshold=256\r\n" +
                $"max-tick-time=60000\r\n" +
                $"require-resource-pack=false\r\n" +
                $"use-native-transport=true\r\n" +
                $"max-players=20\r\n" +
                $"online-mode=true\r\n" +
                $"enable-status=true\r\n" +
                $"allow-flight=false\r\n" +
                $"initial-disabled-packs=\r\n" +
                $"broadcast-rcon-to-ops=true\r\n" +
                $"view-distance=10\r\n" +
                $"server-ip={ConfigManager.LocalAddress!}\r\n" +
                $"resource-pack-prompt=\r\n" +
                $"allow-nether=true\r\n" +
                $"server-port={port}\r\n" +
                $"enable-rcon=true\r\n" +
                $"sync-chunk-writes=true\r\n" +
                $"op-permission-level=4\r\n" +
                $"prevent-proxy-connections=false\r\n" +
                $"hide-online-players=false\r\n" +
                $"resource-pack=\r\n" +
                $"entity-broadcast-range-percentage=100\r\n" +
                $"simulation-distance=10\r\n" +
                $"rcon.password={ConfigManager.RCONPassword!}\r\n" +
                $"player-idle-timeout=0\r\n" +
                $"force-gamemode=false\r\n" +
                $"rate-limit=0\r\n" +
                $"hardcore=false\r\n" +
                $"white-list=true\r\n" +
                $"broadcast-console-to-ops=true\r\n" +
                $"spawn-npcs=true\r\n" +
                $"spawn-animals=true\r\n" +
                $"log-ips=true\r\n" +
                $"function-permission-level=2\r\n" +
                $"initial-enabled-packs=vanilla\r\n" +
                $"level-type=minecraft\\:normal\r\n" +
                $"text-filtering-config=\r\n" +
                $"spawn-monsters=true\r\n" +
                $"enforce-whitelist=false\r\n" +
                $"spawn-protection=16\r\n" +
                $"resource-pack-sha1=\r\n" +
                $"max-world-size=29999984";
        }

        public static async Task<bool> EditProperty<T>(string path, string preference, T value)
        {
            try
            {
                if (!File.Exists(path))
                    return false;

                string[] lines = await File.ReadAllLinesAsync(path);
                bool found = false;
                string newLine = $"{preference}={value}";

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith(preference + "=", StringComparison.OrdinalIgnoreCase))
                    {
                        lines[i] = newLine;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    List<string> updatedLines = lines.ToList();
                    updatedLines.Add(newLine);
                    lines = updatedLines.ToArray();
                }

                await File.WriteAllLinesAsync(path, lines);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<T?> GetProperty<T>(string path, string preference)
        {
            if (!File.Exists(path))
                return default;

            try
            {
                using var reader = new StreamReader(path);
                string? line;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (line.StartsWith(preference + "=", StringComparison.OrdinalIgnoreCase))
                    {
                        string value = line.Substring(preference.Length + 1);
                        if (string.IsNullOrEmpty(value))
                            return default;
                        if(typeof(T) == typeof(GameMode))
                        {
                            return (T)(object)MinecraftServerManager.GetGameMode(value);
                        }
                        if (typeof(T) == typeof(Difficulty))
                        {
                            return (T)(object)MinecraftServerManager.GetDifficulty(value);
                        }
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                }
            }
            catch
            {
                return default;
            }

            return default;
        }
    }
}
