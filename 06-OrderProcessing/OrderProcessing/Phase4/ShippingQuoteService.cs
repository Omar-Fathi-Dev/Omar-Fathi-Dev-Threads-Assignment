
using System.Diagnostics;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase4;

public class ShippingQuoteService
{
    private readonly List<IShippingCarrier> _carriers;

    private static readonly TimeSpan QuoteTimeBudget =
        TimeSpan.FromMilliseconds(1200);

    public ShippingQuoteService(
        IEnumerable<IShippingCarrier> carriers)
    {
        _carriers = new List<IShippingCarrier>(carriers);

        if (_carriers.Count != 3)
        {
            throw new ArgumentException(
                "Exactly three shipping carriers are required.");
        }
    }

    public async Task<ShippingQuote> GetFastestQuoteAsync(
        Order order,
        CancellationToken ct)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        using CancellationTokenSource carrierCts =
            CancellationTokenSource.CreateLinkedTokenSource(ct);

        Dictionary<Task<decimal>, IShippingCarrier> carrierTasks =
            new Dictionary<Task<decimal>, IShippingCarrier>();

        List<Task<decimal>> allTasks = new List<Task<decimal>>();

        // Start all three carriers.
        foreach (IShippingCarrier carrier in _carriers)
        {
            Task<decimal> task =
                carrier.GetQuoteAsync(order, carrierCts.Token);

            carrierTasks.Add(task, carrier);
            allTasks.Add(task);
        }

        Task timeoutTask = Task.Delay(QuoteTimeBudget, ct);

        List<Task<decimal>> pendingTasks =
            new List<Task<decimal>>(allTasks);

        try
        {
            while (pendingTasks.Count > 0)
            {
                List<Task> waitTasks = new List<Task>();

                foreach (Task<decimal> task in pendingTasks)
                {
                    waitTasks.Add(task);
                }

                waitTasks.Add(timeoutTask);

                Task completedTask =
                    await Task.WhenAny(waitTasks);

                // Caller cancellation takes priority over timeout.
                ct.ThrowIfCancellationRequested();

                // No successful quote arrived within 1.2 seconds.
                if (completedTask == timeoutTask)
                {
                    long elapsedMs = stopwatch.ElapsedMilliseconds;

                    List<string> cancelledCarriers =
                        GetRunningCarrierNames(
                            pendingTasks,
                            carrierTasks);

                    carrierCts.Cancel();

                    await ObserveAllTasksAsync(allTasks);

                    Console.WriteLine(
                        $"Order #{order.Id}: Flat rate selected.");

                    return new ShippingQuote
                    {
                        Carrier = "Flat rate",
                        Price = 25.00m,
                        ElapsedMs = elapsedMs,
                        CancelledCarriers = cancelledCarriers
                    };
                }

                Task<decimal> completedQuoteTask =
                    (Task<decimal>)completedTask;

                pendingTasks.Remove(completedQuoteTask);

                try
                {
                    decimal price = await completedQuoteTask;

                    IShippingCarrier winningCarrier =
                        carrierTasks[completedQuoteTask];

                    long elapsedMs = stopwatch.ElapsedMilliseconds;

                    List<string> cancelledCarriers =
                        GetRunningCarrierNames(
                            pendingTasks,
                            carrierTasks);

                    // Cancel carriers that are still running.
                    carrierCts.Cancel();

                    await ObserveAllTasksAsync(allTasks);

                    return new ShippingQuote
                    {
                        Carrier = winningCarrier.Name,
                        Price = price,
                        ElapsedMs = elapsedMs,
                        CancelledCarriers = cancelledCarriers
                    };
                }
                catch (ShippingProviderUnavailableException exception)
                {
                    Console.WriteLine(
                        $"[{carrierTasks[completedQuoteTask].Name}] " +
                        $"failed: {exception.Message}");

                    // Keep waiting for the remaining carriers.
                }
            }

            throw new ShippingProviderUnavailableException(
                "All carriers failed");
        }
        catch
        {
            carrierCts.Cancel();

            await ObserveAllTasksAsync(allTasks);

            throw;
        }
    }

    private static List<string> GetRunningCarrierNames(
        List<Task<decimal>> pendingTasks,
        Dictionary<Task<decimal>, IShippingCarrier> carrierTasks)
    {
        List<string> names = new List<string>();

        foreach (Task<decimal> task in pendingTasks)
        {
            if (!task.IsCompleted)
            {
                names.Add(carrierTasks[task].Name);
            }
        }

        return names;
    }

    private static async Task ObserveAllTasksAsync(
        List<Task<decimal>> tasks)
    {
        try
        {
            await Task.WhenAll(tasks);
        }
        catch
        {
            // Observe expected failures and cancellations.
        }
    }
}
