using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ORM;

public sealed class DbManagerFactory : IDesignTimeDbContextFactory<DbManager>
{
    public DbManager CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DbManager>();
        const string connectionString = "Server=localhost;Database=HealthCompanion;User=root;Password=244466666;";
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

        return new DbManager(optionsBuilder.Options);
    }
}
