using MealPreparationService.Business.DTOs;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;
using MealPreparationService.Domain.Services;
using Microsoft.Extensions.Logging;

namespace MealPreparationService.Business.Services;

public class FeedbackService : IFeedbackService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FeedbackService> _logger;
    private readonly IDateTimeService _dateTimeService;

    public FeedbackService(
        IUnitOfWork unitOfWork,
        ILogger<FeedbackService> logger,
        IDateTimeService dateTimeService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _dateTimeService = dateTimeService;
    }

    public async Task<FeedbackDto> CreateFeedbackAsync(CreateFeedbackDto dto, string customerId)
    {
        _logger.LogInformation("Creating feedback for customer {CustomerId}", customerId);

        // Verify customer exists
        var customer = await _unitOfWork.Accounts.GetByIdAsync(customerId);
        if (customer == null)
        {
            throw new KeyNotFoundException($"Customer {customerId} not found");
        }

        var feedback = new Feedback
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = customerId,
            Title = dto.Title,
            Content = dto.Content,
            CreatedAt = _dateTimeService.Now,
            UpdatedAt = _dateTimeService.Now
        };

        await _unitOfWork.Feedbacks.AddAsync(feedback);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Feedback created successfully with ID {FeedbackId}", feedback.Id);

        return MapToDto(feedback, customer);
    }

    public async Task<FeedbackDto?> GetFeedbackByIdAsync(string feedbackId)
    {
        _logger.LogInformation("Getting feedback {FeedbackId}", feedbackId);

        var feedback = await _unitOfWork.Feedbacks.GetByIdAsync(feedbackId);
        if (feedback == null)
        {
            return null;
        }

        var customer = await _unitOfWork.Accounts.GetByIdAsync(feedback.CustomerId);
        return customer != null ? MapToDto(feedback, customer) : null;
    }

    public async Task<List<FeedbackDto>> GetCustomerFeedbacksAsync(string customerId)
    {
        _logger.LogInformation("Getting feedbacks for customer {CustomerId}", customerId);

        var feedbacks = await _unitOfWork.Feedbacks.GetByCustomerIdAsync(customerId);
        var customer = await _unitOfWork.Accounts.GetByIdAsync(customerId);
        
        if (customer == null)
        {
            return new List<FeedbackDto>();
        }

        return feedbacks.Select(f => MapToDto(f, customer)).ToList();
    }

    public async Task<FeedbackListDto> GetAllFeedbacksAsync(int page = 1, int pageSize = 10)
    {
        _logger.LogInformation("Getting all feedbacks with pagination - Page: {Page}, PageSize: {PageSize}", page, pageSize);

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var skip = (page - 1) * pageSize;
        var feedbacks = await _unitOfWork.Feedbacks.GetAllFeedbacksAsync(skip, pageSize);
        var totalCount = await _unitOfWork.Feedbacks.CountAsync();

        var feedbackDtos = new List<FeedbackDto>();
        foreach (var feedback in feedbacks)
        {
            var customer = feedback.Customer ?? await _unitOfWork.Accounts.GetByIdAsync(feedback.CustomerId);
            if (customer != null)
            {
                feedbackDtos.Add(MapToDto(feedback, customer));
            }
        }

        return new FeedbackListDto
        {
            Feedbacks = feedbackDtos,
            Total = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private FeedbackDto MapToDto(Feedback feedback, Account customer)
    {
        return new FeedbackDto
        {
            Id = feedback.Id,
            CustomerId = feedback.CustomerId,
            CustomerName = customer.FullName,
            Title = feedback.Title,
            Content = feedback.Content,
            CreatedAt = feedback.CreatedAt,
            UpdatedAt = feedback.UpdatedAt
        };
    }
}
