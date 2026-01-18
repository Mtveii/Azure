using System.Text.Json.Serialization;

namespace AzureP33.Models.Orm
{
    public class LanguagesResponse
    {
        [JsonPropertyName("translation")]
        public Dictionary<String, LangData> Translations { get; set; } = new();

        [JsonPropertyName("transliteration")]
        public Dictionary<String, LangData> Transliterations { get; set; } = new();
    }
}
