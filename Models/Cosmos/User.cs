using Newtonsoft.Json;

namespace AzureP33.Models.Cosmos
{
    public class User
    {
        public static readonly String PartitionKey = "Users";


        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("partitionkey")]
        public String partitionKey { get; set; } = User.PartitionKey;
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
    }
}
