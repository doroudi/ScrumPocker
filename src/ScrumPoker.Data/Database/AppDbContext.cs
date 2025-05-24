using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using ScrumPoker.Data.Models;

namespace ScrumPoker.Data.Database;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    #nullable disable
    public DbSet<Session> Sessions { get; init; }

    public static AppDbContext Create(IMongoDatabase database)
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseMongoDB(database.Client, database.DatabaseNamespace.DatabaseName)
            .Options);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Session>().ToCollection("sessions");
    }
}
