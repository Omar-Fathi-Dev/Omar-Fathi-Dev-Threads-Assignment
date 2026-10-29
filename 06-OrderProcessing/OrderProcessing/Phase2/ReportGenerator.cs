using System.Text.Json;
using OrderProcessing.Phase1;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase2;

public class ReportGenerator(OrderRepository orderRepository, ProductRepository productRepository)
{
    private OrderRepository OrderRepository { get; } = orderRepository;
    private ProductRepository ProductRepository { get; } = productRepository;
    
    public async Task<Report> GenerateAsync(CancellationToken ct)
    {
        List<Order> orders = await OrderRepository.GetAllAsync(ct);
        List<Product> products = await ProductRepository.GetAllAsync(ct);
        
        int totalOrders = orders.Count;
        int completedCount = 0;
        int rejectedCount = 0;
        int failedCount = 0;
        int paymentFailedCount = 0;
        decimal totalRevenue = 0;
        
        foreach (Order order in orders)
        {
            if (order.Status == OrderStatus.Completed)
            {
                completedCount++;
                totalRevenue += order.TotalAmount;
            }
            else if (order.Status == OrderStatus.Rejected)
                rejectedCount++;
            
            else if (order.Status == OrderStatus.Failed)
                failedCount++;
            
            else if (order.Status == OrderStatus.PaymentFailed)
                paymentFailedCount++;
        }
        decimal averageOrderValue = completedCount == 0 ? 0 : totalRevenue / completedCount;
        
        Dictionary<int, int> productSales = new Dictionary<int, int>();

        foreach (Order order in orders)
        {
            if (order.Status != OrderStatus.Completed)
                continue;

            foreach (OrderItem item in order.Items)
            {
                if (productSales.ContainsKey(item.ProductId))
                    productSales[item.ProductId] += item.Quantity;
                else
                    productSales[item.ProductId] = item.Quantity;
            }
        }

        int topSellingProductId = 0;
        int topSellingQuantity = 0;

        foreach (KeyValuePair<int, int> sale in productSales)
        {
            if (sale.Value > topSellingQuantity || (sale.Value == topSellingQuantity && sale.Key < topSellingProductId))
            {
                topSellingProductId = sale.Key;
                topSellingQuantity = sale.Value;
            }
        }

        Product? topProduct = products.Find(p => p.Id == topSellingProductId);
        string topSellingProduct = topProduct?.Name ?? string.Empty;
        
        Report report = new Report
        {
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            CompletedCount = completedCount,
            RejectedCount = rejectedCount,
            FailedCount = failedCount,
            PaymentFailedCount = paymentFailedCount,
            TopSellingProduct = topSellingProduct,
            TopSellingQuantity = topSellingQuantity,
            AverageOrderValue = averageOrderValue,
            GeneratedAt = DateTime.Now
        };

        string reportsDirectory = Path.Combine(DataFiles.Root, "reports");
        Directory.CreateDirectory(reportsDirectory);
        string fileName = $"report_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        string filePath = Path.Combine(reportsDirectory, fileName);
        string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json, ct);

        Log.Success("ReportGenerator", $"Report saved to {filePath}");

        return report;
    }
}