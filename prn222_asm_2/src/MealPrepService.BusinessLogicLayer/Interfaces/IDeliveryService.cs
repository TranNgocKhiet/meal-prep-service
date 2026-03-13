using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Interfaces;

public interface IDeliveryService
{
    Task<DeliveryScheduleDto> CreateDeliveryScheduleAsync(Guid orderId, DeliveryScheduleDto dto);
    Task<IEnumerable<DeliveryScheduleDto>> GetByAccountIdAsync(Guid accountId);
    Task<IEnumerable<DeliveryScheduleDto>> GetByDeliveryManAsync(Guid deliveryManId);
    Task<IEnumerable<DeliveryScheduleDto>> GetAllForOperationsAsync();
    Task AssignDeliveryManAsync(Guid deliveryId, Guid deliveryManId);
    Task AcceptDeliveryAsync(Guid deliveryId, Guid deliveryManId);
    Task CompleteDeliveryAsync(Guid deliveryId, Guid deliveryManId, string resultStatus);
    Task UpdateDeliveryTimeAsync(Guid deliveryId, DateTime newTime);
}