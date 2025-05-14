using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HomeSite.Helpers
{
    public static class Helper
    {
        public static WebApplication thisApp { get; private set; } 

        public static void SetThisApp(WebApplication application)
        {
            thisApp = application;
        }

		public static string GetTrimmedLogs(string logs)
		{
			string[] lines = logs.Split('\n');

			if (lines.Length <= 20)
				return logs; // Если строк 20 или меньше, возвращаем их без изменений

			string[] first10 = lines.Take(new Range(1, 6)).ToArray();
			string[] last10 = lines.Skip(lines.Length - 10).ToArray();

			return string.Join("\n", first10) + "\n.\n.\n.\n" + string.Join("\n", last10);
		}

		public static void Copy(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)));

            foreach (var directory in Directory.GetDirectories(sourceDir))
                Copy(directory, Path.Combine(targetDir, Path.GetFileName(directory)));
        }

        public static string ShortenFileName(string fileName, int maxStart = 6, int maxEnd = 8)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "";

            if (fileName.Length <= (maxStart + maxEnd + 3)) // учтём "..."
                return fileName;

            return fileName.Substring(0, maxStart) + "..." + fileName.Substring(fileName.Length - maxEnd);
        }

        public static string FormatFileSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} Б";
            else if (bytes < 1024 * 1024)
                return $"{Math.Round(bytes / 1024.0, 2)} КБ";
            else if (bytes < 1024L * 1024 * 1024)
                return $"{Math.Round(bytes / (1024.0 * 1024), 2)} МБ";
            else
                return $"{Math.Round(bytes / (1024.0 * 1024 * 1024), 2)} ГБ";
        }

        public static string GetIconClass(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return "bi-file-earmark-fill"; // default icon

            switch (extension.ToLower())
            {
                case ".mp3":
                case ".wav":
                    return "bi-file-earmark-music-fill";

                case ".mp4":
                case ".mov":
                    return "bi-file-earmark-play-fill";

                case ".pdf":
                    return "bi-file-earmark-pdf-fill";

                case ".doc":
                case ".docx":
                    return "bi-file-earmark-word-fill";

                case ".xls":
                case ".xlsx":
                    return "bi-file-earmark-excel-fill";

                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                    return "bi-file-earmark-image-fill";

                case ".txt":
                case ".log":
                    return "bi-file-earmark-text-fill";

                case ".zip":
                case ".rar":
                case ".7z":
                    return "bi-file-earmark-zip-fill";

                default:
                    return "bi-file-earmark-fill"; // fallback icon
            }
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
