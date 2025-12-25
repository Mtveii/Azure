using Azure.Models.Orm;
using AzureP33.Models.Orm;

namespace AzureP33.Models.Home
{
    public class HomeIndexViewModel
    {
        public String PageTitle { get; set; } = "";
        public HomeIndexFormModel? FormModel { get; set; }
        public languagesRespons Languages { get; set; } = null!;

        public translatorErrorResponse ErrorResponse { get; set; }
        public List<TranslatorResponseltem> Item { get; set; }
    
    }
}
