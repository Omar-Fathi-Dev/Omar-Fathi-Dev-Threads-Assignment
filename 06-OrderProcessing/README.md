## How to Run

Make sure .NET 10 SDK is installed.

Run the project from the project directory:

```bash
dotnet run
```

This opens a menu where you can choose a phase.

You can also run a specific phase directly:

```bash
dotnet run -- phase1
dotnet run -- phase2
dotnet run -- phase3
dotnet run -- phase4
dotnet run -- all
```

To test cancellation, use `--cancel-after` followed by the delay in milliseconds:

```bash
dotnet run -- phase4 --cancel-after 5000
```
### Data Folder Location

The application creates the `Data` folder inside the build output directory:

```text
OrderProcessing/bin/Debug/net10.0/Data
```

The folder contains the JSON files generated during execution.

## Project Structure

```text
OrderProcessing/
│
├── Phase1/
│   ├── Repositories
│   ├── Validators
│   ├── Phase1Composition.cs
│   └── Phase1Runner.cs
│
├── Phase2/
│   ├── InvoiceGenerator.cs
│   ├── OrderPricer.cs
│   ├── OrderProcessorV2.cs
│   ├── Phase2Composition.cs
│   ├── Phase2Runner.cs
│   ├── PricingService.cs
│   ├── RaceConditionDemo.cs
│   └── ReportGenerator.cs
│
├── Phase3/
│   ├── Async Services
│   ├── Sync Services
│   ├── OrderProcessor.cs
│   ├── Phase3Composition.cs
│   ├── Phase3ComparisonRunner.cs
│   └── Phase3Runner.cs
│
├── Phase4/
│   ├── Shipping Carriers
│   ├── CustomerDashboardService.cs
│   ├── EmailResendRunner.cs
│   ├── FraudScoringRunner.cs
│   ├── LiveCheckoutRunner.cs
│   ├── ShippingQuoteService.cs
│   └── Phase4Composition.cs
│
├── Shared/
│   ├── Exceptions/
│   ├── Logging/
│   ├── Models/
│   └── Storage/
│
└── Program.cs
```

### Main Responsibilities

- **Phase1:** Contains order repositories, validators, and the initial order processing workflow.
- **Phase2:** Handles pricing, invoice generation, reporting, and race condition demonstrations.
- **Phase3:** Compares synchronous and asynchronous order processing and services.
- **Phase4:** Handles live checkout, customer dashboards, shipping quotes, fraud scoring, and email re-sending.
- **Shared/Models:** Contains the common data models used across the project.
- **Shared/Storage:** Handles JSON files, data seeding, and file storage operations.
- **Shared/Logging:** Contains logging functionality.
- **Shared/Exceptions:** Contains custom exceptions used by the application.
- **Program.cs:** Provides the main menu and selects which phase to run.

## Storage Design

### JSON Files

The application stores its data in the `Data` folder using the following JSON files:

| File | Purpose |
|---|---|
| `coupons.json` | Stores coupon information. |
| `customers.json` | Stores customer information. |
| `email_retry.json` | Stores emails waiting for retry. |
| `emails.json` | Stores email records. |
| `incoming_orders.json` | Stores incoming orders before processing. |
| `orders.json` | Stores processed orders. |
| `payments.json` | Stores payment transactions. |
| `products.json` | Stores product information. |

All files use JSON format for persistent data storage.


### JSON Format

The application stores each collection as a JSON array of objects.

The serializer uses these settings:

- **Indented JSON:** Makes files easier to read.
- **Camel Case:** Property names are written in camelCase.
- **String Enums:** Enum values are stored as strings instead of numbers.

### Thread Safety and Data Integrity

The `JsonFileStore` protects files from concurrent access using `ConcurrentDictionary<string, SemaphoreSlim>`.

- Each file gets its own `SemaphoreSlim` lock.
- The lock allows only one operation on the same file at a time.
- Both synchronous and asynchronous read, write, and update operations use the same locking mechanism.
- `UpdateAsync` and `Update` keep the read-modify-write operation under one lock to prevent concurrent updates from overwriting each other.
- Files are written to a temporary file first. After writing is complete, `File.Move` replaces the original file.
- Temporary files are deleted in the `finally` block if they still exist.

This design protects the JSON files from concurrent access and helps prevent incomplete writes and lost updates.

### Phase 1 — Sync vs Async Performance

| Metric | Sync | Async |
|---|---:|---:|
| Total Orders | 20 | 20 |
| Validated | 15 | 15 |
| Rejected | 5 | 5 |
| Execution Time | 46.8 sec | 69.4 sec |
| Distinct Threads | 1 | 3 |
| ThreadPool.ThreadCount | 2 | 4 |

Both versions produced the same validation results.

The Async version took longer because orders were validated sequentially. However, execution continued across different Threads after awaiting asynchronous operations.

### Phase 2 — Concurrent Processing

| Metric | Result |
|---|---:|
| Total Orders Saved | 20 |
| Completed Tasks | 14 |
| Faulted Tasks | 1 |
| Processing Time | 11.42 sec |

All 20 orders were saved successfully.

### Phase 2 — Generated Report

| Metric | Result |
|---|---:|
| Total Orders | 20 |
| Completed | 13 |
| Rejected | 6 |
| Failed | 1 |
| Payment Failed | 0 |
| Total Revenue | 5966.45 |
| Average Order Value | 458.96 |
| Top Selling Product | USB-C Hub |
| Top Selling Quantity | 5 |

### Phase 2 — Sequential vs Parallel Pricing

| Metric | Sequential | Parallel |
|---|---:|---:|
| Execution Time | 6.75 sec | 4.41 sec |

Parallel pricing was approximately 1.53x faster than sequential pricing.

### Order #1 Pricing Result

| Item | Value |
|---|---:|
| SubTotal | 1225.00 |
| Discount | 245.00 |
| ShippingCost | 0.00 |
| TotalAmount | 980.00 |

### Phase 3 — Sync vs Async Comparison

| Metric | Async (10 Tasks) | Sync (10 Threads) |
|---|---:|---:|
| Execution Time | 16.636 sec | 19.585 sec |
| Observed Threads | 6 | 10 |
| Total Orders | 10 | 10 |
| Completed | 6 | 7 |
| Rejected | 2 | 2 |
| Failed | 0 | 0 |
| Payment Failed | 2 | 1 |
| ThreadPool.ThreadCount | 7 | 3 |

The Async version was approximately 1.18x faster than the Sync version.

The results were not identical because Order #5 failed with a payment timeout in the Async run but completed in the Sync run.

### Phase 3 — Sync vs Async Comparison

| Metric | Async | Sync |
|---|---:|---:|
| Execution Time | 13.252 sec | 16.197 sec |
| Observed Threads | 4 | 10 |
| ThreadPool.ThreadCount | 7 | 4 |
| Total Orders | 10 | 10 |
| Completed | 8 | 8 |
| Rejected | 2 | 2 |
| Failed | 0 | 0 |
| Payment Failed | 0 | 0 |

Both versions produced the expected results: 8 completed orders and 2 rejected orders (#4 and #9).

The Async version was approximately 1.22x faster than the Sync version.

The Sync version used 10 dedicated Threads, while the Async version observed 4 different Threads during execution.

### Phase 4 — Live Checkout

| Metric | Result |
|---|---:|
| Total Orders | 20 |
| Completed | 11 |
| Rejected | 6 |
| Payment Failed | 2 |
| Failed | 1 |
| Pending | 0 |
| Validated | 0 |

### Phase 4 — Fraud Scoring

| Metric | Sequential | Parallel |
|---|---:|---:|
| Execution Time | 18.144 sec | 5.634 sec |
| Distinct Threads Used | — | 5 |

- Speed-up: 3.22x
- Processor Count: 4
- Flagged Orders: 1005
- Results Match: Yes

### Phase 4 — Email Re-send

| Metric | Result |
|---|---:|
| Emails Sent | 30 |
| Highest Concurrent Sends | 4 |
| Total Time | 8.627 sec |

### Phase 4 — Shipping Quotes

| Order | Winning Carrier | Price | Time |
|---|---|---:|---:|
| #1 | FastShip | 0.00 | 716 ms |
| #2 | NileCourier | 8.03 | 1019 ms |
| #3 | NileCourier | 11.77 | 1095 ms |
| #4 | FastShip | 7.10 | 1010 ms |
| #5 | CairoExpress | 15.68 | 1104 ms |
| #6 | All carriers failed | — | — |

Caller cancellation was tested successfully.
Losing carrier requests were cancelled after a winner was found.

### Phase 4 — Customer Dashboard

| Customer | Result | Execution Time |
|---|---|---:|
| #3 | Loaded successfully | 1421 ms |
| #99 | Failed: Customer not found and Payments store timeout | 1284 ms |

Customer #3 dashboard contained 2 orders, 2 payments, and 1 email.

### Screenshots

The `Screenshots/` folder contains console screenshots of selected results from this project. It is organized into four subfolders, one for each phase:

- `Phase 1/`
- `Phase 2/`
- `Phase 3/`
- `Phase 4/`

Each folder contains screenshots demonstrating some of the results and behaviors covered in its corresponding phase.

## Problems Encountered and Solutions

1. **Async was slower than Sync in Phase 1:** Async operations were still sequential, so I learned that `async/await` does not automatically improve performance.

2. **Faulted Tasks in Phase 2:** One task failed while others succeeded. I handled task results and statuses to identify failures.

3. **Fire-and-Forget in Phase 3:** The application could exit before background work finished. I compared awaited and non-awaited execution.

4. **Background Exceptions:** Exceptions from fire-and-forget operations might go unobserved. I highlighted the importance of handling exceptions inside background operations.

## Answers to Assignment Questions
## Phase 1 Questions

### 1. Why are the sync and async times almost the same?

Both process orders one by one. Async does not process orders at the same time.

### 2. Does the thread ID always stay the same after await?

No. The program may continue on a different thread. My logs show different thread IDs.

### 3. What happens during Task.Delay() and Thread.Sleep()?

Task.Delay() frees the thread. Thread.Sleep() blocks the thread.

### 4. Why return Task<T> instead of T?

It allows asynchronous work without blocking the thread while waiting for I/O.

### 5. What is the difference between File.ReadAllText and File.ReadAllTextAsync?

File.ReadAllText() blocks the thread while reading. File.ReadAllTextAsync() allows the thread to do other work while waiting for the file operation.

## Phase 2 Questions

**1. What is the difference between starting a task and awaiting it?**  
Starting a task begins its work. await waits for it to finish without blocking the thread.

**2. Does every Task create a new thread? Does Task.Delay() use a thread while waiting?**  
No. Task does not always create a new thread. Task.Delay() does not need a thread while waiting.

**3. What happens if you use .Result or .Wait() instead of await? Why is it dangerous?**  
They block the current thread. This can cause deadlocks and waste threads.

**4. What happens to an exception inside a task that is never awaited?**  
The exception may go unnoticed by the code.

**5. When should you use Task.Run(), and when should you avoid it?**  
Use it for CPU-heavy work. Avoid it for normal async I/O, like database calls.

**6. What is the difference between Task and Task<T>?**  
Task has no result. Task<T> returns a result of type T.

**7. Why can't a lock block contain await?**  
Because lock must be released by the same thread, but await may continue on another thread.

**8. Why did the unprotected stock reservation oversell, and how did you prevent it?**  
Both orders read the same stock before updating it. I used SemaphoreSlim inside JsonFileStore to protect the update.

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

### 1. When is await necessary?

Use await when the current method needs to wait for an asynchronous operation to finish, when the next step depends on it, or when we need to handle its exceptions.

### 2. When is fire-and-forget acceptable?

It may be acceptable for independent, non-critical tasks. However, important operations such as sending order confirmations should be monitored and should not be ignored.

### 3. Why is async void dangerous?

An async void method cannot be awaited. The caller cannot track when it finishes, and exceptions cannot be handled through the normal await flow.

### 4. Why is async void acceptable for event handlers?

Some event handlers require a void return type, such as EventHandler. Therefore, async void is used when an event handler needs asynchronous operations. Exceptions should be handled inside the event handler.

### Actual Experiment Results

| Metric | Without await | With await |
|---|---|---|
| Method waits for email | No | Yes |
| Email completed before method returns | No | Yes, if successful |
| Exception propagated to caller | No, not through normal await flow | Yes |
| "Order processing finished" after email | No guarantee | Yes, if successful |
| Application may exit before email completes | Yes | No, while awaiting |

**Experiment observations:**

- Without await, the method printed Order processing finished while the email was still running.
- With await, the method waited until the email operation completed.
- When the awaited email operation failed, its exception propagated to the caller.
- Without await, the application exited before the email operation reached the failure point in our test.

## Phase 4 Questions

### 1. Which tool did you use in Tasks 10.1–10.4, and why not start and await one by one?

- **10.1:** Task.WhenEach to process orders as they finish.
- **10.2:** Task.WhenAll to load data at the same time.
- **10.3:** Task.WhenAny to get the first successful shipping quote.
- **10.4 A:** Parallel.ForEach to use multiple CPU cores.
- **10.4 B:** Parallel.ForEachAsync to send emails with a limit of 4.

Starting and awaiting one by one makes each operation wait for the previous one. This is slower and does not meet the concurrency requirements.

### 2. In 10.2, how do you get every exception? What does a plain await give you?

Use Task.WhenAll and inspect each task's Exception. A plain await throws one exception.

### 3. What happens to losing carriers in 10.3 if you never cancel them?

They keep running and waste time and resources, even after a winner is found.

### 4. Why does 10.4 A get faster with more CPU cores, while 10.4 B does not depend on the number of cores?

A is CPU-bound, so more cores can do more work at the same time.

B is I/O-bound. It spends most of its time waiting for emails to send, not using the CPU.

### 5. What is the difference between parallel work (10.4 A) and concurrent asynchronous work (10.1)?

Parallel work uses multiple CPU cores to calculate at the same time.

Concurrent asynchronous work handles multiple operations while they wait for I/O.

### 6. Show a cancelled run (--cancel-after) and explain what happened to the orders, stock and payments.

The cancelled run stopped processing when the cancellation token was triggered.

- **Orders:** Some orders were completed, while the remaining orders were not processed.
- **Stock:** Stock changes from completed operations remained. Unprocessed orders did not reserve stock.
- **Payments:** Payments already completed remained. Operations cancelled before payment did not create successful payments.

Cancellation does not automatically undo completed operations.