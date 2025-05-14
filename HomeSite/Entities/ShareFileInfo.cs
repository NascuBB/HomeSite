using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HomeSite.Entities
{
    [Table("fileshares")]
    public class ShareFileInfo
    {
        [Key]
        [Column("fileid")]
        public long FileId { get; set; }

        [Required]
        [Column("userid")]
        public int UserId { get; set; }
        [Column("extension")]
        public string? Extension { get; set; }
        [Column("originalname")]
        public string? OriginalName { get; set; }
        [Column("description")]
        [MaxLength(250)]
        public string? Description { get; set; }
        [Column("size")]
        [Required]
        public long Size { get; set; }
        [Column("share")]
        public bool Share { get; set; } = false;
        [Column("featured")]
        public bool Featured { get; set; } = false;
        [Column("dateuploaded")]
        [Required]
        public DateTime DateUploaded { get; set; }
    }
}
