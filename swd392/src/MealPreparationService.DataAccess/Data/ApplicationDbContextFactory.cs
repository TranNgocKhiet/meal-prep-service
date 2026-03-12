using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MealPreparationService.DataAccess.Data;

/// <summary>
/// Design-time factory for creating ApplicationDbContext instances during migrations
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Use a connection string for design-time operations
        // This will be replaced by the actual connection string at runtime
        optionsBuilder.UseSqlServer("Server=LAPTOP-V23D7L07\\SQLEXPRESS01,1433;uid=sa;pwd=12345;Database=MealPreparationService;TrustServerCertificate=True;MultipleActiveResultSets=true");
        
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
