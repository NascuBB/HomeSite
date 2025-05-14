using HomeSite.Managers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeSite.Entities
{
    [Table("servers")]
    public class Server
    {
        [Key]
        [Column("id")]
        public string Id { get; set; }
        [Column("name")]
        [MaxLength(20)]
        public string Name { get; set; }
        [Column("description")]
        [MaxLength(50)]
        public string? Description { get; set; }
        [Column("version")]
        public MinecraftVersion Version { get; set; }
        [Column("publicport")]
        public int PublicPort { get; set; }
        [Column("rconport")]
        public int RCONPort { get; set; }
    }
}
