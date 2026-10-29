using System.Text;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase2;

public class InvoiceGenerator
{
    public async Task GenerateAsync(Order order, CancellationToken ct)
    {
        Log.Info("InvoiceGenerator", $"Starting invoice generation for Order #{order.Id}");

        Log.Info("InvoiceGenerator", "Rendering invoice");
        
        await Task.Run(() =>
        {
            long result = 0;
            for (int i = 0; i < 20_000_000; i++)
            {
                ct.ThrowIfCancellationRequested();
                result += i;
            }
        }, ct);

        ct.ThrowIfCancellationRequested();

        string directoryPath = Path.Combine(DataFiles.Root, "invoices");
        Directory.CreateDirectory(directoryPath);

        string filePath = Path.Combine(directoryPath, $"invoice_{order.Id}.txt");

        StringBuilder invoice = new StringBuilder();

        invoice.AppendLine($"Invoice #{order.Id}");
        invoice.AppendLine($"Customer ID: {order.CustomerId}");
        invoice.AppendLine($"Created At: {order.CreatedAt}");
        invoice.AppendLine("----------------------------------------");
        foreach (OrderItem item in order.Items)
        {
            invoice.AppendLine($"Product ID: {item.ProductId}, Quantity: {item.Quantity}, Unit Price: {item.UnitPrice:C}");
        }
        invoice.AppendLine("----------------------------------------");
        invoice.AppendLine($"Subtotal: {order.SubTotal:C}");
        invoice.AppendLine($"Discount: {order.Discount:C}");
        invoice.AppendLine($"Shipping: {order.ShippingCost:C}");
        invoice.AppendLine($"Total: {order.TotalAmount:C}");

        await File.WriteAllTextAsync(filePath, invoice.ToString(), ct);

        Log.Success("InvoiceGenerator", $"Invoice generated: {filePath}");
    }
}