using Microsoft.Azure.Cosmos;

namespace AzureP33.Services.CosmosDb
{
    public class SampleCosmosDbService:ICosmosBbService
    {
        private readonly IConfiguration _configuration;
        private Container? container;

        public SampleCosmosDbService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<Container> GetContainerAsync()
        {
            if (container == null)
            {
                var sec = _configuration.GetSection("Azure")?.GetSection("Translator")
               ?? throw new Exception("Configuration error: Azure.Translator is null");

                String connectionString = sec.GetValue<String>("connectionString")
                    ?? throw new Exception("connectionString error: 'Key' is null");

                String DatabaseId = sec.GetValue<String>("DatabaseId")
                    ?? throw new Exception("DatabaseId error: 'Key' is null");

                String ContainerId = sec.GetValue<String>("ContainerId")
                    ?? throw new Exception("ContainerId error: 'Key' is null");

                CosmosClient client = new(
                    connectionString: ""
                );

                Database database = await client.GetDatabase(DatabaseId).ReadAsync();


                Container container = await database.GetContainer(ContainerId).ReadContainerAsync();
            }
            return container!;
        }
    }
}
