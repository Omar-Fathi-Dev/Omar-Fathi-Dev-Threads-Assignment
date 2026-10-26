using MenuViews;

var counter = new MenuViewCounter();
const int threads = 8;
const int viewsPerThread = 50_000;
int expected = threads * viewsPerThread;

Task[] workers = new Task[threads];
for (int t = 0; t < threads; t++)
{
    workers[t] = Task.Run(() =>
    {
        for (int i = 0; i < viewsPerThread; i++)
            counter.RegisterView();
    });
}

foreach (Task w in workers)
    await w;

Console.WriteLine($"Expected: {expected}");
Console.WriteLine($"Actual:   {counter.Views}");


