using System.Text.Json.Serialization;

namespace AzureP33.Models.Orm
{
    // ORM {"translations":[{"text":"Greetings","to":"en"}]}
    public class TranslatorResponseItem
    {
        [JsonPropertyName("translations")]
        public List<Translation> Translations { get; set; } = new();
        
    }
}
