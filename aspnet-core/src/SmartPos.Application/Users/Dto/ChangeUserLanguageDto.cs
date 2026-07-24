using System.ComponentModel.DataAnnotations;

namespace SmartPos.Users.Dto
{
    public class ChangeUserLanguageDto
    {
        [Required]
        public string LanguageName { get; set; }
    }
}