using Microsoft.AspNetCore.Mvc;

namespace AzureP33.Models.Home
{
    public class HomeIndexFormModel
    {
        [FromQuery(Name = "lang-from")]
        public String LangFrom { get; set; } = null!;

        [FromQuery(Name = "lang-to")]
        public String LangTo { get; set; } = null!;

        [FromQuery(Name = "original-text")]
        public String OriginalText { get; set; } = null!;

        [FromQuery(Name = "action-button")]
        public String? Action { get; set; } = null!;
    }
}
