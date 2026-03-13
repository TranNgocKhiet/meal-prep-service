namespace MealPreparationService.Domain.Services;

public interface IDateTimeService
{
    DateTime Now { get; }
    DateTime Today { get; }
}

public class DateTimeService : IDateTimeService
{
    private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
    
    public DateTime Today => Now.Date;
}
