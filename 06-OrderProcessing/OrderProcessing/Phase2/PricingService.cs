using OrderProcessing.Phase1;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase2;

public class PricingService(
    CustomerRepository customerRepository,
    ProductRepository productRepository,
    CouponRepository couponRepository,
    IEnumerable<int> failShippingForOrderIds)
{
    private readonly CustomerRepository _customerRepository = customerRepository;
    private readonly ProductRepository _productsRepository = productRepository;
    private readonly CouponRepository _couponsRepository = couponRepository;
    private HashSet<int> _failShippingForOrderIds = new HashSet<int>(failShippingForOrderIds ?? Enumerable.Empty<int>());
    
    
    public async Task<decimal> CalculateSubTotalAsync(Order order, CancellationToken ct)
    {
        Log.Info("PricingService", $"Order #{order.Id}: Opening connection...");
        await Task.Delay(200, ct);

        Log.Info("PricingService", $"Order #{order.Id}: Executing query...");
        int queryDelay = Random.Shared.Next(300, 600 + 1);
        await Task.Delay(queryDelay, ct);

        decimal subtotal = 0m;

        foreach (var item in order.Items)
        {
            ct.ThrowIfCancellationRequested();

            Product? product = await _productsRepository
                .GetByIdAsync(item.ProductId, ct);

            if (product is null)
                throw new InvalidOperationException(
                    $"Product #{item.ProductId} not found.");

            item.UnitPrice = product.Price;
            subtotal += item.UnitPrice * item.Quantity;
        }

        order.SubTotal = Math.Round(
            subtotal, 2, MidpointRounding.AwayFromZero);

        Log.Success("PricingService",
            $"Order #{order.Id}: SubTotal = {order.SubTotal:F2}");

        return order.SubTotal;
    }


    public async Task<decimal> GetDiscountAsync(Order order, CancellationToken ct)
    {
        Log.Info("PricingService", $"Order #{order.Id}: DNS/handshake...");
        await Task.Delay(150, ct);

        Log.Info("PricingService", $"Order #{order.Id}: Sending discount request...");
        await Task.Delay(300, ct);

        int responseDelay = Random.Shared.Next(500, 1500 + 1);

        Log.Info("PricingService", $"Order #{order.Id}: Waiting for discount response...");
        await Task.Delay(responseDelay, ct);

        decimal discountPercent = 0m;

        Customer? customer = await _customerRepository
            .GetByIdAsync(order.CustomerId, ct);

        if (customer is null)
            throw new InvalidOperationException(
                $"Customer #{order.CustomerId} not found.");

        discountPercent += customer.LoyaltyTier switch
        {
            LoyaltyTier.Bronze => 0m,
            LoyaltyTier.Silver => 5m,
            LoyaltyTier.Gold => 10m,
            _ => 0m
        };

        if (!string.IsNullOrWhiteSpace(order.CouponCode))
        {
            Coupon? coupon = await _couponsRepository
                .GetByCodeAsync(order.CouponCode, ct);

            if (coupon is null)
                throw new InvalidOperationException(
                    $"Coupon '{order.CouponCode}' not found.");

            if (coupon.ExpiryDate < DateTime.Today)
                throw new InvalidOperationException(
                    $"Coupon '{order.CouponCode}' has expired.");

            discountPercent += coupon.DiscountPercent;
        }

        discountPercent = Math.Min(30m, discountPercent);

        decimal discountAmount = order.SubTotal * discountPercent / 100m;

        order.Discount = Math.Round(
            discountAmount, 2, MidpointRounding.AwayFromZero);

        Log.Success("PricingService", $"Order #{order.Id}: Discount = {order.Discount:F2}");

        return order.Discount;
    }

    public async Task<decimal> CalculateShippingCostAsync(Order order, CancellationToken ct)
    {
        Log.Info("PricingService", $"Order #{order.Id}: DNS/handshake...");
        await Task.Delay(150, ct);

        if (_failShippingForOrderIds.Contains(order.Id))
        {
            Log.Error("PricingService", $"Order #{order.Id}: Shipping provider unavailable.");
            throw new ShippingProviderUnavailableException(
                "Shipping provider unavailable");
        }

        Log.Info("PricingService", $"Order #{order.Id}: Sending shipping request...");
        await Task.Delay(300, ct);

        int responseDelay = Random.Shared.Next(500, 1500 + 1);

        Log.Info("PricingService", $"Order #{order.Id}: Waiting for shipping response...");
        await Task.Delay(responseDelay, ct);

        decimal shippingCost = 0m;

        if (order.SubTotal < 1000m)
        {
            decimal totalWeight = 0m;
            int totalQuantity = 0;

            foreach (var item in order.Items)
            {
                ct.ThrowIfCancellationRequested();

                Product? product = await _productsRepository
                    .GetByIdAsync(item.ProductId, ct);

                if (product is null)
                    throw new InvalidOperationException(
                        $"Product #{item.ProductId} not found.");

                totalQuantity += item.Quantity;
                totalWeight += product.WeightKg * item.Quantity;
            }

            shippingCost = 5.00m
                           + 1.50m * totalQuantity
                           + 2.00m * totalWeight;
        }

        order.ShippingCost = Math.Round(
            shippingCost, 2, MidpointRounding.AwayFromZero);

        Log.Success("PricingService", $"Order #{order.Id}: Shipping cost = {order.ShippingCost:F2}");

        return order.ShippingCost;
    }
}