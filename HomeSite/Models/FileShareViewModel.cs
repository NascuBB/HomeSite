using HomeSite.Entities;

namespace HomeSite.Models
{
    public class FileShareViewModel
    {
        public List<ShareFileInfo>? Files { get; set; }
        public double SpaceUsed { get; set; } = 0;
        public int percentUsed { get; set; } = 0;
    }
}
