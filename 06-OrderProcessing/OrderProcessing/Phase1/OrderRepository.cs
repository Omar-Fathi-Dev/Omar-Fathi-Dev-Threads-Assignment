using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1;

public class OrderRepository(JsonFileStore store, IEnumerable<int>? failSaveForOrderIds = null, bool enableFailures = true)
{
    private JsonFileStore Store { get; } = store ?? throw new ArgumentNullException(nameof(store));

    private readonly HashSet<int> _failSaveForOrderIds = enableFailures ? failSaveForOrderIds?.ToHashSet() ?? new HashSet<int>() : new HashSet<int>();

    public async Task SaveAsync(Order order, CancellationToken ct)
    {
        Log.Info("OrderRepository", $"Order #{order.Id}: Opening connection");
        await Task.Delay(200, ct);

        Log.Info("OrderRepository", $"Order #{order.Id}: Beginning transaction");
        await Task.Delay(100, ct);

        Log.Info("OrderRepository", $"Order #{order.Id}: Inserting or updating order");
        await Task.Delay(400, ct);
        
        if (order.Status == OrderStatus.Completed &&
            _failSaveForOrderIds.Contains(order.Id))
        {
            Log.Error(
                "OrderRepository",
                $"Order #{order.Id}: Database connection lost during commit.");

            throw new DatabaseWriteException(
                "Connection lost during commit");
        }
        string path = DataFiles.GetPath(DataFiles.Orders);

        await Store.UpdateAsync<Order>(
            path,
            orders =>
            {
                int index = orders.FindIndex(o => o.Id == order.Id);

                if (index == -1)
                    orders.Add(order);
                else
                    orders[index] = order;
                return orders;
            }, ct);

        Log.Info("OrderRepository", $"Order #{order.Id}: Committing transaction");
        await Task.Delay(200, ct);

        Log.Success("OrderRepository", $"Order #{order.Id} saved successfully");
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

    public async Task<List<Order>> GetByStatusAsync(
        OrderStatus s,
        CancellationToken ct)
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
    
    public void Save(Order order)
    {
        Log.Info("OrderRepository", $"Order #{order.Id}: Opening connection");
        Thread.Sleep(200);

        Log.Info("OrderRepository", $"Order #{order.Id}: Beginning transaction");
        Thread.Sleep(100);

        Log.Info("OrderRepository", $"Order #{order.Id}: Inserting or updating order");
        Thread.Sleep(400);

        if (order.Status == OrderStatus.Completed &&
            _failSaveForOrderIds.Contains(order.Id))
        {
            Log.Error(
                "OrderRepository",
                $"Order #{order.Id}: Database connection lost during commit.");

            throw new DatabaseWriteException("Connection lost during commit");
        }

        string path = DataFiles.GetPath(DataFiles.Orders);

        Store.Update<Order>(
            path,
            orders =>
            {
                int index = orders.FindIndex(o => o.Id == order.Id);

                if (index == -1)
                    orders.Add(order);
                else
                    orders[index] = order;

                return orders;
            });

        Log.Info("OrderRepository", $"Order #{order.Id}: Committing transaction");
        Thread.Sleep(200);

        Log.Success("OrderRepository", $"Order #{order.Id} saved successfully");
    }
}