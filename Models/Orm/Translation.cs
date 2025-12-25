using System.Text.Json.Serialization;

namespace Azure.Models.Orm
{
    public class Translation
    {
        [JsonPropertyName("text")]
        public String Text { get; set; }


        [JsonPropertyName("to")]
        public String ToLang { get; set; }
    }
}
