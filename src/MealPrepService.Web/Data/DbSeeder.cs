using Microsoft.EntityFrameworkCore;
using MealPrepService.DataAccessLayer.Data;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Services;

namespace MealPrepService.Web.Data;

public class DbSeeder
{
    private readonly MealPrepDbContext _context;
    private readonly ILogger<DbSeeder> _logger;
    private readonly IPasswordHasher _passwordHasher;
    private readonly string _filesDirectory;

    public DbSeeder(
        MealPrepDbContext context, 
        ILogger<DbSeeder> logger, 
        IPasswordHasher passwordHasher,
        string filesDirectory = "files")
    {
        _context = context;
        _logger = logger;
        _passwordHasher = passwordHasher;
        _filesDirectory = filesDirectory;
    }

    public async Task SeedAsync()
    {
        try
        {
            // Ensure database is created (use EnsureCreated for in-memory, Migrate for relational)
            var providerName = _context.Database.ProviderName;
            if (providerName != null && providerName.Contains("InMemory"))
            {
                await _context.Database.EnsureCreatedAsync();
            }
            else
            {
                await _context.Database.MigrateAsync();
            }

            // Seed admin account if not exists
            await SeedAdminAccountAsync();

            // Import dataset from Excel files if available
            await ImportDatasetAsync();

            // Seed sample ingredients (only if dataset import didn't run)
            await SeedIngredientsAsync();

            await _context.SaveChangesAsync();
            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private async Task SeedAdminAccountAsync()
    {
        if (await _context.Accounts.AnyAsync(a => a.Role == "Admin"))
        {
            _logger.LogInformation("Admin account already exists, skipping seed");
            return;
        }

        var adminAccount = new Account
        {
            Id = Guid.NewGuid(),
            Email = "admin@mealprep.com",
            // Password: Admin@123
            PasswordHash = _passwordHasher.HashPassword("Admin@123"),
            FullName = "System Administrator",
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        };

        await _context.Accounts.AddAsync(adminAccount);
        _logger.LogInformation("Admin account seeded: {Email}", adminAccount.Email);
    }

    private async Task ImportDatasetAsync()
    {
        // Check if dataset files exist (try both .xlsx and .xls extensions)
        var datasetAllergyPath = Path.Combine(_filesDirectory, "Dataset_Allergy.xlsx");
        if (!File.Exists(datasetAllergyPath))
        {
            datasetAllergyPath = Path.Combine(_filesDirectory, "Dataset_Allergy.xls");
        }
        
        if (!File.Exists(datasetAllergyPath))
        {
            _logger.LogInformation("Dataset files not found in '{Directory}', skipping dataset import", _filesDirectory);
            return;
        }

        // Check if data already exists (skip if already imported)
        if (await _context.Allergies.AnyAsync() || 
            await _context.Ingredients.AnyAsync() || 
            await _context.Recipes.AnyAsync())
        {
            _logger.LogInformation("Database already contains data, skipping dataset import");
            return;
        }

        try
        {
            _logger.LogInformation("Starting dataset import from '{Directory}'", _filesDirectory);
            
            var importer = new DatasetImporter(_context, _filesDirectory);
            await importer.ImportAllAsync();
            
            _logger.LogInformation("Dataset import completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dataset import failed: {Message}", ex.Message);
            // Don't rethrow - allow seeding to continue with sample data
        }
    }

    private async Task SeedIngredientsAsync()
    {
        if (await _context.Ingredients.AnyAsync())
        {
            _logger.LogInformation("Ingredients already exist, skipping seed");
            return;
        }

        var ingredients = new List<Ingredient>
        {
            // Proteins
            new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = "Chicken Breast",
                Unit = "g",
                CaloPerUnit = 1.65f,
                IsAllergen = false,
                CreatedAt = DateTime.UtcNow
            },
            new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = "Salmon",
                Unit = "g",
                CaloPerUnit = 2.08f,
                IsAllergen = false,
                CreatedAt = DateTime.UtcNow
            },
            new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = "Eggs",
                Unit = "piece",
                CaloPerUnit = 78f,
                IsAllergen = true, // Common allergen
                CreatedAt = DateTime.UtcNow
            },
            new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = "Tofu",
                Unit = "g",
                CaloPerUnit = 0.76f,
                IsAllergen = false,
                CreatedAt = DateTime.UtcNow
            },

            // Carbohydrates
            new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = "Brown Rice",
                Unit = "g",
                CaloPerUnit = 1.11f,
                IsAllergen = false,
                CreatedAt = DateTime.UtcNow
            },
            new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = "Quinoa",
                Unit = "g",
                CaloPerUnit = 1.20f,
                IsAllergen = false,
                CreatedAt = DateTime.UtcNow
            },
            new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = "Sweet Potato",
                Unit = "g",
                CaloPerUnit = 0.86f,
                IsAllergen = false,
                CreatedAt = DateTime.UtcNow
            },
            new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = "Whole Wheat Bread",
                Unit = "slice",
                CaloPerUnit = 80f,
                IsAllergen = true, // Contains gluten
                CreatedAt = DateTime.UtcNow
            },

            // Vegetables
            new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = "Broccoli",
                Unit = "g",
                CaloPerUnit = 0.34f,
                IsAllergen = false,
                CreatedAt = DateTime.UtcNow
            },
            new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = "Spinach",
                Unit = "g",
                CaloPerUnit = 0.23f,
                IsAllergen = false,
                CreatedAt = DateTime.UtcNow
            },
            new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = "Carrots",
                Unit = "g",
                CaloPerUnit = 0.41f,
                IsAllergen = false,
                CreatedAt = DateTime.UtcNow
            },
            new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = "Bell Peppers",
                Unit = "g",
                CaloPerUnit = 0.31f,
                IsAllergen = false,
                CreatedAt = DateTime.UtcNow
            },
            new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = "Tomatoes",
                Unit = "g",
                CaloPerUnit = 0.18f,
                IsAllergen = false,
                CreatedAt = DateTime.UtcNow
            },

            // Fruits
            new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = "Banana",
                Unit = "piece",
                CaloPerUnit = 105f,
                IsAllergen = false,
                CreatedAt = DateTime.UtcNow
            },
            new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = "Apple",
                Unit = "piece",
                CaloPerUnit = 95f,
                IsAllergen = false,
                CreatedAt = DateTime.UtcNow
            },
            new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = "Blueberries",
                Unit = "g",
                CaloPerUnit = 0.57f,
                IsAllergen = false,
                CreatedAt = DateTime.UtcNow
            },

            // Dairy
            new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = "Greek Yogurt",
                Unit = "g",
                CaloPerUnit = 0.59f,
                IsAllergen = true, // Dairy allergen
                CreatedAt = DateTime.UtcNow
            },
            new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = "Milk",
                Unit = "ml",
                CaloPerUnit = 0.42f,
                IsAllergen = true, // Dairy allergen
                CreatedAt = DateTime.UtcNow
            },
            new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = "Cheese",
                Unit = "g",
                CaloPerUnit = 4.02f,
                IsAllergen = true, // Dairy allergen
                CreatedAt = DateTime.UtcNow
            },

            // Nuts and Seeds
            new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = "Almonds",
                Unit = "g",
                CaloPerUnit = 5.79f,
                IsAllergen = true, // Nut allergen
                CreatedAt = DateTime.UtcNow
            },
            new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = "Chia Seeds",
                Unit = "g",
                CaloPerUnit = 4.86f,
                IsAllergen = false,
                CreatedAt = DateTime.UtcNow
            },

            // Oils and Fats
            new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = "Olive Oil",
                Unit = "ml",
                CaloPerUnit = 8.84f,
                IsAllergen = false,
                CreatedAt = DateTime.UtcNow
            },
            new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = "Avocado",
                Unit = "piece",
                CaloPerUnit = 240f,
                IsAllergen = false,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.Ingredients.AddRangeAsync(ingredients);
        _logger.LogInformation("Seeded {Count} sample ingredients", ingredients.Count);
    }
}
