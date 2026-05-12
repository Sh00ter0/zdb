using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence
{
    public class ApiSecurityDbContextFactory : IDesignTimeDbContextFactory<ApiSecurityDbContext>
    {
        public ApiSecurityDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables(prefix: "DZB_")
                .Build();

            var dbPath = DbPath.GetDatabasePath(configuration["api:databasePath"] ?? "Data\\api-security.db");

            var optionsBuilder = new DbContextOptionsBuilder<ApiSecurityDbContext>();
            optionsBuilder.UseSqlite($"Data Source={dbPath}", b => b.MigrationsAssembly("Infrastructure"));
            

            return new ApiSecurityDbContext(optionsBuilder.Options);
        }
    }
}
