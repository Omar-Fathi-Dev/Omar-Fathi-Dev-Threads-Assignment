
using System.Diagnostics;
using OrderProcessing.Phase1;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase4;

public class CustomerDashboardService
{
    private readonly JsonFileStore _store;
    private readonly CustomerRepository _customerRepository;
    private readonly OrderRepository _orderRepository;
    private readonly HashSet<int> _failPaymentsForCustomerIds;

    public CustomerDashboardService(
        JsonFileStore store,
        CustomerRepository customerRepository,
        OrderRepository orderRepository,
        IEnumerable<int>? failPaymentsForCustomerIds = null)
    {
        _store = store;
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;

        _failPaymentsForCustomerIds =
            failPaymentsForCustomerIds?.ToHashSet() ?? new HashSet<int>();
    }

    public async Task<CustomerDashboard> LoadAsync(
        int customerId,
        CancellationToken ct)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        // Start the four independent loads.
        Task<Customer> profileTask = LoadProfileAsync(customerId, ct);
        Task<List<Order>> ordersTask = LoadOrdersAsync(customerId, ct);
        Task<List<PaymentTransaction>> paymentsTask =
            LoadPaymentsAsync(customerId, ct);
        Task<List<EmailMessage>> emailsTask =
            LoadEmailsAsync(customerId, ct);

        // Wait for all loads using one await.
        Task allLoadsTask = Task.WhenAll(
            profileTask,
            ordersTask,
            paymentsTask,
            emailsTask);

        try
        {
            await allLoadsTask;
        }
        catch
        {
            stopwatch.Stop();

            AggregateException? aggregateException =
                allLoadsTask.Exception;

            if (aggregateException is not null)
            {
                foreach (Exception exception in
                    aggregateException.Flatten().InnerExceptions)
                {
                    Console.WriteLine($"- {exception.Message}");
                }
            }

            Console.WriteLine(
                $"Customer #{customerId} dashboard load failed " +
                $"in {stopwatch.ElapsedMilliseconds} ms.");

            throw;
        }

        stopwatch.Stop();

        CustomerDashboard dashboard = new CustomerDashboard
        {
            Customer = profileTask.Result,
            Orders = ordersTask.Result,
            Payments = paymentsTask.Result,
            Emails = emailsTask.Result
        };

        Console.WriteLine(
            $"Customer #{customerId} dashboard loaded " +
            $"in {stopwatch.ElapsedMilliseconds} ms.");

        return dashboard;
    }

    private async Task<Customer> LoadProfileAsync(
        int customerId,
        CancellationToken ct)
    {

        Customer? customer =
            await _customerRepository.GetByIdAsync(customerId, ct);

        if (customer is null)
        {
            throw new InvalidOperationException(
                $"Customer #{customerId} not found");
        }

        return customer;
    }

    private async Task<List<Order>> LoadOrdersAsync(
        int customerId,
        CancellationToken ct)
    {
        await SimulateDatabaseQueryAsync(ct);

        List<Order> allOrders =
            await _orderRepository.GetAllAsync(ct);

        List<Order> customerOrders = new List<Order>();

        foreach (Order order in allOrders)
        {
            if (order.CustomerId == customerId)
            {
                customerOrders.Add(order);
            }
        }

        return customerOrders;
    }

    private async Task<List<PaymentTransaction>> LoadPaymentsAsync(
        int customerId,
        CancellationToken ct)
    {
        await SimulateDatabaseQueryAsync(ct);

        if (_failPaymentsForCustomerIds.Contains(customerId))
        {
            throw new TimeoutException("Payments store timeout");
        }

        List<Order> allOrders =
            await _store.ReadAsync<Order>(
                DataFiles.GetPath(DataFiles.Orders),
                ct);

        HashSet<int> customerOrderIds = new HashSet<int>();

        foreach (Order order in allOrders)
        {
            if (order.CustomerId == customerId)
            {
                customerOrderIds.Add(order.Id);
            }
        }

        List<PaymentTransaction> allPayments =
            await _store.ReadAsync<PaymentTransaction>(
                DataFiles.GetPath(DataFiles.Payments),
                ct);

        List<PaymentTransaction> customerPayments =
            new List<PaymentTransaction>();

        foreach (PaymentTransaction payment in allPayments)
        {
            if (customerOrderIds.Contains(payment.OrderId))
            {
                customerPayments.Add(payment);
            }
        }

        return customerPayments;
    }

    private async Task<List<EmailMessage>> LoadEmailsAsync(
        int customerId,
        CancellationToken ct)
    {
        await SimulateDatabaseQueryAsync(ct);

        List<Order> allOrders =
            await _store.ReadAsync<Order>(
                DataFiles.GetPath(DataFiles.Orders),
                ct);

        HashSet<int> customerOrderIds = new HashSet<int>();

        foreach (Order order in allOrders)
        {
            if (order.CustomerId == customerId)
            {
                customerOrderIds.Add(order.Id);
            }
        }

        List<EmailMessage> allEmails =
            await _store.ReadAsync<EmailMessage>(
                DataFiles.GetPath(DataFiles.Emails),
                ct);

        List<EmailMessage> customerEmails =
            new List<EmailMessage>();

        foreach (EmailMessage email in allEmails)
        {
            if (customerOrderIds.Contains(email.OrderId))
            {
                customerEmails.Add(email);
            }
        }

        return customerEmails;
    }

    private static async Task SimulateDatabaseQueryAsync(
        CancellationToken ct)
    {
        await Task.Delay(200, ct);

        int queryDelay = Random.Shared.Next(300, 601);

        await Task.Delay(queryDelay, ct);
    }
}
