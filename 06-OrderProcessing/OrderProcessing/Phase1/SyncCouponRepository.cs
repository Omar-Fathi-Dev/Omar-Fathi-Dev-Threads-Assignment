using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1;

public class SyncCouponRepository(JsonFileStore store)
{
    private JsonFileStore Store { get; } = store ?? throw new ArgumentNullException(nameof(store));

    public Coupon? GetByCode(string code)
    {
        Log.Info("SyncCouponRepository", "Opening connection");
        Thread.Sleep(200);

        Log.Info("SyncCouponRepository", "Executing query");
        int delayMilliseconds = Random.Shared.Next(300, 601);
        Thread.Sleep(delayMilliseconds);

        List<Coupon> coupons = Store.Read<Coupon>(DataFiles.GetPath(DataFiles.Coupons));

        return coupons.Find(c =>
            c.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }
}