using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeSite.Entities
{
    [Table("useraccounts")]
    [Index(nameof(email), IsUnique = true)]
	[Index(nameof(username), IsUnique = true)]
	public class UserAccount
	{
		[Key]
        public int id { get; set; }
		[MaxLength(20)]
        public string username { get; set; }
		[MaxLength(100)]
		[DataType(DataType.EmailAddress)]
		public string email { get; set; }
        public string? serverid { get; set; }
		public bool shortlogs { get; set; }
        public string passwordhash { get; set; }
    }
}
