using System.Text.Json.Serialization;

namespace AzureP33.Models.Orm
{
    // ORM for {"text":"Greetings","to":"en"}
    public class Translation
    {
        [JsonPropertyName("text")]
        public String Text { get; set; } = null!;


        [JsonPropertyName("to")]
        public String ToLang { get; set; } = null!;
    }
}
