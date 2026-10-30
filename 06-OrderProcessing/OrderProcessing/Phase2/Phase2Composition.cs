using OrderProcessing.Phase1;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase2;

public static class Phase2Composition
{
    public static async Task RunAsync(CancellationToken ct)
    {
        JsonFileStore store = new JsonFileStore();

        IncomingOrderRepository incomingOrderRepository =
            new IncomingOrderRepository(store);

        CustomerRepository customerRepository =
            new CustomerRepository(store);

        ProductRepository productRepository =
            new ProductRepository(store);

        CouponRepository couponRepository =
            new CouponRepository(store);

        OrderRepository orderRepository =
            new OrderRepository(store);

        PricingService pricingService = new PricingService(
            customerRepository,
            productRepository,
            couponRepository,
            new[] { 7 });

        OrderPricer orderPricer = new OrderPricer(pricingService);

        OrderValidator orderValidator = new OrderValidator(
            customerRepository,
            productRepository,
            couponRepository,
            orderRepository);

        InvoiceGenerator invoiceGenerator = new InvoiceGenerator();

        OrderProcessorV2 orderProcessor = new OrderProcessorV2(
            orderRepository,
            productRepository,
            orderPricer,
            invoiceGenerator);
        
        ReportGenerator reportGenerator = new ReportGenerator(
            orderRepository,
            productRepository);

        Phase2Runner runner = new Phase2Runner(
            incomingOrderRepository,
            orderPricer,
            orderValidator,
            orderProcessor,
            orderRepository,
            productRepository,
            reportGenerator);

        await runner.RunAsync(ct);
    }
}