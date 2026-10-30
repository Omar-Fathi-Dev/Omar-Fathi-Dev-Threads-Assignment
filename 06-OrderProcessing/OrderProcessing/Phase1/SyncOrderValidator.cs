
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase1;

public class SyncOrderValidator(
    SyncCustomerRepository customerRepository,
    SyncProductRepository productRepository,
    SyncCouponRepository couponRepository)
{
    private readonly SyncCustomerRepository _customerRepository = customerRepository;
    private readonly SyncProductRepository _productsRepository = productRepository;
    private readonly SyncCouponRepository _couponsRepository = couponRepository;

    public void ValidateOrder(Order order)
    {
        Log.Info("SyncOrderValidator", "Starting order validation");
        Thread.Sleep(200);

        Log.Info("SyncOrderValidator", "Checking order data");
        int delayMilliseconds = Random.Shared.Next(300, 601);
        Thread.Sleep(delayMilliseconds);

        Customer? customer = _customerRepository.GetById(order.CustomerId);

        if (customer is null)
        {
            order.FailureReason = $"Customer #{order.CustomerId} not found";
            SaveRejectedOrder(order);
            return;
        }

        if (!customer.IsActive)
        {
            order.FailureReason = "Customer is inactive";
            SaveRejectedOrder(order);
            return;
        }

        foreach (var item in order.Items)
        {
            Product? product = _productsRepository.GetById(item.ProductId);

            if (product is null)
            {
                order.FailureReason = $"Product #{item.ProductId} not found";
                SaveRejectedOrder(order);
                return;
            }

            if (product.StockQuantity < item.Quantity)
            {
                order.FailureReason =
                    $"Insufficient stock for '{product.Name}'";

                SaveRejectedOrder(order);
                return;
            }
        }

        if (order.CouponCode is not null)
        {
            Coupon? coupon = _couponsRepository.GetByCode(order.CouponCode);

            if (coupon is null)
            {
                order.FailureReason =
                    $"Coupon '{order.CouponCode}' does not exist";

                SaveRejectedOrder(order);
                return;
            }

            if (coupon.ExpiryDate < DateTime.Today)
            {
                order.FailureReason =
                    $"Coupon '{order.CouponCode}' has expired";

                SaveRejectedOrder(order);
                return;
            }
        }

        order.Status = OrderStatus.Validated;
        order.FailureReason = null;

        Log.Success("SyncOrderValidator",
            $"Order {order.Id} validated successfully");
    }

    private void SaveRejectedOrder(Order order)
    {
        order.Status = OrderStatus.Rejected;

        Log.Error("SyncOrderValidator", order.FailureReason!);
    }
}
