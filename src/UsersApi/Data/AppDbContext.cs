using Microsoft.EntityFrameworkCore;
using UsersApi.Models;

namespace UsersApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Name = "Alice García",   Email = "alice@example.com",  Role = "Admin",    CreatedAt = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc) },
            new User { Id = 2, Name = "Bob Martínez",   Email = "bob@example.com",    Role = "Editor",   CreatedAt = new DateTime(2025, 2, 15, 0, 0, 0, DateTimeKind.Utc) },
            new User { Id = 3, Name = "Carol Ramírez",  Email = "carol@example.com",  Role = "Viewer",   CreatedAt = new DateTime(2025, 3, 20, 0, 0, 0, DateTimeKind.Utc) },
            new User { Id = 4, Name = "David López",    Email = "david@example.com",  Role = "Editor",   CreatedAt = new DateTime(2025, 4,  5, 0, 0, 0, DateTimeKind.Utc) },
            new User { Id = 5, Name = "Elena Torres",   Email = "elena@example.com",  Role = "Viewer",   CreatedAt = new DateTime(2025, 5, 12, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
