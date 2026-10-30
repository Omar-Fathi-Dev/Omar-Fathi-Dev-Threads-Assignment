
using System.Diagnostics;
using OrderProcessing.Phase1;
using OrderProcessing.Phase3;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase4;

public class LiveCheckoutRunner(OrderProcessor orderProcessor, OrderRepository orderRepository)
{
    private readonly OrderProcessor _orderProcessor = orderProcessor;
    private readonly OrderRepository _orderRepository = orderRepository;

    public async Task RunAsync(List<Order> orders, CancellationToken ct)
    {
        List<Task<(Order Order, TimeSpan Elapsed)>> processingTasks = new();

        foreach (Order order in orders)
        {
            processingTasks.Add(
                ProcessOrderWithTimingAsync(order, ct));
        }

        int completedCount = 0;
        int totalOrders = orders.Count;

        await foreach (Task<(Order Order, TimeSpan Elapsed)> completedTask in Task.WhenEach(processingTasks))
        {
            (Order Order, TimeSpan Elapsed) result = await completedTask;

            completedCount++;
            string reason = string.IsNullOrWhiteSpace(result.Order.FailureReason) ? string.Empty : $" ({result.Order.FailureReason})";

            Console.WriteLine(
                $"[{completedCount,2}/{totalOrders}] " +
                $"Order #{result.Order.Id} -> {result.Order.Status}" +
                $"{reason} after {result.Elapsed.TotalSeconds:F1} s");
        }

        await PrintSummaryAsync(ct);
    }

    private async Task<(Order Order, TimeSpan Elapsed)> ProcessOrderWithTimingAsync(Order order, CancellationToken ct)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        await _orderProcessor.ProcessOrderAsync(order, ct);

        stopwatch.Stop();

        return (order, stopwatch.Elapsed);
    }

    private async Task PrintSummaryAsync(CancellationToken ct)
    {
        List<Order> processedOrders = await _orderRepository.GetAllAsync(ct);

        Console.WriteLine("\n========== CHECKOUT SUMMARY ==========");

        foreach (OrderStatus status in Enum.GetValues<OrderStatus>())
        {
            int count = 0;
            foreach (Order order in processedOrders)
            {
                if (order.Status == status)
                {
                    count++;
                }
            }

            Console.WriteLine($"{status}: {count}");
        }

        Console.WriteLine($"Total: {processedOrders.Count}");
        Console.WriteLine("======================================");
    }
}
