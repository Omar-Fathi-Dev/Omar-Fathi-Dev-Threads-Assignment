
using OrderProcessing.Phase1;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase3;

public static class Phase3Composition
{
    public static async Task RunAsync(
        CancellationToken ct,
        bool enableFailures = true)
    {
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

        // Async Order Processor
        OrderProcessor orderProcessor =
            new OrderProcessor(
                customerService,
                inventoryService,
                pricingService,
                paymentGateway,
                orderRepository,
                emailService,
                productRepository);

        // Sync Services
        SyncCustomerService syncCustomerService =
            new SyncCustomerService(customerRepository);

        SyncInventoryService syncInventoryService =
            new SyncInventoryService(productRepository);

        SyncPricingService syncPricingService =
            new SyncPricingService(phase2PricingService);

        // Sync Order Processor
        SyncOrderProcessor syncOrderProcessor =
            new SyncOrderProcessor(
                syncCustomerService,
                syncInventoryService,
                syncPricingService,
                paymentGateway,
                orderRepository,
                emailService,
                productRepository);

        // Phase 3: Main Order Processing Demo
        Phase3Runner phase3Runner = new Phase3Runner(
            incomingOrderRepository,
            orderProcessor,
            orderRepository);

        await phase3Runner.RunAsync(ct);

// Fire-and-Forget Demo
        await DataSeeder.ResetAsync(ct);

        List<Order> incomingOrders =
            await incomingOrderRepository.GetAllAsync(ct);

        Order? testOrder = incomingOrders.FirstOrDefault(
            order => order.Id == 1);

        if (testOrder == null)
        {
            throw new InvalidOperationException("Test order not found.");
        }

        Customer? testCustomer =
            await customerRepository.GetByIdAsync(
                testOrder.CustomerId,
                ct);

        if (testCustomer == null)
        {
            throw new InvalidOperationException("Customer not found.");
        }

        Console.WriteLine();
        Console.WriteLine("========== FIRE-AND-FORGET DEMO ==========");

        await orderProcessor.ProcessOrderFireAndForgetAsync(
            testOrder,
            testCustomer,
            ct);

// Sync vs Async Comparison
        Phase3ComparisonRunner comparisonRunner =
            new Phase3ComparisonRunner(
                syncOrderProcessor,
                orderProcessor,
                orderRepository,
                incomingOrderRepository);

        await comparisonRunner.RunAsync(ct);
    }
}
