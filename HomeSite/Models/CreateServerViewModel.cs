using HomeSite.Managers;
using System.ComponentModel.DataAnnotations;

namespace HomeSite.Models
{
    public class CreateServerViewModel
    {
        [Required(ErrorMessage = "Это обязательное поле")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Название должно быть от 3 до 20 символов")]
        public string Name { get; set; }

        [MaxLength(50, ErrorMessage = "Описание не должно превышать 50 символов")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Версия обязательна")]
        public MinecraftVersion Version { get; set; }
    }


}
