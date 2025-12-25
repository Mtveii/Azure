using System.Text.Json.Serialization;

namespace Azure.Models.Orm
{
    public class TranslatorResponseltem
    {
        [JsonPropertyName("translations")]
        public List<Translation> Trenslations { get; set; } = new();

    }
}
