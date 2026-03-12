using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MealPreparationService.DataAccess.Data;

/// <summary>
/// Service responsible for seeding initial data into the database.
/// </summary>
public class DatabaseSeeder
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        IUnitOfWork unitOfWork, 
        ILogger<DatabaseSeeder> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the database with initial required data including default admin account.
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            await SeedAdminAccountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    /// <summary>
    /// Creates a default admin account if one doesn't exist.
    /// Email: admin@mealprep.com
    /// Password: Admin123@
    /// Name: System Admin
    /// </summary>
    private async Task SeedAdminAccountAsync()
    {
        const string adminEmail = "admin@mealprep.com";
        const string adminPassword = "Admin123@";
        const string adminName = "System Admin";
        const string adminPhone = "0000000000";

        // Check if admin account already exists
        var existingAdmin = await _unitOfWork.Accounts.GetByEmailAsync(adminEmail);
        if (existingAdmin != null)
        {
            _logger.LogInformation("Admin account already exists. Skipping admin creation.");
            return;
        }

        // Get or create Admin role (RoleId = 1)
        var roles = await _unitOfWork.Roles.GetAllAsync();
        var adminRole = roles.FirstOrDefault(r => r.Id == 1 || r.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase));

        if (adminRole == null)
        {
            _logger.LogWarning("Admin role not found. Creating Admin role for admin account.");
            adminRole = new Role
            {
                Id = 1,
                Name = "Admin"
            };
            await _unitOfWork.Roles.AddAsync(adminRole);
            await _unitOfWork.SaveChangesAsync();
        }

        // Create admin account with hashed password
        // Using BCrypt with work factor 12 (same as PasswordHasher service)
        var adminAccount = new Account
        {
            Id = Guid.NewGuid().ToString(),
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword, workFactor: 12),
            FullName = adminName,
            PhoneNumber = string.Empty,
            RoleId = 1, // Admin role
            CurrentCredits = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _unitOfWork.Accounts.AddAsync(adminAccount);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Default admin account created successfully. Email: {Email}", adminEmail);
    }
}
