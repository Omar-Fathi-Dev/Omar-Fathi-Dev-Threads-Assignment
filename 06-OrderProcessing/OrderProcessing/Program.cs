using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing;

class Program
{
    private static async Task Main()
    {
        await DataSeeder.ResetAsync(CancellationToken.None);
    }
}