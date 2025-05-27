using HomeSite.Entities;
using HomeSite.Helpers;

namespace HomeSite.Managers
{
    public class FileShareCheckerManager : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FileShareCheckerManager> _logger;

        public FileShareCheckerManager(IServiceScopeFactory scopeFactory, ILogger<FileShareCheckerManager> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckExpiredFilesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while reviewing expiration of user files");
                }

                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }

        public async Task CheckExpiredFilesAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var shareContext = scope.ServiceProvider.GetRequiredService<ShareFileInfoDBContext>();
            var userContext = scope.ServiceProvider.GetRequiredService<UserDBContext>();
            var userHelper = scope.ServiceProvider.GetRequiredService<UserHelper>();

            var users = userContext.UserAccounts.ToList();

            foreach (var user in users)
            {
                if(user.DateLogged == null)
                { continue; }
                var daysSinceLastLogin = (DateTime.UtcNow - user.DateLogged.Value).TotalDays;

                if (daysSinceLastLogin >= 60)
                {
                    var files = shareContext.SharedFiles.Where(f => f.UserId == user.Id).ToList();

                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(Path.Combine(FileShareManager.folder, file.FileId.ToString()));
                            userHelper.ChangeUserSizeUsed(-file.Size, file.UserId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error while checking user files");
                        }

                        shareContext.SharedFiles.Remove(file);
                    }

                    await shareContext.SaveChangesAsync();
                }
                else if (daysSinceLastLogin >= 53)
                {
                    await EmailManager.SendFileDeletionEmailAsync(user.Email);
                }
            }
        }
    }
}
