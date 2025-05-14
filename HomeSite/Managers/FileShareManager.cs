using HomeSite.Entities;
using HomeSite.Helpers;
using Newtonsoft.Json;

namespace HomeSite.Managers
{
    public class FileShareManager : IFileShareManager
    {
        private readonly ShareFileInfoDBContext _shareContext;
        private readonly IUserHelper _userHelper;
        public FileShareManager(ShareFileInfoDBContext shareFileInfo, IUserHelper userHelper)
        {
            _shareContext = shareFileInfo;
            _userHelper = userHelper;
        }


        public static string folder = Path.Combine(Directory.GetCurrentDirectory(), "Share");

        public async Task<string> WriteFile(IFormFile file, string username)
        {
            string filename = "";
            try
            {
                long id = DateTime.Now.Ticks;
                var extension = "." + file.FileName.Split('.')[file.FileName.Split('.').Length - 1];
                filename = id.ToString();

                var exactpath = Path.Combine(folder, filename);
                using (var stream = new FileStream(exactpath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _shareContext.SharedFiles.Add(new ShareFileInfo
                {
                    Description = "",
                    OriginalName = file.FileName,
                    FileId = id,
                    Size = file.Length,
                    Extension = extension,
                    Share = false,
                    UserId = _userHelper.GetUserId(username),
                    DateUploaded = DateTime.UtcNow.Date,
                    Featured = false,
                });
                _shareContext.SaveChanges();
                
                return id.ToString();

            }
            catch (Exception ex)
            {
            }
            return filename;
        }

        public bool SharedFile(long id)
        {
            var file = _shareContext.SharedFiles.Find(id);
            return file?.Share ?? false;
        }

        public bool RenameFile(long id, string newFilename)
        {
            var file = _shareContext.SharedFiles.Find(id);
            if (file == null)
                return false;
            string? extension = "." + newFilename.Split('.')[newFilename.Split('.').Length - 1];
            file.OriginalName = newFilename;
            file.Extension = extension ?? ".file";

            return _shareContext.SaveChanges() > 0;
        }
        public bool ChangeShareOfFile(long id, bool newShare)
        {
            var file = _shareContext.SharedFiles.Find(id);
            if (file == null)
                return false;

            file.Share = newShare;

            return _shareContext.SaveChanges() > 0;
        }
        public bool ChangeFeatureOfFile(long id, bool newFeat)
        {
            var file = _shareContext.SharedFiles.Find(id);
            if (file == null)
                return false;

            file.Featured = newFeat;

            return _shareContext.SaveChanges() > 0;
        }
        public bool DeleteFile(long id)
        {
            try
            {
                File.Delete(Path.Combine(folder, id.ToString()));

                ShareFileInfo? file = _shareContext.SharedFiles.Find(id);
                if (file is not null)
                    _shareContext.SharedFiles.Remove(file);

                if(_shareContext.SaveChanges() <= 0)
                    return false;
                _userHelper.ChangeUserSizeUsed(-file.Size, file.UserId);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public List<ShareFileInfo> UserSharedFiles(int userid)
        {
            return _shareContext.SharedFiles.Where(x => x.UserId == userid).ToList();
        }
        public ShareFileInfo? GetFile(long id)
        {
            return _shareContext.SharedFiles.Find(id);
        }
        public static async Task<bool> WriteAndUnpackMods(IFormFile file, string Id)
        {
            //string filename = "";
            try
            {
                string modsPath = Path.Combine(MinecraftServerManager.folder, Id, "mods");
                if (Directory.Exists(modsPath))
                {
                    var di = new DirectoryInfo(modsPath);
                    foreach (FileInfo f in di.GetFiles())
                    {
                        f.Delete();
                    }
                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                }
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
                System.IO.Compression.ZipFile.ExtractToDirectory(exactpath, modsPath);
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

        //public static async Task CheckUserFiles()
        //{
        //    sharedFiles = JsonConvert.DeserializeObject<List<ShareFileInfo>>(File.ReadAllText(sharesFilePath));
        //    if (SharedFiles == null) sharedFiles = new();
        //    foreach (ShareFileInfo sharedFile in sharedFiles.ToList())
        //    {
        //        if (sharedFile.ExpireTime < DateTime.Today)
        //        {
        //            File.Delete(Path.Combine(folder, sharedFile.Filename));
        //            sharedFiles.Remove(sharedFile);
        //        }
        //    }
        //    await File.WriteAllTextAsync(sharesFilePath, JsonConvert.SerializeObject(SharedFiles));
        //}
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
                    //TODO: проверка файлов раз в день на удаление файлов
                    //await CheckUserFiles();
                    await Task.Delay(TimeSpan.FromHours(24));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
