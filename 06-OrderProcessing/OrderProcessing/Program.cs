
using OrderProcessing.Phase1;
using OrderProcessing.Phase2;
using OrderProcessing.Phase3;
using OrderProcessing.Phase4;

namespace OrderProcessing;

class Program
{
    static async Task Main(string[] args)
    {
        CancellationTokenSource cts = new CancellationTokenSource();

        try
        {
            int cancelAfterIndex = Array.IndexOf(args, "--cancel-after");

            if (cancelAfterIndex >= 0)
            {
                if (cancelAfterIndex + 1 >= args.Length ||
                    !int.TryParse(args[cancelAfterIndex + 1], out int milliseconds) ||
                    milliseconds <= 0)
                {
                    Console.WriteLine("Invalid --cancel-after value.");
                    return;
                }

                cts.CancelAfter(milliseconds);
            }

            string mode;

            if (args.Length == 0)
            {
                mode = ShowMenu();
            }
            else
            {
                mode = args[0].ToLowerInvariant();

                if (mode.StartsWith("--"))
                {
                    Console.WriteLine("Please provide a run mode.");
                    return;
                }
            }

            CancellationToken ct = cts.Token;

            switch (mode)
            {
                case "phase1":
                    await Phase1Composition.RunAsync(ct);
                    break;

                case "phase2":
                    await Phase2Composition.RunAsync(ct);
                    break;

                case "phase3":
                    await Phase3Composition.RunAsync(ct);
                    break;

                case "phase4":
                    await Phase4Composition.RunAsync(ct);
                    break;

                case "all":
                    await Phase1Composition.RunAsync(ct);
                    await Phase2Composition.RunAsync(ct);
                    await Phase3Composition.RunAsync(ct);
                    await Phase4Composition.RunAsync(ct);
                    break;

                default:
                    Console.WriteLine("Invalid run mode.");
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Operation cancelled.");
        }
        finally
        {
            cts.Dispose();
        }
    }

    static string ShowMenu()
    {
        Console.WriteLine("========== ORDER PROCESSING ==========");
        Console.WriteLine("1. Phase 1");
        Console.WriteLine("2. Phase 2");
        Console.WriteLine("3. Phase 3");
        Console.WriteLine("4. Phase 4");
        Console.WriteLine("5. All Phases");
        Console.Write("Choose an option: ");

        string? choice = Console.ReadLine();

        return choice switch
        {
            "1" => "phase1",
            "2" => "phase2",
            "3" => "phase3",
            "4" => "phase4",
            "5" => "all",
            _ => string.Empty
        };
    }
}
