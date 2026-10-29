using OrderProcessing.Phase1;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase2;

public class OrderProcessorV2(
    OrderRepository orderRepository,
    ProductRepository productRepository,
    OrderPricer  orderPricer,
    InvoiceGenerator invoiceGenerator)
{
    private readonly OrderRepository _orderRepository = orderRepository;
    private readonly ProductRepository _productsRepository = productRepository;
    private readonly OrderPricer _orderPricer = orderPricer;
    private readonly InvoiceGenerator _invoiceGenerator = invoiceGenerator;
    
    
  
    public async Task<bool> ProcessValidatedOrderAsync(
        Order order,
        CancellationToken ct)
    {
        List<OrderItem> reservedItems = new List<OrderItem>();
        Product? unreservedProduct = null;

        foreach (OrderItem item in order.Items)
        {
            bool reserved = await _productsRepository.ReserveStockAsync(
                item.ProductId,
                item.Quantity,
                ct);

            if (reserved)
            {
                reservedItems.Add(item);
            }
            else
            {
                unreservedProduct = await _productsRepository.GetByIdAsync(
                    item.ProductId,
                    ct);

                break;
            }
        }

        if (reservedItems.Count < order.Items.Count)
        {
            foreach (OrderItem item in reservedItems)
            {
                await _productsRepository.ReleaseStockAsync(
                    item.ProductId,
                    item.Quantity,
                    ct);
            }

            if (unreservedProduct is null)
            {
                throw new InvalidOperationException("Product not found.");
            }

            order.Status = OrderStatus.Rejected;
            order.FailureReason =
                $"Insufficient stock for '{unreservedProduct.Name}'";

            await _orderRepository.SaveAsync(order, ct);

            return false;
        }

        try
        {
            await _orderPricer.PriceOrderAsync(order, ct);

            await _invoiceGenerator.GenerateAsync(order, ct);

            order.Status = OrderStatus.Completed;

            await _orderRepository.SaveAsync(order, ct);

            return true;
        }
        catch
        {
            foreach (OrderItem item in reservedItems)
            {
                await _productsRepository.ReleaseStockAsync(
                    item.ProductId,
                    item.Quantity,
                    ct);
            }

            throw;
        }
    }

}

