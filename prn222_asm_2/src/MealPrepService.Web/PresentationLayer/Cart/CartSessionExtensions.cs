using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace MealPrepService.Web.PresentationLayer.Cart;

public sealed class CartItemSession
{
    public Guid MenuMealId { get; set; }
    public int Quantity { get; set; }
}

public static class CartSessionExtensions
{
    public const string CartSessionKey = "CustomerCart";

    public static List<CartItemSession> GetCartItems(this ISession session)
    {
        var cartJson = session.GetString(CartSessionKey);
        if (string.IsNullOrWhiteSpace(cartJson))
        {
            return new List<CartItemSession>();
        }

        return JsonSerializer.Deserialize<List<CartItemSession>>(cartJson) ?? new List<CartItemSession>();
    }

    public static void SaveCartItems(this ISession session, List<CartItemSession> items)
    {
        session.SetString(CartSessionKey, JsonSerializer.Serialize(items));
    }

    public static void ClearCart(this ISession session)
    {
        session.Remove(CartSessionKey);
    }
}
