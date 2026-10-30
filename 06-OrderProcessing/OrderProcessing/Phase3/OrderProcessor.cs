using System.Diagnostics;
using OrderProcessing.Phase1;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase3;

public class OrderProcessor(
    CustomerService customerService,
    InventoryService inventoryService,
    PricingService pricingService,
    IPaymentGateway paymentGateway,
    OrderRepository orderRepository,
    IEmailService emailService,
    ProductRepository productRepository)
{
    private readonly CustomerService _customerService = customerService;
    private readonly InventoryService _inventoryService = inventoryService;
    private readonly PricingService _pricingService = pricingService;
    private readonly IPaymentGateway _paymentGateway = paymentGateway;
    private readonly OrderRepository _orderRepository = orderRepository;
    private readonly IEmailService _emailService = emailService;
    private readonly ProductRepository _productRepository = productRepository;
    
    
    
    


public async Task<bool> ProcessOrderAsync(Order order, CancellationToken ct)
{
    bool stockReserved = false;
    string? transactionId = null;

    try
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        // Step 1: Get Customer
        Log.Info("OrderProcessor",
            $"Order #{order.Id} -> Step 1/6: Getting customer...");

        Customer customer;

        try
        {
            customer = await _customerService.GetCustomerAsync(
                order.CustomerId, ct);
        }
        catch (CustomerNotFoundException e)
        {
            order.Status = OrderStatus.Rejected;
            order.FailureReason = e.Message;

            await _orderRepository.SaveAsync(order, ct);

            stopwatch.Stop();

            Log.Error("OrderProcessor",
                $"Order #{order.Id} -> Step 1/6: Customer not found. Rejected in {stopwatch.ElapsedMilliseconds} ms.");

            return false;
        }

        if (!customer.IsActive)
        {
            order.Status = OrderStatus.Rejected;
            order.FailureReason = "Customer is inactive";

            await _orderRepository.SaveAsync(order, ct);

            stopwatch.Stop();

            Log.Error("OrderProcessor",
                $"Order #{order.Id} -> Step 1/6: Customer is inactive. Rejected in {stopwatch.ElapsedMilliseconds} ms.");

            return false;
        }

        stopwatch.Stop();

        Log.Success("OrderProcessor",
            $"Order #{order.Id} -> Step 1/6: Customer validated in {stopwatch.ElapsedMilliseconds} ms.");

        // Step 2: Reserve Stock
        stopwatch.Restart();

        Log.Info("OrderProcessor",
            $"Order #{order.Id} -> Step 2/6: Reserving stock...");

        stockReserved = await _inventoryService.ReserveStockAsync(order, ct);

        if (!stockReserved)
        {
            string productName = "Unknown Product";

            foreach (OrderItem item in order.Items)
            {
                Product? product = await _productRepository.GetByIdAsync(
                    item.ProductId, ct);

                if (product is null || product.StockQuantity < item.Quantity)
                {
                    productName = product?.Name ?? $"Product #{item.ProductId}";
                    break;
                }
            }

            order.Status = OrderStatus.Rejected;
            order.FailureReason = $"Insufficient stock for '{productName}'";

            await _orderRepository.SaveAsync(order, ct);

            stopwatch.Stop();

            Log.Error("OrderProcessor",
                $"Order #{order.Id} -> Step 2/6: Insufficient stock for '{productName}'. Rejected in {stopwatch.ElapsedMilliseconds} ms.");

            return false;
        }

        stopwatch.Stop();

        Log.Success("OrderProcessor",
            $"Order #{order.Id} -> Step 2/6: Stock reserved in {stopwatch.ElapsedMilliseconds} ms.");

        // Step 3: Calculate Total
        stopwatch.Restart();

        Log.Info("OrderProcessor",
            $"Order #{order.Id} -> Step 3/6: Calculating total...");

        try
        {
            await _pricingService.CalculateTotalAsync(order, ct);
        }
        catch (InvalidCouponException e)
        {
            await _inventoryService.ReleaseStockAsync(order, ct);
            stockReserved = false;

            order.Status = OrderStatus.Rejected;
            order.FailureReason = e.Message;

            await _orderRepository.SaveAsync(order, ct);

            stopwatch.Stop();

            Log.Error("OrderProcessor",
                $"Order #{order.Id} -> Step 3/6: Invalid coupon. Stock released and order rejected in {stopwatch.ElapsedMilliseconds} ms.");

            return false;
        }

        stopwatch.Stop();

        Log.Success("OrderProcessor",
            $"Order #{order.Id} -> Step 3/6: Total calculated: {order.TotalAmount:F2} in {stopwatch.ElapsedMilliseconds} ms.");

        // Step 4: Process Payment
        stopwatch.Restart();

        Log.Info("OrderProcessor",
            $"Order #{order.Id} -> Step 4/6: Processing payment...");

        PaymentResult paymentResult;

        using (CancellationTokenSource paymentCts =
               CancellationTokenSource.CreateLinkedTokenSource(ct))
        {
            Task<PaymentResult> paymentTask =
                _paymentGateway.ProcessPaymentAsync(
                    order.Id,
                    order.TotalAmount,
                    paymentCts.Token);

            Task timeoutTask = Task.Delay(
                TimeSpan.FromSeconds(3), ct);

            Task completedTask = await Task.WhenAny(
                paymentTask, timeoutTask);

            if (ct.IsCancellationRequested)
            {
                paymentCts.Cancel();

                try
                {
                    await paymentTask;
                }
                catch (OperationCanceledException)
                {
                    // Payment task cancelled.
                }

                throw new OperationCanceledException(ct);
            }

            if (completedTask == timeoutTask)
            {
                paymentCts.Cancel();

                try
                {
                    await paymentTask;
                }
                catch (OperationCanceledException)
                {
                    // Payment task cancelled because of timeout.
                }

                long paymentElapsed = stopwatch.ElapsedMilliseconds;

                await _inventoryService.ReleaseStockAsync(order, ct);
                stockReserved = false;

                order.Status = OrderStatus.PaymentFailed;
                order.FailureReason = "Payment gateway timeout (3s)";

                await _orderRepository.SaveAsync(order, ct);

                Log.Error("OrderProcessor",
                    $"Order #{order.Id} -> Step 4/6: Payment TIMEOUT after {paymentElapsed} ms. Stock released and order saved.");

                return false;
            }

            paymentResult = await paymentTask;
        }

        if (!paymentResult.Success)
        {
            long paymentElapsed = stopwatch.ElapsedMilliseconds;

            await _inventoryService.ReleaseStockAsync(order, ct);
            stockReserved = false;

            order.Status = OrderStatus.PaymentFailed;
            order.FailureReason = "Card declined by issuer";

            await _orderRepository.SaveAsync(order, ct);

            Log.Error("OrderProcessor",
                $"Order #{order.Id} -> Step 4/6: Payment DECLINED after {paymentElapsed} ms. Stock released and order saved.");

            return false;
        }

        transactionId = paymentResult.TransactionId;
        order.TransactionId = transactionId;

        stopwatch.Stop();

        Log.Success("OrderProcessor",
            $"Order #{order.Id} -> Step 4/6: Payment captured ({transactionId}) in {stopwatch.ElapsedMilliseconds} ms.");

        // Step 5: Save Completed Order
        stopwatch.Restart();

        Log.Info("OrderProcessor",
            $"Order #{order.Id} -> Step 5/6: Saving completed order...");

        order.Status = OrderStatus.Completed;
        order.FailureReason = null;

        try
        {
            await _orderRepository.SaveAsync(order, ct);
        }
        catch (DatabaseWriteException)
        {
            Log.Error("OrderProcessor",
                $"Order #{order.Id} -> Step 5/6: Database save failed. Starting compensation...");

            await _paymentGateway.RefundAsync(transactionId!, ct);

            await _inventoryService.ReleaseStockAsync(order, ct);
            stockReserved = false;

            order.Status = OrderStatus.Failed;
            order.FailureReason =
                $"Database save failed after payment; payment {transactionId} refunded";

            await _orderRepository.SaveAsync(order, ct);

            stopwatch.Stop();

            Log.Error("OrderProcessor",
                $"Order #{order.Id} -> Step 5/6: Compensation completed. Payment refunded and stock released in {stopwatch.ElapsedMilliseconds} ms.");

            return false;
        }

        stopwatch.Stop();

        Log.Success("OrderProcessor",
            $"Order #{order.Id} -> Step 5/6: Order saved in {stopwatch.ElapsedMilliseconds} ms.");

        // Step 6: Send Confirmation Email
        stopwatch.Restart();

        Log.Info("OrderProcessor",
            $"Order #{order.Id} -> Step 6/6: Sending confirmation email...");

        await _emailService.SendConfirmationWithRetryAsync(
            order, customer, ct);

        stopwatch.Stop();

        Log.Success("OrderProcessor",
            $"Order #{order.Id} -> Step 6/6: Email processing finished in {stopwatch.ElapsedMilliseconds} ms.");

        return order.Status == OrderStatus.Completed;
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested)
    {
        Log.Error("OrderProcessor",
            $"Order #{order.Id} -> Processing cancelled.");

        if (transactionId is not null)
        {
            await _paymentGateway.RefundAsync(
                transactionId, CancellationToken.None);
        }

        if (stockReserved)
        {
            await _inventoryService.ReleaseStockAsync(
                order, CancellationToken.None);
        }

        order.Status = OrderStatus.Pending;
        order.FailureReason = "Cancelled";

        await _orderRepository.SaveAsync(
            order, CancellationToken.None);

        return false;
    }
}

public async Task ProcessOrderFireAndForgetAsync(
    Order order,
    Customer customer,
    CancellationToken ct)
{
    await _orderRepository.SaveAsync(order, ct);

    await _emailService.SendConfirmationAsync(order, customer, ct);

    Console.WriteLine("Order processing finished");
}


}