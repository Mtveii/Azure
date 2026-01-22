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
                var sec = _configuration.GetSection("Azure")?.GetSection("CosmosDB")
               ?? throw new Exception("Configuration error: Azure.CosmosDB is null");

                String connectionString = sec.GetValue<String>("connectionString")
                    ?? throw new Exception("Configuration error: 'connectionString' is null");

                String DatabaseId = sec.GetValue<String>("DatabaseId")
                    ?? throw new Exception("Configuration error: 'DatabaseId' is null");

                String ContainerId = sec.GetValue<String>("ContainerId")
                    ?? throw new Exception("Configuration error: 'ContainerId' is null");

                CosmosClient client = new(
                    connectionString: connectionString
                );

                Database database = await client.GetDatabase(DatabaseId).ReadAsync();


                Container container = await database.GetContainer(ContainerId).ReadContainerAsync();
            }
            return container!;
        }
    }
}
