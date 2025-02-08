using Newtonsoft.Json;

namespace HomeSite.Managers
{
    public static class FileShareManager
    {
        public static List<ShareFileInfo>? SharedFiles { get => sharedFiles; }
        private static List<ShareFileInfo>? sharedFiles = null;
        public static string folder = Path.Combine(Directory.GetCurrentDirectory(), "Share");
        static string sharesFilePath = Path.Combine(folder, "shares.json");
        public static async void PrepareFileShare()
        {
            if (!Path.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            if (!File.Exists(sharesFilePath))
            {
                sharedFiles = new();
                File.WriteAllText(sharesFilePath, JsonConvert.SerializeObject(SharedFiles));
            }
            else
            {
                //await CheckUserFiles();
            }
        }

        public static async Task<string> WriteFile(IFormFile file, string username)
        {
            string filename = "";
            try
            {
                string id = DateTime.Now.Ticks.ToString();
                var extension = "." + file.FileName.Split('.')[file.FileName.Split('.').Length - 1];
                filename = id + extension;

                var exactpath = Path.Combine(folder, filename);
                using (var stream = new FileStream(exactpath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                sharedFiles.Add(new ShareFileInfo { Description = "", OriginalFilename = file.FileName, ExpireTime = DateTime.Today.AddDays(3), Filename = filename, Size = file.Length, Username = username});
                File.WriteAllText(sharesFilePath, JsonConvert.SerializeObject(sharedFiles));
                return id;

            }
            catch (Exception ex)
            {
            }
            return filename;
        }

        public static async Task<bool> WriteAndUnpackMods(IFormFile file, string Id)
        {
            //string filename = "";
            try
            {
                //string id = DateTime.Now.Ticks.ToString();
                var extension = "." + file.FileName.Split('.')[file.FileName.Split('.').Length - 1];
                if(extension != ".zip")
                    return false;
                //filename = id + extension;

                var exactpath = Path.Combine(MinecraftServerManager.folder, Id, $"mods{extension}");
                using (var stream = new FileStream(exactpath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                System.IO.Compression.ZipFile.ExtractToDirectory(exactpath, Path.Combine(MinecraftServerManager.folder, Id, "mods"));
                await RenameFilesAsync(Path.Combine(MinecraftServerManager.folder, Id, "mods"));
                //sharedFiles.Add(new ShareFileInfo { Description = "", OriginalFilename = file.FileName, ExpireTime = DateTime.Today.AddDays(3), Filename = filename });
                //File.WriteAllText(sharesFilePath, JsonConvert.SerializeObject(sharedFiles));
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        public static async Task CheckUserFiles()
        {
            sharedFiles = JsonConvert.DeserializeObject<List<ShareFileInfo>>(File.ReadAllText(sharesFilePath));
            if (SharedFiles == null) sharedFiles = new();
            foreach (ShareFileInfo sharedFile in sharedFiles.ToList())
            {
                if (sharedFile.ExpireTime < DateTime.Today)
                {
                    File.Delete(Path.Combine(folder, sharedFile.Filename));
                    sharedFiles.Remove(sharedFile);
                }
            }
            await File.WriteAllTextAsync(sharesFilePath, JsonConvert.SerializeObject(SharedFiles));
        }
        static async Task RenameFilesAsync(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine("Указанная папка не существует.");
                return;
            }

            string[] files = Directory.GetFiles(folderPath)
                                      .Where(f => Path.GetFileName(f).Contains("+"))
                                      .ToArray();

            foreach (string filePath in files)
            {
                string directory = Path.GetDirectoryName(filePath)!;
                string fileName = Path.GetFileName(filePath);
                string newFileName = fileName.Replace("+", " ");
                string newFilePath = Path.Combine(directory, newFileName);

                if (!File.Exists(newFilePath)) // Проверка на существование файла с таким именем
                {
                    try
                    {
                        File.Move(filePath, newFilePath);
                        Console.WriteLine($"Переименован: {fileName} -> {newFileName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при переименовании {fileName}: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Файл {newFileName} уже существует, пропущен.");
                }
            }

            await Task.CompletedTask; // Для соответствия сигнатуре async
        }

        public static async Task OnceADayClock()
        {
            while (true)
            {
                try
                {
                    await CheckUserFiles();
                    await Task.Delay(TimeSpan.FromHours(24));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }



    public class ShareFileInfo
    {
        public string Filename { get; set; }
        public string OriginalFilename { get; set; }
        public DateTime ExpireTime { get; set; }
        public string Description { get; set; }
        public string Username { get; set; }
        public long Size { get; set; }
        //public bool Public { get; set; }

    }
}
