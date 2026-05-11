using Microsoft.EntityFrameworkCore;
using Models;

namespace ORM;

public class DbManager : DbContext
{
    private const string ConnectionString = "Server=localhost;Database=swp_maui;User=root;Password=244466666;";

    public DbSet<User> Users { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseMySql(
                ConnectionString,
                ServerVersion.AutoDetect(ConnectionString)
            );
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Password).IsRequired();
            entity.Property(e => e.Birthdate).IsRequired();
            entity.Property(e => e.Gender).IsRequired();
        });
    }
}
