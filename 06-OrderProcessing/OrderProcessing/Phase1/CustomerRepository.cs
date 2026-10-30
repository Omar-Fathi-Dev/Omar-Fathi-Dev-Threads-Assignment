using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1;

public class CustomerRepository(JsonFileStore store)
{
    private JsonFileStore Store { get; } = store ?? throw new ArgumentNullException(nameof(store));
    
    public async Task<Customer?> GetByIdAsync(int id , CancellationToken ct)
    {
        Log.Info("CustomerRepository", "Opening connection");
        await Task.Delay(200, ct);
        
        Log.Info("CustomerRepository", "Executing query");
        int delayMilliseconds = Random.Shared.Next(300, 601);
        await Task.Delay(delayMilliseconds, ct);
        
        List<Customer> customers = await Store.ReadAsync<Customer>(
            DataFiles.GetPath(DataFiles.Customers), ct);

        Customer? customer = customers.Find(c => c.Id == id);

        return customer;
    }
    
    public Customer? GetById(int id)
    {
        Log.Info("CustomerRepository", "Opening connection");
        Thread.Sleep(200);

        Log.Info("CustomerRepository", "Executing query");
        int delayMilliseconds = Random.Shared.Next(300, 601);
        Thread.Sleep(delayMilliseconds);

        List<Customer> customers = Store.Read<Customer>(
            DataFiles.GetPath(DataFiles.Customers));

        Customer? customer = customers.Find(c => c.Id == id);

        return customer;
    }
    
}