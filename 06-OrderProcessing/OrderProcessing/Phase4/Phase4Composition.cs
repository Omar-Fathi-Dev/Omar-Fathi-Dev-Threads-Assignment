
using OrderProcessing.Phase1;
using OrderProcessing.Phase3;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase4;

public static class Phase4Composition
{
    public static async Task RunAsync(
        CancellationToken ct,
        bool enableFailures = true)
    {
        await DataSeeder.ResetAsync(ct);

        JsonFileStore store = new JsonFileStore();

        // Repositories
        IncomingOrderRepository incomingOrderRepository =
            new IncomingOrderRepository(store);

        CustomerRepository customerRepository =
            new CustomerRepository(store);

        ProductRepository productRepository =
            new ProductRepository(store);

        CouponRepository couponRepository =
            new CouponRepository(store);

        OrderRepository orderRepository =
            new OrderRepository(
                store,
                new[] { 13 },
                enableFailures);

        // Phase 2 Pricing Service
        OrderProcessing.Phase2.PricingService phase2PricingService =
            new OrderProcessing.Phase2.PricingService(
                customerRepository,
                productRepository,
                couponRepository,
                Array.Empty<int>());

        // Async Services
        CustomerService customerService =
            new CustomerService(customerRepository);

        InventoryService inventoryService =
            new InventoryService(productRepository);

        PricingService pricingService =
            new PricingService(phase2PricingService);

        PaymentGateway paymentGateway =
            new PaymentGateway(
                store,
                new[] { 5 },
                new[] { 8 },
                enableFailures);

        EmailService emailService =
            new EmailService(
                store,
                new Dictionary<int, int>
                {
                    { 3, 2 },
                    { 17, int.MaxValue }
                },
                enableFailures);

        // Order Processor
        OrderProcessor orderProcessor =
            new OrderProcessor(
                customerService,
                inventoryService,
                pricingService,
                paymentGateway,
                orderRepository,
                emailService,
                productRepository);

        // Prepare all 20 orders
        List<Order> incomingOrders =
            await incomingOrderRepository.GetAllAsync(ct);

        // Live Checkout
        LiveCheckoutRunner liveCheckoutRunner =
            new LiveCheckoutRunner(
                orderProcessor,
                orderRepository);

        await liveCheckoutRunner.RunAsync(
            incomingOrders,
            ct);
        
        // Fraud Scoring
        Console.WriteLine();
        Console.WriteLine("========== FRAUD SCORING ==========");

        FraudScoringRunner fraudScoringRunner =
            new FraudScoringRunner();

        fraudScoringRunner.Run(ct);

        // Email Re-send
        Console.WriteLine();
        Console.WriteLine("========== EMAIL RE-SEND ==========");

        EmailService resendEmailService =
            new EmailService(
                store,
                enableFailures: false);

        EmailResendRunner emailResendRunner =
            new EmailResendRunner(resendEmailService);

        await emailResendRunner.RunAsync(ct);

        // Fastest Shipping Quote
        Console.WriteLine();
        Console.WriteLine("========== FASTEST SHIPPING QUOTES ==========");

        // Load products once for all carriers.
        List<Product> shippingProducts =
            await productRepository.GetAllAsync(ct);

        List<Order> processedOrders =
            await orderRepository.GetAllAsync(ct);

        // Configure the three carriers.
        // Order #6 will fail in all three carriers.
        FastShipCarrier fastShip =
            new FastShipCarrier(shippingProducts, new[] { 6 });

        CairoExpressCarrier cairoExpress =
            new CairoExpressCarrier(shippingProducts, new[] { 6 });

        NileCourierCarrier nileCourier =
            new NileCourierCarrier(shippingProducts, new[] { 6 });

        ShippingQuoteService shippingQuoteService =
            new ShippingQuoteService(
                new IShippingCarrier[]
                {
                    fastShip,
                    cairoExpress,
                    nileCourier
                });

        // Run for Orders #1 to #5.
        for (int orderId = 1; orderId <= 5; orderId++)
        {
            Order? order = null;

            foreach (Order processedOrder in processedOrders)
            {
                if (processedOrder.Id == orderId)
                {
                    order = processedOrder;
                    break;
                }
            }

            if (order is null)
            {
                Console.WriteLine(
                    $"Order #{orderId} was not found.");

                continue;
            }

            ShippingQuote quote =
                await shippingQuoteService.GetFastestQuoteAsync(
                    order,
                    ct);

            string cancelledCarriers =
                quote.CancelledCarriers.Count == 0
                    ? "None"
                    : string.Join(", ", quote.CancelledCarriers);

            Console.WriteLine(
                $"Order #{order.Id} -> {quote.Carrier} | " +
                $"Price: {quote.Price:F2} | " +
                $"Time: {quote.ElapsedMs} ms | " +
                $"Cancelled: {cancelledCarriers}");
        }

        // Run Order #6 - all carriers fail.
        Console.WriteLine();
        Console.WriteLine(
            "Testing all carriers failure for Order #6...");

        Order? orderSix = null;

        foreach (Order processedOrder in processedOrders)
        {
            if (processedOrder.Id == 6)
            {
                orderSix = processedOrder;
                break;
            }
        }

        if (orderSix is not null)
        {
            try
            {
                await shippingQuoteService.GetFastestQuoteAsync(
                    orderSix,
                    ct);
            }
            catch (ShippingProviderUnavailableException exception)
            {
                Console.WriteLine(
                    $"Order #6 -> {exception.Message}");
            }
        }

        // Test R7 - Caller Cancellation
        Console.WriteLine();
        Console.WriteLine("Testing caller cancellation...");

        Order? cancellationTestOrder = null;

        foreach (Order processedOrder in processedOrders)
        {
            if (processedOrder.Id == 1)
            {
                cancellationTestOrder = processedOrder;
                break;
            }
        }

        if (cancellationTestOrder is not null)
        {
            using CancellationTokenSource cancellationCts =
                new CancellationTokenSource();

            try
            {
                Task<ShippingQuote> quoteTask =
                    shippingQuoteService.GetFastestQuoteAsync(
                        cancellationTestOrder,
                        cancellationCts.Token);

                await Task.Delay(300);

                cancellationCts.Cancel();

                await quoteTask;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine(
                    "Caller cancelled the shipping quote successfully.");
            }
        }

        // Customer Dashboard
        Console.WriteLine();
        Console.WriteLine("========== CUSTOMER DASHBOARD ==========");

        CustomerDashboardService dashboardService =
            new CustomerDashboardService(
                store,
                customerRepository,
                orderRepository,
                new[] { 99 });

        // Customer #3 - Successful dashboard load
        try
        {
            CustomerDashboard dashboard =
                await dashboardService.LoadAsync(3, ct);

            Console.WriteLine();
            Console.WriteLine(
                $"Customer #{dashboard.Customer.Id}: " +
                $"{dashboard.Customer.Name}");

            Console.WriteLine(
                $"Orders: {dashboard.Orders.Count}");

            foreach (Order order in dashboard.Orders)
            {
                Console.WriteLine(
                    $"  Order #{order.Id} - {order.Status}");
            }

            Console.WriteLine(
                $"Payments: {dashboard.Payments.Count}");

            foreach (PaymentTransaction payment in dashboard.Payments)
            {
                Console.WriteLine(
                    $"  Order #{payment.OrderId} - {payment.Status}");
            }

            Console.WriteLine(
                $"Emails: {dashboard.Emails.Count}");

            foreach (EmailMessage email in dashboard.Emails)
            {
                Console.WriteLine(
                    $"  Order #{email.OrderId} - {email.Subject}");
            }
        }
        catch (Exception)
        {
            // LoadAsync already prints the individual load failures.
        }

        // Customer #99 - Multiple failures
        Console.WriteLine();
        Console.WriteLine("Loading Customer #99...");

        try
        {
            await dashboardService.LoadAsync(99, ct);
        }
        catch (Exception)
        {
            // Expected: customer not found and payments timeout.
        }
    }
}
