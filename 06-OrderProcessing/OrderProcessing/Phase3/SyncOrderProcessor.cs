using OrderProcessing.Phase1;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase3;

public class SyncOrderProcessor(
    SyncCustomerService customerService,
    SyncInventoryService inventoryService,
    SyncPricingService pricingService,
    PaymentGateway paymentGateway,
    OrderRepository orderRepository,
    EmailService emailService,
    ProductRepository productRepository)
{
    private readonly SyncCustomerService _customerService = customerService;
    private readonly SyncInventoryService _inventoryService = inventoryService;
    private readonly SyncPricingService _pricingService = pricingService;
    private readonly PaymentGateway _paymentGateway = paymentGateway;
    private readonly OrderRepository _orderRepository = orderRepository;
    private readonly EmailService _emailService = emailService;
    private readonly ProductRepository _productRepository = productRepository;

    public bool ProcessOrder(Order order)
    {
        bool stockReserved = false;
        string? transactionId = null;

        Log.Info(
            "SyncOrderProcessor",
            $"Order #{order.Id}: Starting synchronous processing...");

        try
        {
            // 1. Validate Customer
            Customer customer;

            try
            {
                customer = _customerService.GetCustomer(order.CustomerId);
            }
            catch (CustomerNotFoundException ex)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = ex.Message;

                _orderRepository.Save(order);

                return false;
            }

            if (!customer.IsActive)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = "Customer is inactive";

                _orderRepository.Save(order);

                return false;
            }

            // 2. Reserve Stock
            stockReserved = _inventoryService.ReserveStock(order);

            if (!stockReserved)
            {
                foreach (OrderItem item in order.Items)
                {
                    Product? product =
                        _productRepository.GetById(item.ProductId);

                    if (product is null || product.StockQuantity < item.Quantity)
                    {
                        string productName =
                            product?.Name ?? $"Product #{item.ProductId}";

                        order.Status = OrderStatus.Rejected;
                        order.FailureReason =
                            $"Insufficient stock for '{productName}'";

                        _orderRepository.Save(order);

                        return false;
                    }
                }

                order.Status = OrderStatus.Rejected;
                order.FailureReason = "Insufficient stock";

                _orderRepository.Save(order);

                return false;
            }

            // 3. Calculate Pricing
            try
            {
                _pricingService.CalculateTotal(order);
            }
            catch (InvalidCouponException ex)
            {
                _inventoryService.ReleaseStock(order);
                stockReserved = false;

                order.Status = OrderStatus.Rejected;
                order.FailureReason = ex.Message;

                _orderRepository.Save(order);

                return false;
            }

            // 4. Process Payment
            PaymentResult paymentResult =
                _paymentGateway.ProcessPayment(order.Id, order.TotalAmount);

            if (!paymentResult.Success)
            {
                _inventoryService.ReleaseStock(order);
                stockReserved = false;

                order.Status = OrderStatus.PaymentFailed;
                order.FailureReason = "Card declined by issuer";

                _orderRepository.Save(order);

                return false;
            }

            transactionId = paymentResult.TransactionId;
            order.TransactionId = transactionId;

            // 5. Save Completed Order
            order.Status = OrderStatus.Completed;
            order.FailureReason = null;

            try
            {
                _orderRepository.Save(order);
            }
            catch (DatabaseWriteException)
            {
                Log.Error(
                    "SyncOrderProcessor",
                    $"Order #{order.Id}: Database save failed after payment. Compensating...");

                if (transactionId is not null)
                    _paymentGateway.Refund(transactionId);

                if (stockReserved)
                {
                    _inventoryService.ReleaseStock(order);
                    stockReserved = false;
                }

                order.Status = OrderStatus.Failed;
                order.FailureReason =
                    $"Database save failed after payment; payment {transactionId} refunded";

                _orderRepository.Save(order);

                return false;
            }

            // 6. Send Confirmation Email
            _emailService.SendConfirmationWithRetry(order, customer);

            Log.Success(
                "SyncOrderProcessor",
                $"Order #{order.Id}: Processing completed successfully.");

            return order.Status == OrderStatus.Completed;
        }
        catch (Exception ex)
        {
            Log.Error(
                "SyncOrderProcessor",
                $"Order #{order.Id}: Unexpected error: {ex.Message}");

            if (transactionId is not null)
                _paymentGateway.Refund(transactionId);

            if (stockReserved)
                _inventoryService.ReleaseStock(order);

            order.Status = OrderStatus.Failed;
            order.FailureReason = ex.Message;

            _orderRepository.Save(order);

            return false;
        }
    }
}