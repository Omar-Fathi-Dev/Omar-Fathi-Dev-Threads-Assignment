
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase4;

public class CairoExpressCarrier : IShippingCarrier
{
    private readonly List<Product> _products;
    private readonly HashSet<int> _failForOrderIds;

    public string Name => "CairoExpress";

    public CairoExpressCarrier(
        IEnumerable<Product> products,
        IEnumerable<int>? failForOrderIds = null)
    {
        _products = new List<Product>(products);

        _failForOrderIds = failForOrderIds?.ToHashSet()
            ?? new HashSet<int>();
    }

    public async Task<decimal> GetQuoteAsync(
        Order order,
        CancellationToken ct)
    {
        try
        {
            await Task.Delay(150, ct);

            if (order.Id % 2 == 0 ||
                _failForOrderIds.Contains(order.Id))
            {
                throw new ShippingProviderUnavailableException(
                    "CairoExpress is unavailable");
            }

            int responseDelay = Random.Shared.Next(300, 2001);

            await Task.Delay(responseDelay, ct);

            decimal basePrice = 0m;

            if (order.SubTotal < 1000m)
            {
                decimal totalWeight = 0m;
                int totalQuantity = 0;

                foreach (OrderItem item in order.Items)
                {
                    ct.ThrowIfCancellationRequested();

                    Product? product =
                        _products.Find(p => p.Id == item.ProductId);

                    if (product is null)
                    {
                        throw new InvalidOperationException(
                            $"Product #{item.ProductId} not found.");
                    }

                    totalQuantity += item.Quantity;
                    totalWeight += product.WeightKg * item.Quantity;
                }

                basePrice =
                    5.00m +
                    (1.50m * totalQuantity) +
                    (2.00m * totalWeight);
            }

            return Math.Round(
                basePrice * 0.95m,
                2,
                MidpointRounding.AwayFromZero);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[CairoExpress] cancelled");
            throw;
        }
    }
}
