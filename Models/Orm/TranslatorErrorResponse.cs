using System.Text.Json.Serialization;

namespace AzureP33.Models.Orm
{
    // ORM for {"error":{"code":400036,"message":"The target language is not valid."}}
    public class TranslatorErrorResponse
    {
        [JsonPropertyName("error")]
        public ErrorData Error { get; set; } = null!;
    }
}
