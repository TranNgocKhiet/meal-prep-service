namespace MealPreparationService.Domain.Entities;

public class Status
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Navigation properties
    public ICollection<MealPlan> MealPlans { get; set; } = new List<MealPlan>();
    public ICollection<Meal> Meals { get; set; } = new List<Meal>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<DailyMenu> DailyMenus { get; set; } = new List<DailyMenu>();
    public ICollection<PaymentGateway> PaymentGateways { get; set; } = new List<PaymentGateway>();
}
