namespace HomeSite.Models
{
    public class ServerViewModel
    {
        public bool IsRunning { get; set; }
        public ServerState ServerState { get; set; }
        public string logs { get; set; }
        
    }

    public enum ServerState
    {
        starting,
        started
    }
}
