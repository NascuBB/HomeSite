using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HomeSite.Entities
{
	[Index(nameof(Email), IsUnique = true)]
	[Index(nameof(Username), IsUnique = true)]
	public class UserAccount
	{
		[Key]
        public int Id { get; set; }
		[MaxLength(20)]
        public string Username { get; set; }
		[MaxLength(20)]
		[DataType(DataType.EmailAddress)]
		public string Email { get; set; }
        public string PasswordHash { get; set; }
    }
}
