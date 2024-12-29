namespace HomeSite.Helpers
{
    public class ServerInfo
    {
        private static ServerInfo? _instance;
        public CancellationTokenSource CancellationTokenSource { get; }

        private ServerInfo()
        {
            CancellationTokenSource = new CancellationTokenSource();
        }

        public static ServerInfo GetInstance()
        {
            _instance ??= new ServerInfo();
            return _instance;
        }

        public static float CpuPercentage { get { return cpuPercentage; } }
        public static float GpuPercentage { get { return gpuPercentage; } }
        public static float MemFree { get { return memFree; } }

        private static float cpuPercentage = 0;
        private static float gpuPercentage = 0;
        private static float memFree = 0;

        public async Task StartMonitoring(CancellationToken cancellationToken)
        {
            while (true)
            {
                Task t1 = Task.Run(async () => { gpuPercentage = await Helper.GetGPUUsage(); }, cancellationToken);
                Task t2 = Task.Run(async () => { cpuPercentage = await Helper.GetCPUCounter(); }, cancellationToken);
                Task t3 = Task.Run(async () => { memFree = await Helper.GetMEMCounter(); }, cancellationToken);
                await Task.WhenAll(t1, t2, t3);
                await Task.Delay(5000, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
