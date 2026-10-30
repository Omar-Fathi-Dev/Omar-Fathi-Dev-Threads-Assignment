using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1;

public class SyncProductRepository(JsonFileStore store)
{
    private JsonFileStore Store { get; } =
        store ?? throw new ArgumentNullException(nameof(store));

    public Product? GetById(int id)
    {
        Log.Info("SyncProductRepository", "Opening connection");
        Thread.Sleep(200);

        Log.Info("SyncProductRepository", "Executing query");
        int delayMilliseconds = Random.Shared.Next(300, 601);
        Thread.Sleep(delayMilliseconds);

        List<Product> products = Store.Read<Product>(
            DataFiles.GetPath(DataFiles.Products));

        Product? product = products.Find(p => p.Id == id);

        return product;
    }

    public bool ReserveStock(int productId, int qty)
    {
        Log.Info("SyncProductRepository", "Opening connection");
        Thread.Sleep(200);

        Log.Info("SyncProductRepository", "Beginning transaction");
        Thread.Sleep(100);

        Log.Info("SyncProductRepository", "Updating product stock");
        Thread.Sleep(400);

        string path = DataFiles.GetPath(DataFiles.Products);
        bool reserved = false;

        Store.Update<Product>(path, items =>
        {
            Product? product = items.Find(p => p.Id == productId);

            if (product is null || product.StockQuantity < qty)
                return items;

            product.StockQuantity -= qty;
            reserved = true;

            return items;
        });

        Log.Info("SyncProductRepository", "Committing transaction");
        Thread.Sleep(200);

        return reserved;
    }
}