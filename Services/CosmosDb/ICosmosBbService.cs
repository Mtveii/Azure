using Microsoft.Azure.Cosmos;

namespace AzureP33.Services.CosmosDb
{
    public interface ICosmosBbService
    {
        Task<Container> GetContainerAsync();
    }
}
