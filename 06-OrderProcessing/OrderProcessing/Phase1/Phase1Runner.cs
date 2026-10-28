
using System.Diagnostics;
using System.Globalization;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1;

public class Phase1Runner(
    IncomingOrderRepository incomingOrderRepository,
    SyncOrderValidator syncOrderValidator,
    OrderValidator orderValidator,
    OrderRepository orderRepository)
{
    private readonly IncomingOrderRepository _incomingOrderRepository =
        incomingOrderRepository;

    private readonly SyncOrderValidator _syncOrderValidator =
        syncOrderValidator;

    private readonly OrderValidator _orderValidator = orderValidator;

    private readonly OrderRepository _orderRepository = orderRepository;

    public async Task RunAsync(CancellationToken ct)
    {
        // 1. Reset data and load the original orders.
        await DataSeeder.ResetAsync(ct);

        List<Order> syncOrders =
            await _incomingOrderRepository.GetAllAsync(ct);

        // 2. Run synchronous validation.
        Stopwatch syncStopwatch = new Stopwatch();
        syncStopwatch.Start();

        foreach (Order order in syncOrders)
        {
            ct.ThrowIfCancellationRequested();
            _syncOrderValidator.ValidateOrder(order);
        }

        syncStopwatch.Stop();

        // 3. Reset again so the async run starts with fresh data.
        await DataSeeder.ResetAsync(ct);

        List<Order> asyncOrders =
            await _incomingOrderRepository.GetAllAsync(ct);

        // 4. Run asynchronous validation sequentially.
        Stopwatch asyncStopwatch = new Stopwatch();
        asyncStopwatch.Start();

        foreach (Order order in asyncOrders)
        {
            ct.ThrowIfCancellationRequested();
            await _orderValidator.ValidateOrderAsync(order, ct);
        }

        asyncStopwatch.Stop();

        // 5. Read the actual saved results from orders.json.
        List<Order> validatedOrders =
            await _orderRepository.GetByStatusAsync(
                OrderStatus.Validated, ct);

        List<Order> rejectedOrders =
            await _orderRepository.GetByStatusAsync(
                OrderStatus.Rejected, ct);

        List<Order> savedOrders =
            await _orderRepository.GetAllAsync(ct);

        rejectedOrders.Sort(
            (first, second) => first.Id.CompareTo(second.Id));

        // 6. Print the summary.
        Console.WriteLine();
        Console.WriteLine("===== Validation Summary =====");
        Console.WriteLine($"Total Orders: {savedOrders.Count}");
        Console.WriteLine($"Validated:    {validatedOrders.Count}");
        Console.WriteLine($"Rejected:     {rejectedOrders.Count}");

        foreach (Order order in rejectedOrders)
        {
            Console.WriteLine(
                $"  - Order #{order.Id}:  {order.FailureReason}");
        }

        Console.WriteLine(
            $"Sync Time:  {syncStopwatch.Elapsed.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture)} seconds");

        Console.WriteLine(
            $"Async Time: {asyncStopwatch.Elapsed.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture)} seconds");

        Console.WriteLine(
            $"orders.json now contains {savedOrders.Count} records.");
    }
}
