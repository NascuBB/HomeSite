using System.ComponentModel.DataAnnotations;

namespace HomeSite.Models
{
	public class LoginViewModel
	{
		[Required(ErrorMessage = "Почта или пароль обязательные")]
		[MaxLength(50, ErrorMessage = "максимум 50 символов")]
		public string EmailOrUsername { get; set; }

		[Required(ErrorMessage = "Пароль обязательный")]
		[StringLength(20, MinimumLength = 5, ErrorMessage = "Пароль должен быть от 5 до 20 символов")]
		//[EmailAddress(ErrorMessage = "Это не похоже на почту")]
		//[RegularExpression(@"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "Это не похоже на почту")]
		public string Password { get; set; }
		public bool RememberMe { get; set; } = false;
	}
}
