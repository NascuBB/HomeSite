using HomeSite.Managers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeSite.Entities
{
    [Table("servers")]
    public class Server
    {
        [Key]
        public string id { get; set; }
        [MaxLength(20)]
        public string name { get; set; }
        [MaxLength(50)]
        public string description { get; set; }
        public MinecraftVersion version { get; set; }
        public int publicport { get; set; }
        public int rconport { get; set; }
    }
}
