using HomeSite.Generated;
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
        [MaxLength(255)]
        public string? Description { get; set; }

        [Column("version")]
        public required string Version { get; set; }

        [Column("curseforgepack")]
        public string? CurseforgePack { get; set; }

        [Column("bedrockport")]
        public int BedrockPort { get; set; } = 0;

        [Column("core")]
        public required string ServerCore { get; set; }

        [Column("portmappings")]
        public required List<PortMapping> PortMappings { get; set; } = [new PortMapping { Port = 8080, Path = null }];

        [Column("domainname")]
        public required string DomainName { get; set; }
    }
}
