using OrderProcessing.Phase1;

namespace OrderProcessing;

class Program
{
    static async Task Main(string[] args)
    {
        #region Phase 1
            await Phase1Composition.RunAsync(CancellationToken.None);
        #endregion
        
    }
}