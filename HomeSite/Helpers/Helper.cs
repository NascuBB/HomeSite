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
