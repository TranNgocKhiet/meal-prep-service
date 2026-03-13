namespace MealPreparationService.Business.DTOs;

public class AICreditTransactionDto
{
    public string Id { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string AIcreditPackageId { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public int CreditAmount { get; set; }
    public decimal Price { get; set; }
    public string PaymentGatewayId { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class PurchaseAICreditDto
{
    public string AIcreditPackageId { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "VNPay";
}

public class AICreditPurchaseResponseDto
{
    public string TransactionId { get; set; } = string.Empty;
    public string? PaymentUrl { get; set; }
    public bool RequiresPayment { get; set; }
}
