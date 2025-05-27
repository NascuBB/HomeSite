using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeSite.Entities
{
    [Table("useraccounts")]
    [Index(nameof(Email), IsUnique = true)]
	[Index(nameof(Username), IsUnique = true)]
	public class UserAccount
	{
		[Key]
        [Column("id")]
        public int Id { get; set; }
		[MaxLength(20)]
        [Column("username")]
        public required string Username { get; set; }
		[MaxLength(100)]
		[DataType(DataType.EmailAddress)]
        [Column("email")]
		public required string Email { get; set; }
        [Column("serverid")]
        public string? ServerId { get; set; }
        [Column("shortlogs")]
        public bool ShortLogs { get; set; }
        [Column("passwordhash")]
        public required string PasswordHash { get; set; }
        [Column("sizeused")]
        public long SizeUsed { get; set; }
		[Column("datelogged")]
		public DateTime? DateLogged { get; set; }
        [Column("verified")]
        public bool Verified { get; set; } = false;
    }
}
