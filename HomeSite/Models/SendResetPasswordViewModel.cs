using System.ComponentModel.DataAnnotations;

namespace HomeSite.Models
{
    public class SendResetPasswordViewModel
    {
        [Required(ErrorMessage = "Напишите почту связанную с аккаунтом")]
        [EmailAddress(ErrorMessage = "Введите корректную электронную почту")]
        public string? Email { get; set; }
    }
}
