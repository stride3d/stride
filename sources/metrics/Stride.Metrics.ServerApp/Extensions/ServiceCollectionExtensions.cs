using Stride.Metrics.ServerApp.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseSeeder(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<DatabaseSeederService>();
        return serviceCollection;
    }
}