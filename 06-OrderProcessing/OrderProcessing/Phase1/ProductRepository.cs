using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1;

public class ProductRepository(JsonFileStore store)
{
    private JsonFileStore Store { get; } = store ?? throw new ArgumentNullException(nameof(store));
    
    public async Task<Product?> GetByIdAsync(int id , CancellationToken ct)
    {
        Log.Info("ProductRepository", "Opening connection");
        await Task.Delay(200, ct);
        
        Log.Info("ProductRepository", "Executing query");
        int delayMilliseconds = Random.Shared.Next(300, 601);
        await Task.Delay(delayMilliseconds, ct);
        
        List<Product> products = await Store.ReadAsync<Product>(
            DataFiles.GetPath(DataFiles.Products), ct);

        Product? product = products.Find(p => p.Id == id);

        return product;
    }
    
    
    public async Task<bool> ReserveStockAsync(int productId, int qty, CancellationToken ct)
    {
        Log.Info("ProductRepository", "Opening connection");
        await Task.Delay(200, ct);

        Log.Info("ProductRepository", "Beginning transaction");
        await Task.Delay(100, ct);

        Log.Info("ProductRepository", "Updating product stock");
        await Task.Delay(400, ct);

        string path = DataFiles.GetPath(DataFiles.Products);
        bool reserved = false;

        await Store.UpdateAsync<Product>(path, items =>
        {
            Product? product = items.Find(p => p.Id == productId);

            if (product is null || product.StockQuantity < qty)
                return items;

            product.StockQuantity -= qty;
            reserved = true;

            return items;
        }, ct);

        Log.Info("ProductRepository", "Committing transaction");
        await Task.Delay(200, ct);

        return reserved;
    }
    
    
    public async Task<bool> ReserveStockUnsafeAsync(int productId, int qty, CancellationToken ct)
    {
        Product? product = await GetByIdAsync(productId, ct);

        if (product is null || product.StockQuantity < qty)
            return false;

        int newStock = product.StockQuantity - qty;

        Log.Info("ProductRepository", "Opening connection");
        await Task.Delay(200, ct);

        Log.Info("ProductRepository", "Beginning transaction");
        await Task.Delay(100, ct);

        Log.Info("ProductRepository", "Updating product stock");
        await Task.Delay(400, ct);

        string path = DataFiles.GetPath(DataFiles.Products);

        await Store.UpdateAsync<Product>(path, products =>
        {
            Product? currentProduct = products.Find(p => p.Id == productId);

            if (currentProduct is not null)
                currentProduct.StockQuantity = newStock;

            return products;
        }, ct);

        Log.Info("ProductRepository", "Committing transaction");
        await Task.Delay(200, ct);

        return true;
    }

    
    public async Task ReleaseStockAsync(int productId, int qty, CancellationToken ct)
    {
        Log.Info("ProductRepository", "Opening connection");
        await Task.Delay(200, ct);

        Log.Info("ProductRepository", "Beginning transaction");
        await Task.Delay(100, ct);

        Log.Info("ProductRepository", "Updating product stock");
        await Task.Delay(400, ct);

        string path = DataFiles.GetPath(DataFiles.Products);

        await Store.UpdateAsync<Product>(path, items =>
        {
            Product? product = items.Find(p => p.Id == productId);

            if (product is null)
                return items;

            product.StockQuantity += qty;

            return items;
        }, ct);

        Log.Info("ProductRepository", "Committing transaction");
        await Task.Delay(200, ct);
    }

    public async Task<List<Product>> GetAllAsync(CancellationToken ct)
    {
        Log.Info("ProductRepository", "Opening connection");
        await Task.Delay(200, ct);

        Log.Info("ProductRepository", "Executing query");
        int delayMilliseconds = Random.Shared.Next(300, 601);
        await Task.Delay(delayMilliseconds, ct);

        List<Product> products = await Store.ReadAsync<Product>(
            DataFiles.GetPath(DataFiles.Products), ct);

        return products;
    }
}

