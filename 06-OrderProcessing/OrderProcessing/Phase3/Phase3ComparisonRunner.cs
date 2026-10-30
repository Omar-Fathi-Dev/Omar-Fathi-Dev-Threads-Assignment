
using System.Collections.Concurrent;
using System.Diagnostics;
using OrderProcessing.Phase1;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase3;

public class Phase3ComparisonRunner(
    SyncOrderProcessor syncOrderProcessor,
    OrderProcessor orderProcessor,
    OrderRepository orderRepository,
    IncomingOrderRepository incomingOrderRepository)
{
    private readonly SyncOrderProcessor _syncOrderProcessor = syncOrderProcessor;
    private readonly OrderProcessor _orderProcessor = orderProcessor;
    private readonly OrderRepository _orderRepository = orderRepository;
    private readonly IncomingOrderRepository _incomingOrderRepository = incomingOrderRepository;

    public async Task RunAsync(CancellationToken ct)
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("       SYNC vs ASYNC COMPARISON");
        Console.WriteLine("========================================");

        await RunSyncVersionAsync(ct);
        await RunAsyncVersionAsync(ct);

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("       COMPARISON COMPLETED");
        Console.WriteLine("========================================");
    }

    private async Task RunSyncVersionAsync(CancellationToken ct)
    {
        Console.WriteLine();
        Console.WriteLine("========== SYNCHRONOUS VERSION ==========");

        await DataSeeder.ResetAsync(ct);

        List<Order> allOrders = await _incomingOrderRepository.GetAllAsync(ct);
        List<Order> orders = new List<Order>();

        foreach (Order order in allOrders)
        {
            if (order.Id >= 1 && order.Id <= 10)
            {
                orders.Add(order);
            }
        }

        ConcurrentDictionary<int, byte> threadIds = new();

        Thread[] threads = new Thread[orders.Count];

        Stopwatch stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < orders.Count; i++)
        {
            Order currentOrder = orders[i];

            threads[i] = new Thread(() =>
            {
                threadIds.TryAdd(
                    Thread.CurrentThread.ManagedThreadId, 0);

                _syncOrderProcessor.ProcessOrder(currentOrder);
            });

            threads[i].Start();
        }

        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        stopwatch.Stop();

        List<Order> processedOrders =
            await _orderRepository.GetAllAsync(ct);

        PrintResults(
            "SYNC",
            stopwatch.ElapsedMilliseconds,
            threadIds.Count,
            processedOrders);

        Console.WriteLine(
            $"ThreadPool.ThreadCount: {ThreadPool.ThreadCount}");
    }

    private async Task RunAsyncVersionAsync(CancellationToken ct)
    {
        Console.WriteLine();
        Console.WriteLine("========== ASYNCHRONOUS VERSION ==========");

        await DataSeeder.ResetAsync(ct);

        List<Order> allOrders = await _incomingOrderRepository.GetAllAsync(ct);
        List<Order> orders = new List<Order>();

        foreach (Order order in allOrders)
        {
            if (order.Id >= 1 && order.Id <= 10)
            {
                orders.Add(order);
            }
        }

        ConcurrentDictionary<int, byte> threadIds = new();

        Stopwatch stopwatch = Stopwatch.StartNew();

        List<Task<bool>> processingTasks = new();

        foreach (Order order in orders)
        {
            processingTasks.Add(
                ProcessAndTrackAsync(order, threadIds, ct));
        }

        foreach (Task<bool> task in processingTasks)
        {
            await task;
        }

        stopwatch.Stop();

        List<Order> processedOrders =
            await _orderRepository.GetAllAsync(ct);

        PrintResults(
            "ASYNC",
            stopwatch.ElapsedMilliseconds,
            threadIds.Count,
            processedOrders);

        Console.WriteLine(
            $"ThreadPool.ThreadCount: {ThreadPool.ThreadCount}");
    }

    private async Task<bool> ProcessAndTrackAsync(
        Order order,
        ConcurrentDictionary<int, byte> threadIds,
        CancellationToken ct)
    {
        threadIds.TryAdd(
            Thread.CurrentThread.ManagedThreadId, 0);

        bool result = await _orderProcessor.ProcessOrderAsync(order, ct);

        threadIds.TryAdd(
            Thread.CurrentThread.ManagedThreadId, 0);

        return result;
    }

    private static void PrintResults(
        string version,
        long elapsedMilliseconds,
        int threadCount,
        List<Order> orders)
    {
        int completed = orders.Count(
            order => order.Status == OrderStatus.Completed);

        int rejected = orders.Count(
            order => order.Status == OrderStatus.Rejected);

        int failed = orders.Count(
            order => order.Status == OrderStatus.Failed);

        int paymentFailed = orders.Count(
            order => order.Status == OrderStatus.PaymentFailed);

        Console.WriteLine();
        Console.WriteLine($"========== {version} RESULTS ==========");
        Console.WriteLine($"Execution Time: {elapsedMilliseconds} ms");
        Console.WriteLine($"Observed Threads: {threadCount}");
        Console.WriteLine($"Total Orders: {orders.Count}");
        Console.WriteLine($"Completed: {completed}");
        Console.WriteLine($"Rejected: {rejected}");
        Console.WriteLine($"Failed: {failed}");
        Console.WriteLine($"Payment Failed: {paymentFailed}");

        Console.WriteLine();
        Console.WriteLine("Order Details:");

        foreach (Order order in orders.OrderBy(order => order.Id))
        {
            Console.WriteLine(
                $"Order #{order.Id}: {order.Status} | {order.FailureReason}");
        }
    }
}
