namespace MealPreparationService.Domain.Entities;

public class Account : BaseEntity
{
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public string? GoogleAuthId { get; set; }
    public GoogleAuth? GoogleAuth { get; set; }
    
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int CurrentCredits { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public ICollection<MealPlan> MealPlans { get; set; } = new List<MealPlan>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<HealthProfile> HealthProfiles { get; set; } = new List<HealthProfile>();
    public ICollection<Fridge> Fridges { get; set; } = new List<Fridge>();
    public ICollection<Cart> Carts { get; set; } = new List<Cart>();
    public ICollection<DeliverySchedule> DeliverySchedules { get; set; } = new List<DeliverySchedule>();
    public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
    public ICollection<AIcreditTransaction> AIcreditTransactions { get; set; } = new List<AIcreditTransaction>();
    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}




