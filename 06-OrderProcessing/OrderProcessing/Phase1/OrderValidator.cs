
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase1;

public class OrderValidator(
    CustomerRepository customerRepository,
    ProductRepository productRepository,
    CouponRepository couponRepository,
    OrderRepository orderRepository)
{
    private readonly CustomerRepository _customerRepository = customerRepository;
    private readonly ProductRepository _productsRepository = productRepository;
    private readonly CouponRepository _couponsRepository = couponRepository;
    private readonly OrderRepository _orderRepository = orderRepository;

    public async Task ValidateOrderAsync(Order order, CancellationToken ct)
    {
        Log.Info("OrderValidator", "Starting order validation");
        await Task.Delay(200, ct);

        Log.Info("OrderValidator", "Checking order data");
        int delayMilliseconds = Random.Shared.Next(300, 601);
        await Task.Delay(delayMilliseconds, ct);

        Customer? customer = await _customerRepository.GetByIdAsync(
            order.CustomerId, ct);

        if (customer is null)
        {
            order.FailureReason = $"Customer #{order.CustomerId} not found";
            await SaveRejectedOrderAsync(order, ct);
            return;
        }

        if (!customer.IsActive)
        {
            order.FailureReason = "Customer is inactive";
            await SaveRejectedOrderAsync(order, ct);
            return;
        }

        foreach (var item in order.Items)
        {
            Product? product = await _productsRepository.GetByIdAsync(
                item.ProductId, ct);

            if (product is null)
            {
                order.FailureReason = $"Product #{item.ProductId} not found";
                await SaveRejectedOrderAsync(order, ct);
                return;
            }

            if (product.StockQuantity < item.Quantity)
            {
                order.FailureReason =
                    $"Insufficient stock for '{product.Name}'";

                await SaveRejectedOrderAsync(order, ct);
                return;
            }
        }

        if (order.CouponCode is not null)
        {
            Coupon? coupon = await _couponsRepository.GetByCodeAsync(
                order.CouponCode, ct);

            if (coupon is null)
            {
                order.FailureReason =
                    $"Coupon '{order.CouponCode}' does not exist";

                await SaveRejectedOrderAsync(order, ct);
                return;
            }

            if (coupon.ExpiryDate < DateTime.Today)
            {
                order.FailureReason =
                    $"Coupon '{order.CouponCode}' has expired";

                await SaveRejectedOrderAsync(order, ct);
                return;
            }
        }

        order.Status = OrderStatus.Validated;
        order.FailureReason = null;

        await _orderRepository.SaveAsync(order, ct);

        Log.Success("OrderValidator",
            $"Order {order.Id} validated successfully");
    }

    private async Task SaveRejectedOrderAsync(
        Order order, CancellationToken ct)
    {
        order.Status = OrderStatus.Rejected;

        Log.Error("OrderValidator", order.FailureReason!);

        await _orderRepository.SaveAsync(order, ct);
    }
}
