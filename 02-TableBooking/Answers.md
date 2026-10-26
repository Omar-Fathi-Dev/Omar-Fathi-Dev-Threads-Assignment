# Assignment 02 — Table Booking

**Student Name:** Omar Fathy  
**Group / ID:** 3

## R1 — Test Results

I ran the original program three times.

| Run | Booked | Tables Left |
|---|---:|---:|
| 1 | 9 | -3 |
| 2 | 10 | -5 |
| 3 | 34 | -33 |

The results were wrong. The program booked more tables than available.

## R2 — What Was Wrong?

- Many customers tried to book at the same time.
- More than one thread could pass the `if` condition.
- The shared values could change at the same time.
- This caused a **Race Condition**.
- The number of booked tables became greater than 5.

## R3 — The Fix

I used `SemaphoreSlim` to protect the booking code.

- `WaitAsync()` waits for permission to book.
- Only one customer can enter the protected code at a time.
- The program checks `TablesLeft` after getting permission.
- `TablesLeft` and `Booked` are updated inside the protected code.
- `finally` calls `Release()` to return the permission.

I also changed the booking method to use `async` and return a `Task`.

## R4 — Final Results

After the fix, the program produced this output:

```text
Customer-01 booked a table. Left=4
Customer-02 booked a table. Left=3
Customer-03 booked a table. Left=2
Customer-04 booked a table. Left=1
Customer-05 booked a table. Left=0

Booked=5, TablesLeft=0
```

The program booked exactly 5 tables. No extra bookings were made.

## Conclusion

`SemaphoreSlim` fixed the Race Condition. The program now keeps the booking numbers correct.