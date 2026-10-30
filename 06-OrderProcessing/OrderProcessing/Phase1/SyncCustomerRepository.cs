using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1;

public class SyncCustomerRepository(JsonFileStore store)
{
    private JsonFileStore Store { get; } = store ?? throw new ArgumentNullException(nameof(store));

    public Customer? GetById(int id)
    {
        Log.Info("SyncCustomerRepository", "Opening connection");
        Thread.Sleep(200);

        Log.Info("SyncCustomerRepository", "Executing query");
        int delayMilliseconds = Random.Shared.Next(300, 601);
        Thread.Sleep(delayMilliseconds);

        List<Customer> customers = Store.Read<Customer>(
            DataFiles.GetPath(DataFiles.Customers));

        Customer? customer = customers.Find(c => c.Id == id);

        return customer;
    }
}