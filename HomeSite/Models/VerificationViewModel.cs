using System.ComponentModel.DataAnnotations;

namespace HomeSite.Models
{
    public class VerificationViewModel
    {
        [Required(ErrorMessage = "Поле с кодом обязательное")]
        public int Code { get; set; }
    }
}
