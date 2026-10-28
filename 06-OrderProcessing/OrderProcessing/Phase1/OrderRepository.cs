using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1;

public class OrderRepository(JsonFileStore store)
{
    private JsonFileStore Store { get; } = store ?? throw new ArgumentNullException(nameof(store));

    public async Task SaveAsync(Order order, CancellationToken ct)
    {
        Log.Info("OrderRepository", "Opening connection");
        await Task.Delay(200, ct);

        Log.Info("OrderRepository", "Beginning transaction");
        await Task.Delay(100, ct);

        Log.Info("OrderRepository", "Inserting or updating order");
        await Task.Delay(400, ct);

        string path = DataFiles.GetPath(DataFiles.Orders);

        await Store.UpdateAsync<Order>(path, orders =>
        {
            int index = orders.FindIndex(o => o.Id == order.Id);

            if (index == -1)
                orders.Add(order);
            else
                orders[index] = order;

            return orders;
        }, ct);

        Log.Info("OrderRepository", "Committing transaction");
        await Task.Delay(200, ct);

        Log.Success("OrderRepository", $"Order {order.Id} saved successfully");
    }
    
    public async Task<List<Order>> GetAllAsync(CancellationToken ct)
    {
        Log.Info("OrderRepository", "Opening connection");
        await Task.Delay(200, ct);

        Log.Info("OrderRepository", "Executing query");
        int delayMilliseconds = Random.Shared.Next(300, 601);
        await Task.Delay(delayMilliseconds, ct);

        string path = DataFiles.GetPath(DataFiles.Orders);
        List<Order> orders = await Store.ReadAsync<Order>(path, ct);

        return orders;
    }

    public async Task<List<Order>> GetByStatusAsync(OrderStatus s, CancellationToken ct)
    {
        Log.Info("OrderRepository", "Opening connection");
        await Task.Delay(200, ct);

        Log.Info("OrderRepository", "Executing query");
        int delayMilliseconds = Random.Shared.Next(300, 601);
        await Task.Delay(delayMilliseconds, ct);

        string path = DataFiles.GetPath(DataFiles.Orders);
        List<Order> orders = await Store.ReadAsync<Order>(path, ct);

        List<Order> filteredOrders = new List<Order>();

        foreach (Order order in orders)
        {
            if (order.Status == s)
                filteredOrders.Add(order);
        }

        return filteredOrders;
    }
}