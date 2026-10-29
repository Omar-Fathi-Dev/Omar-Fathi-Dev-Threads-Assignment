using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase2;

public class OrderPricer(PricingService pricingService)
{
    private readonly PricingService _pricingService = pricingService;



    public async Task<Order> PriceOrderAsync(Order order, CancellationToken ct)
    {
        order.SubTotal = await _pricingService.CalculateSubTotalAsync(order, ct);
        var discount =  _pricingService.GetDiscountAsync(order, ct);
        var shippingCost =   _pricingService.CalculateShippingCostAsync(order, ct);
        
        order.Discount = await discount;
        order.ShippingCost = await shippingCost;
        order.TotalAmount = Math.Round(order.SubTotal - order.Discount + order.ShippingCost, 2, MidpointRounding.AwayFromZero);
        return order;
    }

    public async Task<Order> PriceOrderSequentialAsync(Order order, CancellationToken ct)
    {
        order.SubTotal = await _pricingService.CalculateSubTotalAsync(order, ct);
        order.Discount =  await _pricingService.GetDiscountAsync(order, ct);
        order.ShippingCost = await  _pricingService.CalculateShippingCostAsync(order, ct);
        order.TotalAmount = Math.Round(order.SubTotal - order.Discount + order.ShippingCost, 2, MidpointRounding.AwayFromZero);
        return order;
    }
}