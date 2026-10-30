
using System.Collections.Concurrent;
using System.Diagnostics;

namespace OrderProcessing.Phase4;

public class FraudScoringRunner
{
    private const int OrderCount = 10_000;
    private const int FlagThreshold = 90;

    private static int Score(int orderId)
    {
        long h = orderId;

        for (int i = 0; i < 100_000; i++)
        {
            h = (h * 31 + i) % 1_000_000_007;
        }

        return (int)(h % 100);
    }

    public void Run(CancellationToken ct)
    {
        List<int> orderIds = new List<int>(OrderCount);

        for (int id = 1; id <= OrderCount; id++)
        {
            orderIds.Add(id);
        }

        // Sequential run
        List<int> sequentialFlaggedIds = new List<int>();

        Stopwatch sequentialStopwatch = Stopwatch.StartNew();

        foreach (int orderId in orderIds)
        {
            ct.ThrowIfCancellationRequested();

            int score = Score(orderId);

            if (score >= FlagThreshold)
            {
                sequentialFlaggedIds.Add(orderId);
            }
        }

        sequentialStopwatch.Stop();

        // Parallel run
        ConcurrentBag<int> parallelFlaggedIds = new ConcurrentBag<int>();
        ConcurrentDictionary<int, byte> usedThreads =
            new ConcurrentDictionary<int, byte>();

        ParallelOptions parallelOptions = new ParallelOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        Stopwatch parallelStopwatch = Stopwatch.StartNew();

        Parallel.ForEach(
            orderIds,
            parallelOptions,
            orderId =>
            {
                usedThreads.TryAdd(
                    Environment.CurrentManagedThreadId,
                    0);

                int score = Score(orderId);

                if (score >= FlagThreshold)
                {
                    parallelFlaggedIds.Add(orderId);
                }
            });

        parallelStopwatch.Stop();

        // Compare results
        List<int> parallelFlaggedIdsList =
            parallelFlaggedIds.OrderBy(id => id).ToList();

        List<int> sequentialFlaggedIdsSorted =
            sequentialFlaggedIds.OrderBy(id => id).ToList();

        bool sameResults =
            sequentialFlaggedIdsSorted.SequenceEqual(
                parallelFlaggedIdsList);

        if (!sameResults)
        {
            throw new InvalidOperationException(
                "Sequential and parallel results do not match.");
        }

        if (sequentialFlaggedIds.Count != 1005 ||
            !sequentialFlaggedIdsSorted.Take(5)
                .SequenceEqual(new[] { 4, 8, 12, 29, 33 }))
        {
            throw new InvalidOperationException(
                "Fraud scoring results do not match expected values.");
        }

        double speedUp =
            (double)sequentialStopwatch.ElapsedMilliseconds /
            parallelStopwatch.ElapsedMilliseconds;

        Console.WriteLine();
        Console.WriteLine("========== FRAUD SCORING ==========");
        Console.WriteLine($"Flagged orders: {parallelFlaggedIdsList.Count}");
        Console.WriteLine(
            $"Sequential time: {sequentialStopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine(
            $"Parallel time: {parallelStopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"Speed-up: {speedUp:F2}x");
        Console.WriteLine($"Distinct threads used: {usedThreads.Count}");
        Console.WriteLine(
            $"Processor count: {Environment.ProcessorCount}");
        Console.WriteLine(
            $"First five flagged IDs: {string.Join(", ", parallelFlaggedIdsList.Take(5))}");
        Console.WriteLine("Results match: Yes");
    }
}
