using System.ComponentModel.DataAnnotations;

namespace HomeSite.Models
{
	public class RegisterViewModel
	{
        [Required(ErrorMessage = "Имя обязательное")]
		[StringLength(20, MinimumLength = 5, ErrorMessage = "Имя должно быть от 3 до 20 символов")]
		public string Username { get; set; }

        [Required(ErrorMessage = "Почта обязательная")]
        [MaxLength(50, ErrorMessage = "Почта максимум 50 символов")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Пароль обязательный")]
        [StringLength(20,MinimumLength = 5, ErrorMessage = "Пароль должен быть от 5 до 20 символов")]
		//[EmailAddress(ErrorMessage = "Это не похоже на почту")]
		[RegularExpression(@"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "Это не похоже на почту")]
		public string Password { get; set; }

        [Compare("Password", ErrorMessage = "Подтвердите свой пароль")]
        public string ConfirmPassword { get; set; }
    }
}
