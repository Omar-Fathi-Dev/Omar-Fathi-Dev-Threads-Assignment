using OrderProcessing.Phase1;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase3;

public class SyncInventoryService(ProductRepository repository)
{
    private readonly ProductRepository _productRepository = repository;

    public bool ReserveStock(Order order)
    {
        List<OrderItem> reservedItems = new List<OrderItem>();

        Log.Info("SyncInventoryService", $"Opening connection to reserve stock for Order #{order.Id}...");
        Thread.Sleep(200);

        Log.Info("SyncInventoryService", $"Beginning transaction for Order #{order.Id}...");
        Thread.Sleep(100);

        foreach (OrderItem item in order.Items)
        {
            Log.Info("SyncInventoryService", $"Reserving Product #{item.ProductId} for Order #{order.Id}...");
            Thread.Sleep(400);

            bool reserved = _productRepository.ReserveStock(item.ProductId, item.Quantity);

            if (reserved)
            {
                reservedItems.Add(item);
            }
            else
            {
                Log.Error("SyncInventoryService", $"Insufficient stock for Product #{item.ProductId}. Rolling back...");

                foreach (OrderItem reservedItem in reservedItems)
                {
                    Log.Info("SyncInventoryService", $"Releasing Product #{reservedItem.ProductId}...");

                    _productRepository.ReleaseStock(
                        reservedItem.ProductId,
                        reservedItem.Quantity);
                }

                Log.Info("SyncInventoryService", $"Transaction rolled back for Order #{order.Id}.");
                return false;
            }
        }

        Log.Info("SyncInventoryService", $"Committing transaction for Order #{order.Id}...");
        Thread.Sleep(200);

        Log.Success("SyncInventoryService", $"Stock reserved for Order #{order.Id}.");
        return true;
    }

    public void ReleaseStock(Order order)
    {
        Log.Info("SyncInventoryService", $"Opening connection to release stock for Order #{order.Id}...");
        Thread.Sleep(200);

        Log.Info("SyncInventoryService", $"Beginning transaction to release stock for Order #{order.Id}...");
        Thread.Sleep(100);

        foreach (OrderItem item in order.Items)
        {
            Log.Info("SyncInventoryService", $"Releasing Product #{item.ProductId} for Order #{order.Id}...");
            Thread.Sleep(400);

            _productRepository.ReleaseStock(
                item.ProductId,
                item.Quantity);
        }

        Log.Info("SyncInventoryService", $"Committing transaction to release stock for Order #{order.Id}...");
        Thread.Sleep(200);

        Log.Success("SyncInventoryService", $"Stock released for Order #{order.Id}.");
    }
}