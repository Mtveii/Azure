using System.Text.Json.Serialization;

namespace AzureP33.Models.Orm
{

    public class TransliteratorResponseItem
    {
        [JsonPropertyName("text")]
        public String Text { get; set; } = null!;


        [JsonPropertyName("script")]
        public String Script { get; set; } = null!;
    }
}
