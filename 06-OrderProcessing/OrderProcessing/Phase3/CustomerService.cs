using OrderProcessing.Phase1;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase3;

public class CustomerService(CustomerRepository customerRepository)
{
   private readonly CustomerRepository _customerRepository = customerRepository;

   public async Task<Customer> GetCustomerAsync(int id , CancellationToken ct)
   {
      Log.Info("CustomerService", $"Getting customer #{id}...");

      Customer? customer = await _customerRepository.GetByIdAsync(id, ct);

      if (customer is null)
      {
         Log.Error("CustomerService", $"Customer #{id} not found.");
         throw new CustomerNotFoundException($"Customer #{id} not found");
      }

      Log.Success("CustomerService", $"Customer #{id} found.");
      return customer;
   }
}

