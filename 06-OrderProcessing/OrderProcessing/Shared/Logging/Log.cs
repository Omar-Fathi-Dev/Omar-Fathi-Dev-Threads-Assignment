namespace OrderProcessing.Shared.Logging;

public  class Log
{
    private static readonly object LockObject = new object();
    
    public static void Info(string repoName , string action )
    {
        Print(repoName, action , ConsoleColor.Gray);
    }
    public static void Error(string repoName , string action )
    {
        Print(repoName, action , ConsoleColor.Red);
    }
    public static void Success(string repoName , string action )
    {
        Print(repoName, action , ConsoleColor.Green);
    }

    private static void Print(string repoName, string action , ConsoleColor color)
    {
        lock (LockObject)
        {
            try
            {
                int threadId = Environment.CurrentManagedThreadId;
                Console.ForegroundColor = color;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Thread {threadId}] {repoName} -> {action}");
            }
            finally
            {
                Console.ResetColor();
            }
        }
        
        
    }
       
}

