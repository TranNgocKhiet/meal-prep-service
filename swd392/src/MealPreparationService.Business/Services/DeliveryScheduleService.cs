using MealPreparationService.Business.DTOs;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MealPreparationService.Business.Services;

public class DeliveryScheduleService : IDeliveryScheduleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeliveryScheduleService> _logger;

    public DeliveryScheduleService(
        IUnitOfWork unitOfWork,
        ILogger<DeliveryScheduleService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<DeliveryScheduleDto> CreateDeliveryScheduleAsync(CreateDeliveryScheduleDto dto)
    {
        _logger.LogInformation("Creating delivery schedule for order {OrderId} with driver {DriverId}", 
            dto.OrderId, dto.DriverId);

        // Verify driver exists and has Deliveryman role (RoleId = 5)
        var driver = await _unitOfWork.Accounts.GetAllQueryable()
            .Include(a => a.Role)
            .FirstOrDefaultAsync(a => a.Id == dto.DriverId);

        if (driver == null)
        {
            throw new KeyNotFoundException($"Driver with ID {dto.DriverId} not found");
        }

        if (driver.RoleId != 5)
        {
            throw new InvalidOperationException("Selected user is not a deliveryman");
        }

        // Verify order exists and is prepared
        var order = await _unitOfWork.Orders.GetAllQueryable()
            .Include(o => o.Customer)
            .Include(o => o.Status)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order with ID {dto.OrderId} not found");
        }

        if (order.StatusId != 7) // StatusId 7 = Prepared
        {
            throw new InvalidOperationException("Only prepared orders can be scheduled for delivery");
        }

        // Check if order already has a delivery schedule
        var existingSchedule = await _unitOfWork.DeliverySchedules.GetAllQueryable()
            .FirstOrDefaultAsync(ds => ds.OrderId == dto.OrderId);

        if (existingSchedule != null)
        {
            throw new InvalidOperationException("This order already has a delivery schedule");
        }

        // Create delivery schedule
        var schedule = new DeliverySchedule
        {
            Id = Guid.NewGuid().ToString(),
            DriverId = dto.DriverId,
            OrderId = dto.OrderId,
            DeliveryTime = dto.DeliveryTime,
            Address = dto.Address,
            DriverContact = dto.DriverContact,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        schedule = await _unitOfWork.DeliverySchedules.AddAsync(schedule);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Successfully created delivery schedule {ScheduleId}", schedule.Id);

        return await GetDeliveryScheduleByIdAsync(schedule.Id) 
            ?? throw new InvalidOperationException("Failed to retrieve created delivery schedule");
    }

    public async Task<DeliveryScheduleDto> UpdateDeliveryScheduleAsync(string scheduleId, UpdateDeliveryScheduleDto dto)
    {
        _logger.LogInformation("Updating delivery schedule {ScheduleId}", scheduleId);

        var schedule = await _unitOfWork.DeliverySchedules.GetByIdAsync(scheduleId);
        if (schedule == null)
        {
            throw new KeyNotFoundException($"Delivery schedule {scheduleId} not found");
        }

        // Update fields if provided
        if (!string.IsNullOrEmpty(dto.DriverId))
        {
            var driver = await _unitOfWork.Accounts.GetAllQueryable()
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.Id == dto.DriverId);

            if (driver == null)
            {
                throw new KeyNotFoundException($"Driver with ID {dto.DriverId} not found");
            }

            if (driver.Role.Name != "Staff" && driver.Role.Name != "Admin")
            {
                throw new InvalidOperationException("Selected user is not a staff member or admin");
            }

            schedule.DriverId = dto.DriverId;
        }

        if (dto.DeliveryTime.HasValue)
        {
            schedule.DeliveryTime = dto.DeliveryTime.Value;
        }

        if (!string.IsNullOrEmpty(dto.Address))
        {
            schedule.Address = dto.Address;
        }

        if (!string.IsNullOrEmpty(dto.DriverContact))
        {
            schedule.DriverContact = dto.DriverContact;
        }

        schedule.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Successfully updated delivery schedule {ScheduleId}", scheduleId);

        return await GetDeliveryScheduleByIdAsync(scheduleId) 
            ?? throw new InvalidOperationException("Failed to retrieve updated delivery schedule");
    }

    public async Task DeleteDeliveryScheduleAsync(string scheduleId)
    {
        _logger.LogInformation("Deleting delivery schedule {ScheduleId}", scheduleId);

        var schedule = await _unitOfWork.DeliverySchedules.GetByIdAsync(scheduleId);
        if (schedule == null)
        {
            throw new KeyNotFoundException($"Delivery schedule {scheduleId} not found");
        }

        await _unitOfWork.DeliverySchedules.DeleteAsync(scheduleId);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Successfully deleted delivery schedule {ScheduleId}", scheduleId);
    }

    public async Task<DeliveryScheduleDto?> GetDeliveryScheduleByIdAsync(string scheduleId)
    {
        var schedule = await _unitOfWork.DeliverySchedules.GetAllQueryable()
            .Include(ds => ds.Driver)
            .Include(ds => ds.Order)
                .ThenInclude(o => o.Customer)
            .Include(ds => ds.Order)
                .ThenInclude(o => o.Status)
            .FirstOrDefaultAsync(ds => ds.Id == scheduleId);

        if (schedule == null)
        {
            return null;
        }

        return MapToDto(schedule);
    }

    public async Task<List<DeliveryScheduleDto>> GetAllDeliverySchedulesAsync()
    {
        var schedules = await _unitOfWork.DeliverySchedules.GetAllQueryable()
            .Include(ds => ds.Driver)
            .Include(ds => ds.Order)
                .ThenInclude(o => o.Customer)
            .Include(ds => ds.Order)
                .ThenInclude(o => o.Status)
            .OrderByDescending(ds => ds.DeliveryTime)
            .ToListAsync();

        return schedules.Select(MapToDto).ToList();
    }

    public async Task<List<DeliveryScheduleDto>> GetDeliverySchedulesByDriverAsync(string driverId)
    {
        var schedules = await _unitOfWork.DeliverySchedules.GetAllQueryable()
            .Where(ds => ds.DriverId == driverId)
            .Include(ds => ds.Driver)
            .Include(ds => ds.Order)
                .ThenInclude(o => o.Customer)
            .Include(ds => ds.Order)
                .ThenInclude(o => o.Status)
            .OrderByDescending(ds => ds.DeliveryTime)
            .ToListAsync();

        return schedules.Select(MapToDto).ToList();
    }

    public async Task<List<DriverDto>> GetAvailableDriversAsync()
    {
        // Filter by RoleId = 5 (Deliveryman)
        var drivers = await _unitOfWork.Accounts.GetAllQueryable()
            .Include(a => a.Role)
            .Where(a => a.RoleId == 5 && a.IsActive)
            .OrderBy(a => a.FullName)
            .ToListAsync();

        return drivers.Select(d => new DriverDto
        {
            Id = d.Id,
            FullName = d.FullName,
            Email = d.Email,
            PhoneNumber = d.PhoneNumber
        }).ToList();
    }

    private DeliveryScheduleDto MapToDto(DeliverySchedule schedule)
    {
        return new DeliveryScheduleDto
        {
            Id = schedule.Id,
            DriverId = schedule.DriverId,
            DriverName = schedule.Driver.FullName,
            DriverEmail = schedule.Driver.Email,
            DriverContact = schedule.DriverContact,
            OrderId = schedule.OrderId,
            OrderNumber = schedule.Order.Id.Substring(0, 8).ToUpper(),
            OrderTotal = schedule.Order.Amount,
            OrderStatus = schedule.Order.Status.Name,
            Address = schedule.Address,
            CustomerName = schedule.Order.Customer.FullName,
            CustomerPhone = schedule.Order.PhoneNumber ?? "N/A",
            DeliveryTime = schedule.DeliveryTime,
            CreatedAt = schedule.CreatedAt,
            UpdatedAt = schedule.UpdatedAt
        };
    }
}
