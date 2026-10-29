## Phase 1 Questions

### 1. Why are the sync and async times almost the same?

Both process orders one by one. Async does not process orders at the same time.

### 2. Does the thread ID always stay the same after `await`?

No. The program may continue on a different thread. My logs show different thread IDs.

### 3. What happens during `Task.Delay()` and `Thread.Sleep()`?

`Task.Delay()` frees the thread. `Thread.Sleep()` blocks the thread.

### 4. Why return `Task<T>` instead of `T`?

It allows asynchronous work without blocking the thread while waiting for I/O.

### 5. What is the difference between `File.ReadAllText` and `File.ReadAllTextAsync`?

`File.ReadAllText()` blocks the thread while reading. `File.ReadAllTextAsync()` allows the thread to do other work while waiting for the file operation.

## Phase 2 Questions

**1. What is the difference between starting a task and awaiting it?**  
Starting a task begins its work. `await` waits for it to finish without blocking the thread.

**2. Does every Task create a new thread? Does Task.Delay() use a thread while waiting?**  
No. `Task` does not always create a new thread. `Task.Delay()` does not need a thread while waiting.

**3. What happens if you use `.Result` or `.Wait()` instead of `await`? Why is it dangerous?**  
They block the current thread. This can cause deadlocks and waste threads.

**4. What happens to an exception inside a task that is never awaited?**  
The exception may go unnoticed by the code.

**5. When should you use `Task.Run()`, and when should you avoid it?**  
Use it for CPU-heavy work. Avoid it for normal async I/O, like database calls.

**6. What is the difference between `Task` and `Task<T>`?**  
`Task` has no result. `Task<T>` returns a result of type `T`.

**7. Why can't a `lock` block contain `await`?**  
Because `lock` must be released by the same thread, but `await` may continue on another thread.

**8. Why did the unprotected stock reservation oversell, and how did you prevent it?**  
Both orders read the same stock before updating it. I used `SemaphoreSlim` inside `JsonFileStore` to protect the update.