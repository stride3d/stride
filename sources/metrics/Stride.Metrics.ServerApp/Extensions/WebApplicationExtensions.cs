using Stride.Metrics.ServerApp.Data;

namespace Stride.Metrics.ServerApp.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseDatabaseSeeder(this WebApplication app)
    {
        using var serviceScope = app.Services.CreateScope();
        var databaseSeederService = serviceScope.ServiceProvider.GetRequiredService<DatabaseSeederService>();
        databaseSeederService.SeedDatabase();

        return app;
    }
}
