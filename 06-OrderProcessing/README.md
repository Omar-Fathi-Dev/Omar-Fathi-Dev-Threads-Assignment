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