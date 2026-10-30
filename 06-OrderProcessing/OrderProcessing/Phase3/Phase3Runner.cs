using System.Diagnostics;
using System.Globalization;
using OrderProcessing.Phase1;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase3;

public class Phase3Runner(
    IncomingOrderRepository incomingOrderRepository,
    OrderProcessor orderProcessor,
    OrderRepository orderRepository)
{
    private readonly IncomingOrderRepository _incomingOrderRepository =
        incomingOrderRepository;

    private readonly OrderProcessor _orderProcessor = orderProcessor;

    private readonly OrderRepository _orderRepository = orderRepository;

    public async Task RunAsync(CancellationToken ct)
    {
        // Step 1: Reset Data
        Console.WriteLine();
        Console.WriteLine("===== Phase 3: Order Processing System =====");

        await DataSeeder.ResetAsync(ct);

        // Step 2: Load Incoming Orders
        List<Order> orders =
            await _incomingOrderRepository.GetAllAsync(ct);

        Console.WriteLine();
        Console.WriteLine("===== Incoming Orders =====");
        Console.WriteLine($"Orders loaded: {orders.Count}");

        // Step 3: Start Processing All Orders Concurrently
        Console.WriteLine();
        Console.WriteLine("===== Concurrent Order Processing =====");

        Stopwatch processingStopwatch = Stopwatch.StartNew();

        List<Task<bool>> processingTasks = new List<Task<bool>>();

        foreach (Order order in orders)
        {
            Task<bool> processingTask =
                _orderProcessor.ProcessOrderAsync(order, ct);

            processingTasks.Add(processingTask);
        }

        Console.WriteLine($"Processing tasks started: {processingTasks.Count}");

        // Step 4: Wait for All Processing Tasks
        int completedCount = 0;

        foreach (Task<bool> task in processingTasks)
        {
            bool succeeded = await task;

            if (succeeded)
            {
                completedCount++;
            }
        }

        processingStopwatch.Stop();

        // Step 5: Load Saved Orders
        List<Order> savedOrders =
            await _orderRepository.GetAllAsync(ct);

        // Step 6: Print Final Processing Summary
        Console.WriteLine();
        Console.WriteLine("===== Final Processing Summary =====");

        Console.WriteLine(
            $"Processing completed in {processingStopwatch.Elapsed.TotalSeconds.ToString("F2", CultureInfo.InvariantCulture)} seconds");

        Console.WriteLine($"Processing tasks: {processingTasks.Count}");
        Console.WriteLine($"Successful tasks: {completedCount}");
        Console.WriteLine($"Unsuccessful tasks: {processingTasks.Count - completedCount}");

        Console.WriteLine();
        Console.WriteLine("===== Final Orders =====");

        Console.WriteLine(
            $"{"Order",-10} {"Status",-18} {"Total",-12} {"Failure Reason"}");

        Console.WriteLine(new string('-', 90));

        foreach (Order order in savedOrders.OrderBy(order => order.Id))
        {
            Console.WriteLine(
                $"{"#" + order.Id,-10} " +
                $"{order.Status,-18} " +
                $"{order.TotalAmount.ToString("F2", CultureInfo.InvariantCulture),-12} " +
                $"{order.FailureReason ?? "-"}");
        }

        Console.WriteLine(new string('-', 90));

        // Step 7: Verify Saved Orders
        Console.WriteLine();
        Console.WriteLine("===== Data Verification =====");

        Console.WriteLine($"Saved orders count: {savedOrders.Count}");
        Console.WriteLine($"Exactly 20 orders saved: {savedOrders.Count == 20}");

        int completedOrders = 0;
        int rejectedOrders = 0;
        int failedOrders = 0;
        int paymentFailedOrders = 0;

        foreach (Order order in savedOrders)
        {
            switch (order.Status)
            {
                case OrderStatus.Completed:
                    completedOrders++;
                    break;

                case OrderStatus.Rejected:
                    rejectedOrders++;
                    break;

                case OrderStatus.Failed:
                    failedOrders++;
                    break;

                case OrderStatus.PaymentFailed:
                    paymentFailedOrders++;
                    break;
            }
        }

        Console.WriteLine();
        Console.WriteLine("===== Orders Statistics =====");

        Console.WriteLine($"Completed:      {completedOrders}");
        Console.WriteLine($"Rejected:       {rejectedOrders}");
        Console.WriteLine($"Failed:         {failedOrders}");
        Console.WriteLine($"Payment Failed: {paymentFailedOrders}");
        Console.WriteLine($"Total:          {savedOrders.Count}");

        Console.WriteLine();
        Console.WriteLine("===== Phase 3 Completed =====");
    }
}