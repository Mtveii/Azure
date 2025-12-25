using System.Text.Json.Serialization;

namespace AzureP33.Models.Orm
{
    public class LangData
    {
        [JsonPropertyName("name")]
        public String Name { get; set; } = null;

        [JsonPropertyName("nativeName")]
        public String NativeName { get; set; } = null;

        [JsonPropertyName("dir")]
        public String? Dir { get; set; } = null;
    }
}
