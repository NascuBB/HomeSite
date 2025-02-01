using System.Diagnostics;

namespace HomeSite.Helpers
{
    public static class Helper
    {
        public static WebApplication thisApp { get; private set; } 

        public static void SetThisApp(WebApplication application)
        {
            thisApp = application;
        }
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
                $"server-ip=192.168.31.204\r\n" +
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
                $"rcon.password=gamemode1\r\n" +
                $"player-idle-timeout=0\r\n" +
                $"force-gamemode=true\r\n" +
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

        public static void Copy(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)));

            foreach (var directory in Directory.GetDirectories(sourceDir))
                Copy(directory, Path.Combine(targetDir, Path.GetFileName(directory)));
        }
        public static bool GetMinecraftServer()
        {
            Process? mServer = Process.GetProcessesByName("Minecraft Server")[0];
            return false;
        }
        public static async Task<float> GetCPUCounter()
        {

            PerformanceCounter cpuCounter = new PerformanceCounter();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";

            // will always start at 0
            dynamic firstValue = cpuCounter.NextValue();
            await Task.Delay(1000);
            // now matches task manager reading
            dynamic secondValue = cpuCounter.NextValue();

            return secondValue;

        }
        public static async Task<float> GetMEMCounter()
        {
            PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            // will always start at 0
            dynamic firstValue = ramCounter.NextValue();
            await Task.Delay(1000);
            // now matches task manager reading
            dynamic secondValue = ramCounter.NextValue();

            return secondValue;
        }
        public static async Task<float> GetGPUUsage()
        {
            try
            {
                var category = new PerformanceCounterCategory("GPU Engine");
                var counterNames = category.GetInstanceNames();
                var gpuCounters = new List<PerformanceCounter>();
                var result = 0f;

                foreach (string counterName in counterNames)
                {
                    if (counterName.EndsWith("engtype_3D"))
                    {
                        foreach (PerformanceCounter counter in category.GetCounters(counterName))
                        {
                            if (counter.CounterName == "Utilization Percentage")
                            {
                                gpuCounters.Add(counter);
                            }
                        }
                    }
                }

                gpuCounters.ForEach(x =>
                {
                    _ = x.NextValue();
                });

                await Task.Delay(1000);

                gpuCounters.ForEach(x =>
                {
                    result += x.NextValue();
                });

                return result;
            }
            catch
            {
                return 0f;
            }
        }
    }
}
