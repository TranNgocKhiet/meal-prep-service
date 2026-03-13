using MealPreparationService.Domain.Services;
using MealPreparationService.Business.DTOs;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MealPreparationService.Business.Services;

public class SubscriptionPackageService : ISubscriptionPackageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SubscriptionPackageService> _logger;
    private readonly IDateTimeService _dateTimeService;

    public SubscriptionPackageService(IUnitOfWork unitOfWork, ILogger<SubscriptionPackageService> logger, IDateTimeService dateTimeService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _dateTimeService = dateTimeService;
    }

    public async Task<List<SubscriptionPackageDto>> GetAllAsync()
    {
        var packages = await _unitOfWork.SubscriptionPackages.GetAllQueryable()
            .OrderBy(p => p.Price)
            .ToListAsync();

        return packages.Select(MapToDto).ToList();
    }

    public async Task<SubscriptionPackageDto> GetByIdAsync(string id)
    {
        var package = await _unitOfWork.SubscriptionPackages.GetByIdAsync(id);
        
        if (package == null)
        {
            throw new KeyNotFoundException($"Subscription Package {id} not found");
        }

        return MapToDto(package);
    }

    public async Task<SubscriptionPackageDto> CreateAsync(CreateSubscriptionPackageDto dto)
    {
        _logger.LogInformation("Creating Subscription Package: {PackageName}", dto.PackageName);

        var package = new SubscriptionPackage
        {
            Id = Guid.NewGuid().ToString(),
            PackageName = dto.PackageName,
            Price = dto.Price,
            CreditAmount = dto.CreditAmount,
            DurationDays = dto.DurationDays,
            Description = dto.Description,
            CreatedAt = _dateTimeService.Now,
            UpdatedAt = _dateTimeService.Now
        };

        await _unitOfWork.SubscriptionPackages.AddAsync(package);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(package);
    }

    public async Task<SubscriptionPackageDto> UpdateAsync(string id, UpdateSubscriptionPackageDto dto)
    {
        _logger.LogInformation("Updating Subscription Package: {Id}", id);

        var package = await _unitOfWork.SubscriptionPackages.GetByIdAsync(id);
        
        if (package == null)
        {
            throw new KeyNotFoundException($"Subscription Package {id} not found");
        }

        package.PackageName = dto.PackageName;
        package.Price = dto.Price;
        package.CreditAmount = dto.CreditAmount;
        package.DurationDays = dto.DurationDays;
        package.Description = dto.Description;
        package.UpdatedAt = _dateTimeService.Now;

        await _unitOfWork.SaveChangesAsync();

        return MapToDto(package);
    }

    public async Task DeleteAsync(string id)
    {
        _logger.LogInformation("Deleting Subscription Package: {Id}", id);

        var package = await _unitOfWork.SubscriptionPackages.GetByIdAsync(id);
        
        if (package == null)
        {
            throw new KeyNotFoundException($"Subscription Package {id} not found");
        }

        await _unitOfWork.SubscriptionPackages.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }

    private SubscriptionPackageDto MapToDto(SubscriptionPackage package)
    {
        return new SubscriptionPackageDto
        {
            Id = package.Id,
            PackageName = package.PackageName,
            Price = package.Price,
            CreditAmount = package.CreditAmount,
            DurationDays = package.DurationDays,
            Description = package.Description,
            CreatedAt = package.CreatedAt,
            UpdatedAt = package.UpdatedAt
        };
    }
}


