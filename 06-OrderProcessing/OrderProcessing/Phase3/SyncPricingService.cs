using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase3;

public class SyncPricingService(Phase2.PricingService service)
{
    private readonly Phase2.PricingService _pricingService = service;

    public decimal CalculateTotal(Order order)
    {
        Log.Info("SyncPricingService", $"Order #{order.Id}: Starting checkout pricing...");

        // Step 1: Calculate subtotal first.
        decimal subTotal = _pricingService.CalculateSubTotal(order);

        // Step 2: Calculate discount and shipping cost sequentially.
        decimal discount = _pricingService.GetDiscount(order);
        decimal shippingCost = _pricingService.CalculateShippingCost(order);

        order.SubTotal = subTotal;
        order.Discount = discount;
        order.ShippingCost = shippingCost;

        order.TotalAmount = Math.Round(
            subTotal - discount + shippingCost,
            2,
            MidpointRounding.AwayFromZero);

        Log.Success("SyncPricingService", $"Order #{order.Id}: Total = {order.TotalAmount:F2}");

        return order.TotalAmount;
    }
}