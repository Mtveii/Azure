namespace AzureP33.Services.CosmosDb
{
    public static class  CosmosDbServiceExtension
    {
        public static void AddCosmosDb(this IServiceCollection services)
        {
            services.AddSingleton <ICosmosBbService, SampleCosmosDbService>();
        }
    }
}
