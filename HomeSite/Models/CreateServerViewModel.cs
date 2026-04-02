using HomeSite.Generated;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HomeSite.Models
{
    public class CreateServerViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Это обязательное поле")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Название должно быть от 3 до 20 символов")]
        public string Name { get; set; }

        [MaxLength(50, ErrorMessage = "Описание не должно превышать 50 символов")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Выберите ядро")]
        public string ServerCore { get; set; }

        public string? Version { get; set; }

        [Display(Name = "URL сборки CurseForge")]
        public string? CurseforgePackId { get; set; }

        public IEnumerable<SelectListItem> CoreOptions { get; set; } =
            Enum.GetValues(typeof(ServerCore))
                .Cast<ServerCore>()
                .Select(e => new SelectListItem
                {
                    Text = e.ToString(),
                    Value = e.ToString()
                });

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.Equals(ServerCore, "CURSEFORGE", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(CurseforgePackId))
                {
                    yield return new ValidationResult("Введите URL сборки CurseForge", new[] { nameof(CurseforgePackId) });
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(Version))
                {
                    yield return new ValidationResult("Версия обязательна", new[] { nameof(Version) });
                }
            }
        }
    }
}
