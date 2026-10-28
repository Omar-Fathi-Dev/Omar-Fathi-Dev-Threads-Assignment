using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1;

public class IncomingOrderRepository(JsonFileStore store)
{
    private JsonFileStore Store { get; } =
        store ?? throw new ArgumentNullException(nameof(store));

    public async Task<List<Order>> GetAllAsync(CancellationToken ct)
    {
        Log.Info("IncomingOrderRepository", "Opening connection");
        await Task.Delay(200, ct);

        Log.Info("IncomingOrderRepository", "Executing query");
        int delayMilliseconds = Random.Shared.Next(300, 601);
        await Task.Delay(delayMilliseconds, ct);

        string path = DataFiles.GetPath(DataFiles.IncomingOrders);
        List<Order> orders = await Store.ReadAsync<Order>(path, ct);

        return orders;
    }
}