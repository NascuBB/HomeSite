using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeSite.Entities
{
    [Table("versions")]
    public class MinecraftVersion
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("core")]
        public required string Core { get; set; }
        [Column("version")]
        public required string VersionNumber { get; set; }
    }
}
