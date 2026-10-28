using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Storage;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using OrderProcessing.Phase1;
namespace OrderProcessing;

class Program
{
    private static async Task Main()
    {
        CancellationTokenSource cancellationSource = new CancellationTokenSource();
        CancellationToken ct = cancellationSource.Token;

        await DataSeeder.ResetAsync(ct);

        JsonFileStore store = new JsonFileStore();

        CustomerRepository customerRepository = new CustomerRepository(store);
        ProductRepository productRepository = new ProductRepository(store);
        CouponRepository couponRepository = new CouponRepository(store);
        OrderRepository orderRepository = new OrderRepository(store);

        OrderValidator validator = new OrderValidator(
            customerRepository,
            productRepository,
            couponRepository,
            orderRepository);

        Order testOrder = new Order
        {
            Id = 999,
            CustomerId = -1,
            Status = OrderStatus.Pending
        };

        await validator.ValidateOrderAsync(testOrder, ct);

        Console.WriteLine($"Status: {testOrder.Status}");
        Console.WriteLine($"FailureReason: {testOrder.FailureReason}");
    }
}