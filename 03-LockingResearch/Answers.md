# Assignment 03 — Locking Research

**Student Name:** Omar Fathy  
**Group / ID:** 3

## Q1 — What is `System.Threading.Monitor` used for?

`Monitor` controls access to shared data.

It allows only one thread at a time to enter a protected section when using the same lock object.

This helps prevent race conditions when multiple threads use the same data.

## Q2 — What does the C# `lock` statement compile to?

For a normal object, `lock` uses `Monitor.Enter()` and `Monitor.Exit()`.

The compiler uses `try` and `finally` to make sure the lock is released, even if an exception happens.

## Q3 — Differences between `lock` and `Monitor`

1. **Easy to use:** `lock` is simpler to write and releases the lock automatically.
2. **More methods:** `Monitor` has methods like `TryEnter()`, `Wait()`, and `Pulse()`.
3. **Manual release:** When using `Monitor.Enter()` directly, we use `try` and `finally` to release the lock safely.

## Q4 — When might we use `Monitor.TryEnter()`, `Monitor.Pulse()`, or `Monitor.Wait()`?

- **`Monitor.TryEnter()`:** To try to get a lock with a time limit.
- **`Monitor.Wait()`:** To wait for a condition. It releases the lock while waiting and gets it again before returning.
- **`Monitor.Pulse()`:** To notify one waiting thread that it can try to continue.

These methods give us more control over threads than a simple `lock`.

## Q5 — The same critical section using `lock` and `Monitor`

### Using `lock`

```csharp
object gate = new object();
int counter = 0;

lock (gate)
{
    counter++;
}
```

### Using `Monitor.Enter()` and `Monitor.Exit()`

```csharp
bool lockTaken = false;

try
{
    Monitor.Enter(gate, ref lockTaken);
    counter++;
}
finally
{
    if (lockTaken)
    {
        Monitor.Exit(gate);
    }
}
```

Both examples protect the same operation.

Only one thread at a time can update `counter` if all threads use the same `gate` object.

## Sources

1. Dot Net Tutorials — [Monitor Class in C#](https://dotnettutorials.net/lesson/multithreading-using-monitor/)
