using System.Text.Json.Serialization;

namespace AzureP33.Models.Orm
{
    // ORM for {"code":400036,"message":"The target language is not valid."}
    public class ErrorData
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }


        [JsonPropertyName("message")]
        public String Message { get; set; } = null!;
    }
}
