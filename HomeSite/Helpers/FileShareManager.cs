using Newtonsoft.Json;

namespace HomeSite.Helpers
{
    public static class FileShareManager
    {
        public static List<ShareFileInfo>? SharedFiles { get => sharedFiles; }
        private static List<ShareFileInfo>? sharedFiles = null;
        public static string folder = Path.Combine(Directory.GetCurrentDirectory(), "Share");
        static string sharesFilePath = Path.Combine(folder, "shares.json");
        public static void PrepareFileShare()
        {
            if(!Path.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            if(!File.Exists(sharesFilePath))
            {
                sharedFiles = new();
                File.WriteAllText(sharesFilePath, JsonConvert.SerializeObject(SharedFiles));
            }
            else
            {
                sharedFiles = JsonConvert.DeserializeObject<List<ShareFileInfo>>(File.ReadAllText(sharesFilePath));
                if(SharedFiles == null) sharedFiles = new();
                foreach(ShareFileInfo sharedFile in sharedFiles.ToList())
                {
                    if(sharedFile.ExpireTime < DateTime.Today)
                    {
                        File.Delete(Path.Combine(folder,sharedFile.Filename));
                        sharedFiles.Remove(sharedFile);
                    }
                }
                File.WriteAllText(sharesFilePath, JsonConvert.SerializeObject(SharedFiles));
            }
        }

        public static async Task<string> WriteFile(IFormFile file)
        {
            string filename = "";
            try
            {
                var extension = "." + file.FileName.Split('.')[file.FileName.Split('.').Length - 1];
                filename = DateTime.Now.Ticks.ToString() + extension;

                var exactpath = Path.Combine(folder, filename);
                using (var stream = new FileStream(exactpath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                SharedFiles.Add(new ShareFileInfo { Description = "", OriginalFilename = file.FileName, ExpireTime = DateTime.Today.AddDays(3), Filename = filename});
                File.WriteAllText(sharesFilePath, JsonConvert.SerializeObject(SharedFiles));
                return filename;

            }
            catch (Exception ex)
            {
            }
            return filename;
        }
    }



    public class ShareFileInfo
    {
        public string Filename { get; set; }
        public string OriginalFilename { get; set; }
        public DateTime ExpireTime { get; set; }
        public string Description { get; set; }
        //public bool Public { get; set; }

    }
}
