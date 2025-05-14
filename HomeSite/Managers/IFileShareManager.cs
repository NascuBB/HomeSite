using HomeSite.Entities;
using static NuGet.Packaging.PackagingConstants;

namespace HomeSite.Managers
{
    public interface IFileShareManager
    {
        public List<ShareFileInfo> UserSharedFiles(int userid);
        public Task<string> WriteFile(IFormFile file, string username);
        public bool SharedFile(long id);
        public bool RenameFile(long id, string newFilename);
        public bool DeleteFile(long id);
        public ShareFileInfo? GetFile(long id);
        public bool ChangeShareOfFile(long id, bool newShare);
        public bool ChangeFeatureOfFile(long id, bool newFeat);
    }
}
