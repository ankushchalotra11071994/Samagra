using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Samagra.Infrastructure.Data;

// Used ONLY by the dotnet-ef tooling at design time.
// Never called at runtime — runtime uses DI.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SAMAGRA_CONNECTION")
            ?? "Server=localhost,1433;Database=SamagraDb;User Id=sa;Password=Password@123;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}