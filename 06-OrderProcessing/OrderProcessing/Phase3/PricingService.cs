using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase3;

public class PricingService(Phase2.PricingService service)
{
    private readonly Phase2.PricingService _pricingService = service;

    public async Task<decimal> CalculateTotalAsync(
        Order order,
        CancellationToken ct)
    {
        Log.Info("PricingService", $"Order #{order.Id}: Starting checkout pricing...");

        // Step 1: Calculate subtotal first.
        decimal subTotal = await _pricingService.CalculateSubTotalAsync(order, ct);

        // Step 2: Run discount and shipping concurrently.
        Task<decimal> discountTask = _pricingService.GetDiscountAsync(order, ct);
        Task<decimal> shippingCostTask = _pricingService.CalculateShippingCostAsync(order, ct);

        decimal discount = await discountTask;
        decimal shippingCost = await shippingCostTask;

        order.SubTotal = subTotal;
        order.Discount = discount;
        order.ShippingCost = shippingCost;

        order.TotalAmount = Math.Round(
            subTotal - discount + shippingCost,
            2,
            MidpointRounding.AwayFromZero);

        Log.Success("PricingService", $"Order #{order.Id}: Total = {order.TotalAmount:F2}");

        return order.TotalAmount;
    }
}