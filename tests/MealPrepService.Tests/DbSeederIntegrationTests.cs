using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MealPrepService.DataAccessLayer.Data;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.Web.Data;
using Xunit;

namespace MealPrepService.Tests;

public class DbSeederIntegrationTests
{
    private MealPrepDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<MealPrepDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new MealPrepDbContext(options);
    }

    private ILogger<DbSeeder> CreateLogger()
    {
        return LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<DbSeeder>();
    }

    [Fact]
    public async Task DbSeeder_ShouldInitializeWithoutErrors()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var logger = CreateLogger();
        var passwordHasher = new MealPrepService.BusinessLogicLayer.Services.PasswordHasher();
        var filesDirectory = "files";

        // Act
        var seeder = new DbSeeder(context, logger, passwordHasher, filesDirectory);

        // Assert
        Assert.NotNull(seeder);
    }

    [Fact]
    public async Task DbSeeder_ShouldSeedAdminAccount()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var logger = CreateLogger();
        var passwordHasher = new MealPrepService.BusinessLogicLayer.Services.PasswordHasher();
        var filesDirectory = "nonexistent"; // Use nonexistent directory to skip dataset import

        var seeder = new DbSeeder(context, logger, passwordHasher, filesDirectory);

        // Act
        await seeder.SeedAsync();

        // Assert
        var adminAccount = await context.Accounts.FirstOrDefaultAsync(a => a.Role == "Admin");
        Assert.NotNull(adminAccount);
        Assert.Equal("admin@mealprep.com", adminAccount.Email);
    }

    [Fact]
    public async Task DbSeeder_ShouldSkipDatasetImportWhenFilesNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var logger = CreateLogger();
        var passwordHasher = new MealPrepService.BusinessLogicLayer.Services.PasswordHasher();
        var filesDirectory = "nonexistent";

        var seeder = new DbSeeder(context, logger, passwordHasher, filesDirectory);

        // Act
        await seeder.SeedAsync();

        // Assert - Should complete without errors even when dataset files don't exist
        var adminAccount = await context.Accounts.FirstOrDefaultAsync(a => a.Role == "Admin");
        Assert.NotNull(adminAccount);
    }

    [Fact]
    public async Task DbSeeder_ShouldSkipDatasetImportWhenDataAlreadyExists()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var logger = CreateLogger();
        var passwordHasher = new MealPrepService.BusinessLogicLayer.Services.PasswordHasher();
        var filesDirectory = "files";

        // Add some existing data
        context.Allergies.Add(new MealPrepService.DataAccessLayer.Entities.Allergy
        {
            Id = Guid.NewGuid(),
            AllergyName = "Test Allergy",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var seeder = new DbSeeder(context, logger, passwordHasher, filesDirectory);

        // Act
        await seeder.SeedAsync();

        // Assert - Should skip dataset import since data already exists
        var allergyCount = await context.Allergies.CountAsync();
        Assert.Equal(1, allergyCount); // Only the one we added manually
    }
}
