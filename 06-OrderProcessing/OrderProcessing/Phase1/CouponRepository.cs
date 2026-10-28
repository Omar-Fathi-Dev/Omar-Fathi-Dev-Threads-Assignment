using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1;

public class CouponRepository(JsonFileStore store)
{
    private JsonFileStore Store { get; } =
        store ?? throw new ArgumentNullException(nameof(store));

    public async Task<Coupon?> GetByCodeAsync(
        string code,
        CancellationToken ct)
    {
        Log.Info("CouponRepository", "Opening connection");
        await Task.Delay(200, ct);

        Log.Info("CouponRepository", "Executing query");
        int delayMilliseconds = Random.Shared.Next(300, 601);
        await Task.Delay(delayMilliseconds, ct);

        List<Coupon> coupons = await Store.ReadAsync<Coupon>(
            DataFiles.GetPath(DataFiles.Coupons), ct);

        return coupons.Find(c =>
            c.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }
}