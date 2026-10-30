
using System.Diagnostics;
using System.Threading;
using OrderProcessing.Phase3;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase4;

public class EmailResendRunner
{
    private readonly IEmailService _emailService;

    public EmailResendRunner(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        List<(Order Order, Customer Customer)> testEmails =
            new List<(Order, Customer)>();

        for (int id = 1001; id <= 1030; id++)
        {
            Order order = new Order
            {
                Id = id,
                CustomerId = id,
                Status = OrderStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                TotalAmount = 100m
            };

            Customer customer = new Customer
            {
                Id = id,
                Name = $"Test Customer {id}",
                Email = $"customer{id}@example.com",
                IsActive = true
            };

            testEmails.Add((order, customer));
        }

        int currentlySending = 0;
        int highestConcurrentSends = 0;

        ParallelOptions options = new ParallelOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = 4
        };

        Stopwatch stopwatch = Stopwatch.StartNew();

        await Parallel.ForEachAsync(
            testEmails,
            options,
            async (email, cancellationToken) =>
            {
                int running = Interlocked.Increment(
                    ref currentlySending);

                int previousHighest;

                do
                {
                    previousHighest = highestConcurrentSends;

                    if (running <= previousHighest)
                    {
                        break;
                    }
                }
                while (Interlocked.CompareExchange(
                    ref highestConcurrentSends,
                    running,
                    previousHighest) != previousHighest);

                try
                {
                    await _emailService.SendConfirmationAsync(
                        email.Order,
                        email.Customer,
                        cancellationToken);
                }
                finally
                {
                    Interlocked.Decrement(ref currentlySending);
                }
            });

        stopwatch.Stop();

        Console.WriteLine();
        Console.WriteLine("========== EMAIL RE-SEND ==========");
        Console.WriteLine($"Emails sent: {testEmails.Count}");
        Console.WriteLine(
            $"Highest concurrent sends: {highestConcurrentSends}");
        Console.WriteLine(
            $"Total time: {stopwatch.ElapsedMilliseconds} ms");

        if (highestConcurrentSends != 4)
        {
            throw new InvalidOperationException(
                $"Expected 4 concurrent sends, but got {highestConcurrentSends}.");
        }
    }
}
