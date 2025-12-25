using System.Text.Json.Serialization;

namespace Azure.Models.Orm
{
    public class ErrorData
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = null!;
    }
}
