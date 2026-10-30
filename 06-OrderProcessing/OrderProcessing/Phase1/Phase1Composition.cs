using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1;

public static class Phase1Composition
{
    public static async Task RunAsync(CancellationToken ct)
    {
        JsonFileStore store = new JsonFileStore();

        IncomingOrderRepository incomingOrderRepository = new IncomingOrderRepository(store);

        SyncCustomerRepository syncCustomerRepository = new SyncCustomerRepository(store);

        SyncProductRepository syncProductRepository = new SyncProductRepository(store);

        SyncCouponRepository syncCouponRepository = new SyncCouponRepository(store);

        SyncOrderValidator syncOrderValidator = new SyncOrderValidator(syncCustomerRepository, syncProductRepository, syncCouponRepository);

        CustomerRepository customerRepository = new CustomerRepository(store);

        ProductRepository productRepository = new ProductRepository(store);

        CouponRepository couponRepository = new CouponRepository(store);

        OrderRepository orderRepository = new OrderRepository(store);

        OrderValidator orderValidator = new OrderValidator(customerRepository, productRepository, couponRepository, orderRepository);

        Phase1Runner runner = new Phase1Runner(incomingOrderRepository, syncOrderValidator, orderValidator, orderRepository);

        await runner.RunAsync(ct);
    }
}