using Microsoft.EntityFrameworkCore;
using Models;

namespace ORM;

public class DbManager : DbContext
{
    public DbManager(DbContextOptions<DbManager> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;

    public async Task<User> AddUserAsync(User user, CancellationToken cancellationToken = default)
    {
        Users.Add(user);
        await SaveChangesAsync(cancellationToken);
        return user;
    }

    public Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return Users.FirstOrDefaultAsync(user => user.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            const string connectionString = "Server=localhost;Database=HealthCompanion;User=root;Password=244466666;";
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Password).IsRequired().HasMaxLength(512);
            entity.Property(e => e.PolarAccessToken).HasMaxLength(4096);
            entity.Property(e => e.PolarUserId).HasMaxLength(100);
        });
    }
}
