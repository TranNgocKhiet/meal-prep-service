using MealPreparationService.Business.DTOs;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MealPreparationService.Business.Services;

public class AICreditPackageService : IAICreditPackageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AICreditPackageService> _logger;

    public AICreditPackageService(IUnitOfWork unitOfWork, ILogger<AICreditPackageService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<AICreditPackageDto>> GetAllAsync()
    {
        var packages = await _unitOfWork.AICreditPackages.GetAllQueryable()
            .OrderBy(p => p.Price)
            .ToListAsync();

        return packages.Select(MapToDto).ToList();
    }

    public async Task<AICreditPackageDto> GetByIdAsync(string id)
    {
        var package = await _unitOfWork.AICreditPackages.GetByIdAsync(id);
        
        if (package == null)
        {
            throw new KeyNotFoundException($"AI Credit Package {id} not found");
        }

        return MapToDto(package);
    }

    public async Task<AICreditPackageDto> CreateAsync(CreateAICreditPackageDto dto)
    {
        _logger.LogInformation("Creating AI Credit Package: {PackageName}", dto.PackageName);

        var package = new AIcreditPackage
        {
            Id = Guid.NewGuid().ToString(),
            PackageName = dto.PackageName,
            Price = dto.Price,
            CreditAmount = dto.CreditAmount
        };

        await _unitOfWork.AICreditPackages.AddAsync(package);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(package);
    }

    public async Task<AICreditPackageDto> UpdateAsync(string id, UpdateAICreditPackageDto dto)
    {
        _logger.LogInformation("Updating AI Credit Package: {Id}", id);

        var package = await _unitOfWork.AICreditPackages.GetByIdAsync(id);
        
        if (package == null)
        {
            throw new KeyNotFoundException($"AI Credit Package {id} not found");
        }

        package.PackageName = dto.PackageName;
        package.Price = dto.Price;
        package.CreditAmount = dto.CreditAmount;

        await _unitOfWork.SaveChangesAsync();

        return MapToDto(package);
    }

    public async Task DeleteAsync(string id)
    {
        _logger.LogInformation("Deleting AI Credit Package: {Id}", id);

        var package = await _unitOfWork.AICreditPackages.GetByIdAsync(id);
        
        if (package == null)
        {
            throw new KeyNotFoundException($"AI Credit Package {id} not found");
        }

        await _unitOfWork.AICreditPackages.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }

    private AICreditPackageDto MapToDto(AIcreditPackage package)
    {
        return new AICreditPackageDto
        {
            Id = package.Id,
            PackageName = package.PackageName,
            Price = package.Price,
            CreditAmount = package.CreditAmount
        };
    }
}
