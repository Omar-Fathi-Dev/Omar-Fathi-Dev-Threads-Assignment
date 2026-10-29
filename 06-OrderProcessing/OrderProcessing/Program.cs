using OrderProcessing.Phase1;
using OrderProcessing.Phase2;

namespace OrderProcessing;

class Program
{
    static async Task Main(string[] args)
    {
        #region Phase 1
            // await Phase1Composition.RunAsync(CancellationToken.None);
        #endregion
        
        #region Phase 2
            await Phase2Composition.RunAsync(CancellationToken.None);
            //await RaceConditionDemo.RunAsync(CancellationToken.None);
        #endregion
        
    }
}