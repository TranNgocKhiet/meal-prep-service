using MealPreparationService.Business.DTOs;

namespace MealPreparationService.Business.Services;

public interface IDeliveryScheduleService
{
    Task<DeliveryScheduleDto> CreateDeliveryScheduleAsync(CreateDeliveryScheduleDto dto);
    Task<DeliveryScheduleDto> UpdateDeliveryScheduleAsync(string scheduleId, UpdateDeliveryScheduleDto dto);
    Task DeleteDeliveryScheduleAsync(string scheduleId);
    Task<DeliveryScheduleDto?> GetDeliveryScheduleByIdAsync(string scheduleId);
    Task<List<DeliveryScheduleDto>> GetAllDeliverySchedulesAsync();
    Task<List<DeliveryScheduleDto>> GetDeliverySchedulesByDriverAsync(string driverId);
    Task<List<DriverDto>> GetAvailableDriversAsync();
}
