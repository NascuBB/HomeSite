namespace HomeSite.Helpers
{
    public static class FileShareManager
    {
        public static void PrepareFileShare()
        {
            if(!Path.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Share")))
            {
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Share"));
            }
        }
    }
}
