using AzureP33.Models.Orm;

namespace AzureP33.Models.Home
{
    public class HomeIndexViewModel
    {
        public String PageTitle { get; set; } = "";
        public HomeIndexFormModel? FormModel { get; set; }
        public LanguagesResponse LanguagesResponse { get; set; } = null!;
        public TranslatorErrorResponse? ErrorResponse { get; set; }
        public List<TranslatorResponseItem>? Items { get; set; }
        public TransliteratorResponseItem? FromTransliteration { get; set; }
        public TransliteratorResponseItem? ToTransliteration { get; set; }
    }
}
