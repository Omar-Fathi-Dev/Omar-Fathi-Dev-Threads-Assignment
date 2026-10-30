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

## Task 9.6 — Sync vs Async Comparison

### 1. Does async make one order's workflow faster?

Not always. Async mainly helps Threads do other work while waiting.

### 2. Why does async scale better for many orders?

Because it uses Threads better and does not block them while waiting for I/O.

### 3. What happens to the sync version with 1,000 orders?

It creates 1,000 Threads, which uses a lot of memory and system resources.

### 4. Why is async essential for apps that call databases, files and APIs?

Because these operations take time. Async lets Threads do other work instead of waiting.

### Actual Experiment Results

| Metric | Sync | Async |
|---|---:|---:|
| Total Orders | 10 | 10 |
| Completed | 8 | 8 |
| Rejected | 2 | 2 |
| Failed | 0 | 0 |
| Execution Time | 15,731 ms | 13,905 ms |
| Observed Threads | 10 | 5 |

Both versions produced the same business results. The Async version completed the experiment approximately 1.8 seconds faster and used fewer observed Threads.

## Task 9.7 — Fire-and-Forget Investigation

### 1. When is `await` necessary?

Use `await` when the current method needs to wait for an asynchronous operation to finish, when the next step depends on it, or when we need to handle its exceptions.

### 2. When is fire-and-forget acceptable?

It may be acceptable for independent, non-critical tasks. However, important operations such as sending order confirmations should be monitored and should not be ignored.

### 3. Why is `async void` dangerous?

An `async void` method cannot be awaited. The caller cannot track when it finishes, and exceptions cannot be handled through the normal `await` flow.

### 4. Why is `async void` acceptable for event handlers?

Some event handlers require a `void` return type, such as `EventHandler`. Therefore, `async void` is used when an event handler needs asynchronous operations. Exceptions should be handled inside the event handler.

### Actual Experiment Results

| Metric | Without `await` | With `await` |
|---|---|---|
| Method waits for email | No | Yes |
| Email completed before method returns | No | Yes, if successful |
| Exception propagated to caller | No, not through normal `await` flow | Yes |
| "Order processing finished" after email | No guarantee | Yes, if successful |
| Application may exit before email completes | Yes | No, while awaiting |

**Experiment observations:**

- Without `await`, the method printed `Order processing finished` while the email was still running.
- With `await`, the method waited until the email operation completed.
- When the awaited email operation failed, its exception propagated to the caller.
- Without `await`, the application exited before the email operation reached the failure point in our test.

## Phase 4 Questions

### 1. Which tool did you use in Tasks 10.1–10.4, and why not start and await one by one?

- **10.1:** `Task.WhenEach` to process orders as they finish.
- **10.2:** `Task.WhenAll` to load data at the same time.
- **10.3:** `Task.WhenAny` to get the first successful shipping quote.
- **10.4 A:** `Parallel.ForEach` to use multiple CPU cores.
- **10.4 B:** `Parallel.ForEachAsync` to send emails with a limit of 4.

Starting and awaiting one by one makes each operation wait for the previous one. This is slower and does not meet the concurrency requirements.

### 2. In 10.2, how do you get every exception? What does a plain await give you?

Use `Task.WhenAll` and inspect each task's `Exception`. A plain `await` throws one exception.

### 3. What happens to losing carriers in 10.3 if you never cancel them?

They keep running and waste time and resources, even after a winner is found.

### 4. Why does 10.4 A get faster with more CPU cores, while 10.4 B does not depend on the number of cores?

A is CPU-bound, so more cores can do more work at the same time.

B is I/O-bound. It spends most of its time waiting for emails to send, not using the CPU.

### 5. What is the difference between parallel work (10.4 A) and concurrent asynchronous work (10.1)?

Parallel work uses multiple CPU cores to calculate at the same time.

Concurrent asynchronous work handles multiple operations while they wait for I/O.

### 6. Show a cancelled run (`--cancel-after`) and explain what happened to the orders, stock and payments.

The cancelled run stopped processing when the cancellation token was triggered.

- **Orders:** Some orders were completed, while the remaining orders were not processed.
- **Stock:** Stock changes from completed operations remained. Unprocessed orders did not reserve stock.
- **Payments:** Payments already completed remained. Operations cancelled before payment did not create successful payments.

Cancellation does not automatically undo completed operations.