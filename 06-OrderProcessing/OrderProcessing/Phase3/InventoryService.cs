using OrderProcessing.Phase1;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase3;

public class InventoryService(ProductRepository repository)
{
    private readonly ProductRepository _productRepository = repository;

    public async Task<bool> ReserveStockAsync(Order order, CancellationToken ct)
    {
        List<OrderItem> reservedItems = new List<OrderItem>();

        Log.Info("InventoryService", $"Opening connection to reserve stock for Order #{order.Id}...");
        await Task.Delay(200, ct);

        Log.Info("InventoryService", $"Beginning transaction for Order #{order.Id}...");
        await Task.Delay(100, ct);

        foreach (OrderItem item in order.Items)
        {
            Log.Info("InventoryService", $"Reserving Product #{item.ProductId} for Order #{order.Id}...");
            await Task.Delay(400, ct);

            bool reserved = await _productRepository.ReserveStockAsync(
                item.ProductId, item.Quantity, ct);

            if (reserved)
            {
                reservedItems.Add(item);
            }
            else
            {
                Log.Error("InventoryService", $"Insufficient stock for Product #{item.ProductId}. Rolling back...");

                foreach (OrderItem reservedItem in reservedItems)
                {
                    Log.Info("InventoryService", $"Releasing Product #{reservedItem.ProductId}...");

                    await _productRepository.ReleaseStockAsync(
                        reservedItem.ProductId, reservedItem.Quantity, ct);
                }

                Log.Info("InventoryService", $"Transaction rolled back for Order #{order.Id}.");
                return false;
            }
        }

        Log.Info("InventoryService", $"Committing transaction for Order #{order.Id}...");
        await Task.Delay(200, ct);

        Log.Success("InventoryService", $"Stock reserved for Order #{order.Id}.");
        return true;
    }

    public async Task ReleaseStockAsync(Order order, CancellationToken ct)
    {
        Log.Info("InventoryService", $"Opening connection to release stock for Order #{order.Id}...");
        await Task.Delay(200, ct);

        Log.Info("InventoryService", $"Beginning transaction to release stock for Order #{order.Id}...");
        await Task.Delay(100, ct);

        foreach (OrderItem item in order.Items)
        {
            Log.Info("InventoryService", $"Releasing Product #{item.ProductId} for Order #{order.Id}...");
            await Task.Delay(400, ct);

            await _productRepository.ReleaseStockAsync(
                item.ProductId, item.Quantity, ct);
        }

        Log.Info("InventoryService", $"Committing transaction to release stock for Order #{order.Id}...");
        await Task.Delay(200, ct);

        Log.Success("InventoryService", $"Stock released for Order #{order.Id}.");
    }
}

