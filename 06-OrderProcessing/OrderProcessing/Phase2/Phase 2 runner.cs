
using System.Diagnostics;
using System.Globalization;
using OrderProcessing.Phase1;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase2;

public class Phase2Runner(
    IncomingOrderRepository incomingOrderRepository,
    OrderPricer orderPricer,
    OrderValidator orderValidator,
    OrderProcessorV2 orderProcessor,
    OrderRepository orderRepository,
    ProductRepository productRepository,
    ReportGenerator reportGenerator)
{
    private readonly IncomingOrderRepository _incomingOrderRepository = incomingOrderRepository;
    private readonly OrderPricer _orderPricer = orderPricer;
    private readonly OrderValidator _orderValidator = orderValidator;
    private readonly OrderProcessorV2 _orderProcessor = orderProcessor;
    private readonly OrderRepository _orderRepository = orderRepository;
    private readonly ProductRepository _productRepository = productRepository;
    private ReportGenerator ReportGenerator { get; } = reportGenerator;
    public async Task RunAsync(CancellationToken ct)
    {
        await DataSeeder.ResetAsync(ct);
        await RaceConditionDemo.RunAsync(ct);
        await DataSeeder.ResetAsync(ct);
        

        List<Order> sequentialOrders = await _incomingOrderRepository.GetAllAsync(ct);
        Order? sequentialOrder = null;
        foreach (Order order in sequentialOrders)
        {
            if (order.Id == 1)
            {
                sequentialOrder = order;
                break;
            }
        }

        if (sequentialOrder is null)
            throw new InvalidOperationException("Order #1 not found.");

        Stopwatch sequentialStopwatch = Stopwatch.StartNew();

        await _orderPricer.PriceOrderSequentialAsync(
            sequentialOrder, ct);

        sequentialStopwatch.Stop();

        List<Order> parallelOrders =
            await _incomingOrderRepository.GetAllAsync(ct);

        Order? parallelOrder = null;

        foreach (Order order in parallelOrders)
        {
            if (order.Id == 1)
            {
                parallelOrder = order;
                break;
            }
        }

        if (parallelOrder is null)
            throw new InvalidOperationException("Order #1 not found.");

        Stopwatch parallelStopwatch = Stopwatch.StartNew();

        await _orderPricer.PriceOrderAsync(parallelOrder, ct);

        parallelStopwatch.Stop();

        Console.WriteLine();
        Console.WriteLine("===== Pricing Performance =====");

        Console.WriteLine(
            $"Sequential: {sequentialStopwatch.Elapsed.TotalSeconds.ToString("F2", CultureInfo.InvariantCulture)} seconds");

        Console.WriteLine(
            $"Parallel:   {parallelStopwatch.Elapsed.TotalSeconds.ToString("F2", CultureInfo.InvariantCulture)} seconds");

        Console.WriteLine();
        Console.WriteLine("===== Order #1 Pricing =====");

        Console.WriteLine($"SubTotal:     {parallelOrder.SubTotal:F2}");
        Console.WriteLine($"Discount:     {parallelOrder.Discount:F2}");
        Console.WriteLine($"ShippingCost: {parallelOrder.ShippingCost:F2}");
        Console.WriteLine($"TotalAmount:  {parallelOrder.TotalAmount:F2}");
        
        Console.WriteLine();
        Console.WriteLine("===== Concurrent Order Validation =====");

        List<Order> orders = await _incomingOrderRepository.GetAllAsync(ct);

        Stopwatch validationStopwatch = Stopwatch.StartNew();

        List<Task> validationTasks = new List<Task>();

        foreach (Order order in orders)
        {
            Task validationTask = _orderValidator.ValidateOrderAsync(order, ct);
            validationTasks.Add(validationTask);
        }

        Console.WriteLine($"Validation tasks started: {validationTasks.Count}");

        await Task.WhenAll(validationTasks);

        validationStopwatch.Stop();

        Console.WriteLine(
            $"Validation completed in {validationStopwatch.Elapsed.TotalSeconds.ToString("F2", CultureInfo.InvariantCulture)} seconds");

        int validatedCount = 0;
        int rejectedCount = 0;

        foreach (Order order in orders)
        {
            if (order.Status == OrderStatus.Validated)
                validatedCount++;
            else if (order.Status == OrderStatus.Rejected)
                rejectedCount++;
        }

        Console.WriteLine($"Validated: {validatedCount}");
        Console.WriteLine($"Rejected:  {rejectedCount}");
        Console.WriteLine($"Total:     {orders.Count}");
        
        Console.WriteLine();
        
        Console.WriteLine("===== Concurrent Order Processing =====");

        Stopwatch processingStopwatch = Stopwatch.StartNew();

        List<Order> validatedOrders = new List<Order>();

        foreach (Order order in orders)
        {
            if (order.Status == OrderStatus.Validated)
            {
                validatedOrders.Add(order);
            }
        }

        List<Task<bool>> processingTasks = new List<Task<bool>>();

        foreach (Order order in validatedOrders)
        {
            Task<bool> processingTask =
                _orderProcessor.ProcessValidatedOrderAsync(order, ct);

            processingTasks.Add(processingTask);
        }

        Console.WriteLine($"Processing tasks started: {processingTasks.Count}");

        if (processingTasks.Count > 0)
        {
            Console.WriteLine($"First task status: {processingTasks[0].Status}");
        }

        await Task.Delay(1000, ct);

        Console.WriteLine();
        Console.WriteLine("===== Task Status After 1 Second =====");

        foreach (Task<bool> task in processingTasks)
        {
            Console.WriteLine(task.Status);
        }

        Console.WriteLine();
        Console.WriteLine("===== Processing Results =====");

        for (int i = 0; i < processingTasks.Count; i++)
        {
            Task<bool> task = processingTasks[i];
            Order order = validatedOrders[i];

            try
            {
                bool succeeded = await task;
                if (!succeeded)
                {
                    Console.WriteLine($"Order #{order.Id} was rejected during processing.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Order #{order.Id} failed: {ex.Message}");
                order.Status = OrderStatus.Failed;
                order.FailureReason = ex.Message;

                foreach (OrderItem item in order.Items)
                {
                    await _productRepository.ReleaseStockAsync(item.ProductId, item.Quantity, ct);
                }
                await _orderRepository.SaveAsync(order, ct);
            }
            Console.WriteLine($"Order #{order.Id}: Task Status = {task.Status}");
        }

        processingStopwatch.Stop();

        Console.WriteLine();
        Console.WriteLine("===== Final Processing Summary =====");

        foreach (Task<bool> task in processingTasks)
        {
            Console.WriteLine($"Task Status: {task.Status}");
        }   
        Console.WriteLine($"Processing completed in {processingStopwatch.Elapsed.TotalSeconds.ToString("F2", CultureInfo.InvariantCulture)} seconds");
        List<Order> savedOrders = await _orderRepository.GetAllAsync(ct);
        Console.WriteLine($"Saved orders count: {savedOrders.Count}");
        Console.WriteLine($"Exactly 20 orders saved: {savedOrders.Count == 20}");
        
        Console.WriteLine();
        Console.WriteLine("===== Report Generation =====");

        Task<Report> reportTask = ReportGenerator.GenerateAsync(ct);

        Console.WriteLine("Generating report...");

        Report report = await reportTask;

        Console.WriteLine($"Generated At:       {report.GeneratedAt}");
        Console.WriteLine($"Total Orders:       {report.TotalOrders}");
        Console.WriteLine($"Completed:          {report.CompletedCount}");
        Console.WriteLine($"Rejected:            {report.RejectedCount}");
        Console.WriteLine($"Failed:              {report.FailedCount}");
        Console.WriteLine($"Payment Failed:      {report.PaymentFailedCount}");
        Console.WriteLine($"Total Revenue:       {report.TotalRevenue:F2}");
        Console.WriteLine($"Average Order Value: {report.AverageOrderValue:F2}");
        Console.WriteLine($"Top Selling Product: {report.TopSellingProduct}");
        Console.WriteLine($"Top Selling Quantity:{report.TopSellingQuantity}");
        
    }
}
