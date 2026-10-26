# Assignment 01 — Invoice Export

**Student Name:** Omar Fathy  
**Group / ID:** 3

## R2 — Problems with the Original Design

- `Thread.Sleep()` blocks the current thread while waiting.
- Reading customer files and writing invoice files use synchronous I/O.
- The `foreach` loop processes each order one by one.
- The total export time increases when the number of orders increases.

## R3 — Changes Made

- Changed the database methods to use `async` and `await`.
- Replaced `Thread.Sleep()` with `Task.Delay()` in the database methods.
- Used `File.ReadAllTextAsync()` to read customer files asynchronously.
- Used `File.WriteAllTextAsync()` to write invoice files asynchronously.
- Changed the methods to return `Task` or `Task<T>`.
- Started the export tasks first, then awaited them one by one.
- Removed the extra database call because the order data was already available.

## R4 — Results

The export time improved because the file operations for different orders could run concurrently.

- **Before:** 6.6 seconds
- **After:** 0.9 seconds
- **Result:** The export time decreased by about 86%.

The program still exports the same five invoices. The order of the console messages may change because the tasks can finish at different times.

## Conclusion

Using asynchronous file operations and concurrent tasks reduced the total export time. It also allowed the program to wait for I/O operations without blocking a thread during the wait.