using System.Text.Json.Serialization;

namespace Azure.Models.Orm
{
    public class translatorErrorResponse
    {
        [JsonPropertyName("error")]
        public ErrorData Error { get; set; } = null!;
    }
}
