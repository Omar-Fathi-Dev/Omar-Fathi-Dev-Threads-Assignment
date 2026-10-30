using OrderProcessing.Phase1;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase3;

public class SyncCustomerService(CustomerRepository customerRepository)
{
    private readonly CustomerRepository _customerRepository = customerRepository;

    public Customer GetCustomer(int id)
    {
        Log.Info("SyncCustomerService", $"Getting customer #{id}...");

        Customer? customer = _customerRepository.GetById(id);

        if (customer is null)
        {
            Log.Error("SyncCustomerService", $"Customer #{id} not found.");
            throw new CustomerNotFoundException($"Customer #{id} not found");
        }

        Log.Success("SyncCustomerService", $"Customer #{id} found.");
        return customer;
    }
}