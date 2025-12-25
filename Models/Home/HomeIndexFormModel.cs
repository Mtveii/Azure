using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AzureP33.Models.Home
{
    public class HomeIndexFormModel
    {
        [FromQuery(Name = "lang-from")]
        public String LangFrom { get; set; } = null!;

        [FromQuery(Name = "lang-to")]
        public String LangTo { get; set; } = null!;

        [FromQuery(Name = "original-text")]
        [Required(ErrorMessage = "Поле обов'язкове.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Текст має бути від {2} до {1} символів.")]
        public String OriginalText { get; set; } = null!;

        [FromQuery(Name = "action-button")]
        public String? Action { get; set; } = null!;
    }
}
