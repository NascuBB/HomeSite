using System.ComponentModel.DataAnnotations;

namespace HomeSite.Models
{
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Введите новый пароль")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "Пароль должен быть от 5 до 20 символов")]
        //[EmailAddress(ErrorMessage = "Это не похоже на почту")]
        public required string NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "Подтвердите новый пароль")]
        public required string ConfirmPassword { get; set; }
    }
}
