using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase3;

public class EmailService(JsonFileStore store, Dictionary<int, int>? failAttempts = null, bool enableFailures = true) : IEmailService
{
    private readonly JsonFileStore _store = store;

    private readonly Dictionary<int, int> _failAttempts = enableFailures ? failAttempts ?? new Dictionary<int, int>() : new Dictionary<int, int>();
    public async Task SendConfirmationAsync(Order order, Customer customer, CancellationToken ct)
    {
        Log.Info("EmailService", $"Order #{order.Id}: Rendering confirmation email...");
        await Task.Delay(200, ct);
        EmailMessage email = new EmailMessage
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            To = customer.Email,
            Subject = $"Order #{order.Id} Confirmation",
            Body = $"Your order #{order.Id} is confirmed. Total: {order.TotalAmount:F2}",
            SentAt = DateTime.Now,
            Attempts = 1,
            LastError = null
        };
        Log.Info("EmailService", $"Order #{order.Id}: Connecting to SMTP...");
        await Task.Delay(300, ct);
        Log.Info("EmailService", $"Order #{order.Id}: Sending email...");
        await Task.Delay(500, ct);
        int maxFailures = _failAttempts.GetValueOrDefault(order.Id);
        if (email.Attempts <= maxFailures)
        {
            email.LastError = "SMTP server unavailable";
            Log.Error("EmailService", $"Order #{order.Id}: Email attempt #{email.Attempts} failed.");
            throw new SmtpUnavailableException("SMTP server unavailable");
        }
        await _store.UpdateAsync<EmailMessage>(
            DataFiles.GetPath(DataFiles.Emails),
            emails =>
            {
                emails.Add(email);
                return emails;
            }, ct);
        Log.Success("EmailService", $"Order #{order.Id}: Confirmation email saved.");
    }
    
    public async Task SendConfirmationWithRetryAsync(
        Order order,
        Customer customer,
        CancellationToken ct)
    {
        int[] retryDelays = { 500, 1000, 2000 };

        int maxFailures = _failAttempts.GetValueOrDefault(order.Id);

        EmailMessage email = new EmailMessage
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            To = customer.Email,
            Subject = $"Order #{order.Id} Confirmation",
            Body = $"Your order #{order.Id} is confirmed. Total: {order.TotalAmount:F2}",
            SentAt = DateTime.Now,
            Attempts = 0,
            LastError = null
        };

        for (int attempt = 1; attempt <= 4; attempt++)
        {
            if (attempt > 1)
            {
                int delay = retryDelays[attempt - 2];

                Log.Info(
                    "EmailService",
                    $"Order #{order.Id}: Waiting {delay}ms before retry #{attempt - 1}...");

                await Task.Delay(delay, ct);
            }

            email.Attempts = attempt;

            try
            {
                Log.Info(
                    "EmailService",
                    $"Order #{order.Id}: Rendering confirmation email (Attempt #{attempt})...");

                await Task.Delay(200, ct);

                Log.Info(
                    "EmailService",
                    $"Order #{order.Id}: Connecting to SMTP...");

                await Task.Delay(300, ct);

                Log.Info(
                    "EmailService",
                    $"Order #{order.Id}: Sending email...");

                await Task.Delay(500, ct);

                if (attempt <= maxFailures)
                {
                    throw new SmtpUnavailableException(
                        "SMTP server unavailable.");
                }

                email.SentAt = DateTime.Now;
                email.LastError = null;

                await _store.UpdateAsync<EmailMessage>(
                    DataFiles.GetPath(DataFiles.Emails),
                    emails =>
                    {
                        emails.Add(email);
                        return emails;
                    },
                    ct);

                Log.Success(
                    "EmailService",
                    $"Order #{order.Id}: Confirmation email sent successfully on attempt #{attempt}.");

                return;
            }
            catch (SmtpUnavailableException ex)
            {
                email.LastError = ex.Message;

                Log.Error(
                    "EmailService",
                    $"Order #{order.Id}: Email attempt #{attempt} failed: {ex.Message}");
            }
        }

        await _store.UpdateAsync<EmailMessage>(
            DataFiles.GetPath(DataFiles.EmailRetry),
            emails =>
            {
                emails.Add(email);
                return emails;
            },
            ct);

        Log.Error(
            "EmailService",
            $"Email for Order #{order.Id} queued for retry later.");
    }
    
    public void SendConfirmationWithRetry(Order order, Customer customer)
{
    int[] retryDelays = { 500, 1000, 2000 };

    int maxFailures = _failAttempts.GetValueOrDefault(order.Id);

    EmailMessage email = new EmailMessage
    {
        Id = Guid.NewGuid(),
        OrderId = order.Id,
        To = customer.Email,
        Subject = $"Order #{order.Id} Confirmation",
        Body = $"Your order #{order.Id} is confirmed. Total: {order.TotalAmount:F2}",
        SentAt = DateTime.Now,
        Attempts = 0,
        LastError = null
    };

    for (int attempt = 1; attempt <= 4; attempt++)
    {
        if (attempt > 1)
        {
            int delay = retryDelays[attempt - 2];

            Log.Info(
                "EmailService",
                $"Order #{order.Id}: Waiting {delay}ms before retry #{attempt - 1}...");

            Thread.Sleep(delay);
        }

        email.Attempts = attempt;

        try
        {
            Log.Info(
                "EmailService",
                $"Order #{order.Id}: Rendering confirmation email (Attempt #{attempt})...");

            Thread.Sleep(200);

            Log.Info(
                "EmailService",
                $"Order #{order.Id}: Connecting to SMTP...");

            Thread.Sleep(300);

            Log.Info(
                "EmailService",
                $"Order #{order.Id}: Sending email...");

            Thread.Sleep(500);

            if (attempt <= maxFailures)
            {
                throw new SmtpUnavailableException(
                    "SMTP server unavailable.");
            }

            email.SentAt = DateTime.Now;
            email.LastError = null;

            _store.Update<EmailMessage>(
                DataFiles.GetPath(DataFiles.Emails),
                emails =>
                {
                    emails.Add(email);
                    return emails;
                });

            Log.Success(
                "EmailService",
                $"Order #{order.Id}: Confirmation email sent successfully on attempt #{attempt}.");

            return;
        }
        catch (SmtpUnavailableException ex)
        {
            email.LastError = ex.Message;

            Log.Error(
                "EmailService",
                $"Order #{order.Id}: Email attempt #{attempt} failed: {ex.Message}");
        }
    }

    _store.Update<EmailMessage>(
        DataFiles.GetPath(DataFiles.EmailRetry),
        emails =>
        {
            emails.Add(email);
            return emails;
        });

    Log.Error(
        "EmailService",
        $"Email for Order #{order.Id} queued for retry later.");
}
}

