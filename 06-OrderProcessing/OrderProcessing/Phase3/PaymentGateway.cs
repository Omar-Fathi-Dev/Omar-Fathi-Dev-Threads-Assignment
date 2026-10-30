using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase3;

public class PaymentGateway(
    JsonFileStore store,
    IEnumerable<int>? timeoutOrderIds = null,
    IEnumerable<int>? declineOrderIds = null,
    bool enableFailures = true) : IPaymentGateway
{
    private readonly JsonFileStore _store = store;
    private readonly HashSet<int> _timeoutOrderIds = enableFailures ? timeoutOrderIds?.ToHashSet() ?? new HashSet<int>() : new HashSet<int>();

    private readonly HashSet<int> _declineOrderIds = enableFailures ? declineOrderIds?.ToHashSet() ?? new HashSet<int>() : new HashSet<int>();

    public async Task<PaymentResult> ProcessPaymentAsync(int orderId, decimal amount, CancellationToken ct)
    {
        Log.Info("PaymentGateway", $"Order #{orderId}: Connecting to payment gateway...");
        await Task.Delay(150, ct);

        Log.Info("PaymentGateway", $"Order #{orderId}: Authorizing payment...");
        await Task.Delay(800, ct);

        Log.Info("PaymentGateway", $"Order #{orderId}: Checking fraud...");
        await Task.Delay(500, ct);

        bool success = !_declineOrderIds.Contains(orderId);

        string? transactionId = success ? "TXN-" + Guid.NewGuid().ToString("N")[..12].ToUpper() : null;

        PaymentStatus status = success ? PaymentStatus.Captured : PaymentStatus.Declined;

        string message = success ? "Payment captured" : "Card declined by issuer";

        if (success)
        {
            Log.Info("PaymentGateway", $"Order #{orderId}: Capturing payment...");
            int captureDelay = _timeoutOrderIds.Contains(orderId) ? 5000 : 700;
            await Task.Delay(captureDelay, ct);
        }
        else
        {
            Log.Error("PaymentGateway", $"Order #{orderId}: Card declined by issuer.");
        }

        PaymentTransaction transaction = new PaymentTransaction
        {
            OrderId = orderId,
            Amount = amount,
            Status = status,
            TransactionId = transactionId,
            CreatedAt = DateTime.Now
        };

        await _store.UpdateAsync<PaymentTransaction>(
            DataFiles.GetPath(DataFiles.Payments),
            payments =>
            {
                payments.Add(transaction);
                return payments;
            }, ct);

        Log.Info("PaymentGateway", $"Order #{orderId}: Payment status is {status}.");

        return new PaymentResult
        {
            Success = success,
            TransactionId = transactionId,
            Status = status,
            Message = message
        };
    }

    public async Task RefundAsync(string transactionId, CancellationToken ct)
    {
        Log.Info("PaymentGateway", $"Refunding transaction {transactionId}...");
        await Task.Delay(300, ct);

        await _store.UpdateAsync<PaymentTransaction>(
            DataFiles.GetPath(DataFiles.Payments),
            payments =>
            {
                PaymentTransaction? payment =
                    payments.Find(p => p.TransactionId == transactionId);

                if (payment is not null)
                    payment.Status = PaymentStatus.Refunded;

                return payments;
            }, ct);

        Log.Success("PaymentGateway", $"Transaction {transactionId} refunded.");
    }
    
    public PaymentResult ProcessPayment(int orderId, decimal amount)
    {
        Log.Info("PaymentGateway", $"Order #{orderId}: Connecting to payment gateway...");
        Thread.Sleep(150);

        Log.Info("PaymentGateway", $"Order #{orderId}: Authorizing payment...");
        Thread.Sleep(800);

        Log.Info("PaymentGateway", $"Order #{orderId}: Checking fraud...");
        Thread.Sleep(500);

        bool success = !_declineOrderIds.Contains(orderId);

        string? transactionId = success
            ? "TXN-" + Guid.NewGuid().ToString("N")[..12].ToUpper()
            : null;

        PaymentStatus status = success
            ? PaymentStatus.Captured
            : PaymentStatus.Declined;

        string message = success
            ? "Payment captured"
            : "Card declined by issuer";

        if (success)
        {
            Log.Info("PaymentGateway", $"Order #{orderId}: Capturing payment...");

            int captureDelay = _timeoutOrderIds.Contains(orderId) ? 5000 : 700;
            Thread.Sleep(captureDelay);
        }
        else
        {
            Log.Error("PaymentGateway", $"Order #{orderId}: Card declined by issuer.");
        }

        PaymentTransaction transaction = new PaymentTransaction
        {
            OrderId = orderId,
            Amount = amount,
            Status = status,
            TransactionId = transactionId,
            CreatedAt = DateTime.Now
        };

        _store.Update<PaymentTransaction>(
            DataFiles.GetPath(DataFiles.Payments),
            payments =>
            {
                payments.Add(transaction);
                return payments;
            });

        Log.Info("PaymentGateway", $"Order #{orderId}: Payment status is {status}.");

        return new PaymentResult
        {
            Success = success,
            TransactionId = transactionId,
            Status = status,
            Message = message
        };
    }
    
    public void Refund(string transactionId)
    {
        Log.Info("PaymentGateway", $"Refunding transaction {transactionId}...");
        Thread.Sleep(300);

        _store.Update<PaymentTransaction>(
            DataFiles.GetPath(DataFiles.Payments),
            payments =>
            {
                PaymentTransaction? payment =
                    payments.Find(p => p.TransactionId == transactionId);

                if (payment is not null)
                    payment.Status = PaymentStatus.Refunded;

                return payments;
            });

        Log.Success("PaymentGateway", $"Transaction {transactionId} refunded.");
    }
}

