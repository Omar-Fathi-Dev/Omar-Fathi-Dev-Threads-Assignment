
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

        // Prepare test data
        List<Order> incomingOrders =
            await incomingOrderRepository.GetAllAsync(ct);

        Order? testOrder = null;

        foreach (Order order in incomingOrders)
        {
            if (order.Id == 1)
            {
                testOrder = order;
                break;
            }
        }

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

        // Fire-and-forget test
        await orderProcessor.ProcessOrderFireAndForgetAsync(
            testOrder,
            testCustomer,
            ct);

        // Comparison Runner
        // Temporarily disabled during the fire-and-forget investigation.

        /*
        Phase3ComparisonRunner comparisonRunner =
            new Phase3ComparisonRunner(
                syncOrderProcessor,
                orderProcessor,
                orderRepository,
                incomingOrderRepository);

        await comparisonRunner.RunAsync(ct);
        */
    }
}
