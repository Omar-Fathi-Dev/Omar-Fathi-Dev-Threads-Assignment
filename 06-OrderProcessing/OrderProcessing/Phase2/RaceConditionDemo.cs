
using OrderProcessing.Phase1;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase2;

public class RaceConditionDemo
{
    public static async Task RunAsync(CancellationToken ct)
    {
        JsonFileStore store = new JsonFileStore();
        ProductRepository productRepository = new ProductRepository(store);

        string productsPath = DataFiles.GetPath(DataFiles.Products);

        await store.UpdateAsync<Product>(
            productsPath,
            products =>
            {
                Product? keyboard = products.Find(p => p.Id == 4);

                if (keyboard is null)
                    throw new InvalidOperationException(
                        "Mechanical Keyboard not found.");

                keyboard.StockQuantity = 1;
                return products;
            },
            ct);

        Console.WriteLine("\n===== Unsafe Stock Reservation =====");

        Task<bool> order10Task =
            productRepository.ReserveStockUnsafeAsync(4, 1, ct);

        Task<bool> order11Task =
            productRepository.ReserveStockUnsafeAsync(4, 1, ct);

        bool order10Result = await order10Task;
        bool order11Result = await order11Task;

        List<Product> productsAfterReservation =
            await store.ReadAsync<Product>(productsPath, ct);

        Product? finalKeyboard =
            productsAfterReservation.Find(p => p.Id == 4);

        Console.WriteLine($"Order #10 reserved keyboard: {order10Result}");
        Console.WriteLine($"Order #11 reserved keyboard: {order11Result}");
        Console.WriteLine($"Final stock: {finalKeyboard?.StockQuantity}");

        await store.UpdateAsync<Product>(
            productsPath,
            products =>
            {
                Product? keyboard = products.Find(p => p.Id == 4);

                if (keyboard is null)
                    throw new InvalidOperationException(
                        "Mechanical Keyboard not found.");

                keyboard.StockQuantity = 1;
                return products;
            },
            ct);

        Console.WriteLine("\n===== Safe Stock Reservation =====");

        Task<bool> safeOrder10Task =
            productRepository.ReserveStockAsync(4, 1, ct);

        Task<bool> safeOrder11Task =
            productRepository.ReserveStockAsync(4, 1, ct);

        bool safeOrder10Result = await safeOrder10Task;
        bool safeOrder11Result = await safeOrder11Task;

        List<Product> productsAfterSafeReservation =
            await store.ReadAsync<Product>(productsPath, ct);

        Product? finalSafeKeyboard =
            productsAfterSafeReservation.Find(p => p.Id == 4);

        Console.WriteLine($"Order #10 reserved keyboard: {safeOrder10Result}");
        Console.WriteLine($"Order #11 reserved keyboard: {safeOrder11Result}");
        Console.WriteLine($"Final stock: {finalSafeKeyboard?.StockQuantity}");
    }
}
