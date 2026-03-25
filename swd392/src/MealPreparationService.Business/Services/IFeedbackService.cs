using MealPreparationService.Business.DTOs;

namespace MealPreparationService.Business.Services;

public interface IFeedbackService
{
    Task<FeedbackDto> CreateFeedbackAsync(CreateFeedbackDto dto, string customerId);
    Task<FeedbackDto?> GetFeedbackByIdAsync(string feedbackId);
    Task<List<FeedbackDto>> GetCustomerFeedbacksAsync(string customerId);
    Task<FeedbackListDto> GetAllFeedbacksAsync(int page = 1, int pageSize = 10);
}
