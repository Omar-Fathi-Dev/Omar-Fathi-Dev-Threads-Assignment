# Advanced C# — Async, Threads & Thread Safety

## Overview

This repository contains the assignments and mini project completed during the Advanced C# section of the .NET Diploma at Simulation Academy.

The main goal was to understand asynchronous programming, multithreading, thread safety, synchronization, and parallel processing through practical scenarios.

The repository contains five individual assignments, one complete Order Processing System mini project, and LinkedIn posts covering selected topics.

## Repository Structure

```text
(repository root)
│
├── README.md
│
├── 01-InvoiceExport/
│   ├── InvoiceExport/
│   └── ANSWERS.md
│
├── 02-TableBooking/
│   ├── TableBooking/
│   └── ANSWERS.md
│
├── 03-LockingResearch/
│   └── ANSWERS.md
│
├── 04-MenuViews/
│   ├── MenuViews/
│   └── ANSWERS.md
│
├── 05-ViewsAndEmails/
│   ├── ViewsAndEmails/
│   └── ANSWERS.md
│
├── 06-OrderProcessing/
│   ├── OrderProcessing/
│   ├── sample-output/
│   ├── screenshots/
│   └── README.md
│
└── LinkedIn/
    └── linked.md
```

## Assignments

### 01 — Invoice Export

**Main topic:** Asynchronous File and Database Operations.

SwiftBite runs a nightly invoice export. For each pending order, the application loads the order, reads the customer profile, and writes an invoice file.

**Main objective:**
- Identify performance problems when processing many orders.
- Improve the implementation using asynchronous operations.
- Preserve the original business flow and invoice content.
- Produce the same five invoices.

**Results:**

| Metric | Before | After |
|---|---:|---:|
| Execution Time | 6.6 seconds | 0.9 seconds |
| Invoices Produced | 5 | 5 |

See `01-InvoiceExport/ANSWERS.md` for the problems, changes, and before/after output.

### 02 — Table Booking

**Main topic:** Race Conditions and Thread Safety.

Multiple customers attempt to book a limited number of tables simultaneously.

**Problem observed:**

The original implementation allowed multiple threads to update the shared booking data at the same time, resulting in incorrect totals and overbooking.

**Sample results before the fix:**

| Run | Booked | Tables Left |
|---|---:|---:|
| 1 | 9 | -3 |
| 2 | 10 | -5 |
| 3 | 34 | -33 |

**Main objective:**
- Identify the race condition.
- Protect shared booking data.
- Ensure that concurrent bookings produce correct totals.

See `02-TableBooking/ANSWERS.md` for the investigation and solution.

### 03 — Locking Research

**Main topic:** `lock` and `Monitor`.

This assignment focused on understanding synchronization mechanisms in .NET through research and code examples.

Topics covered:
- What `System.Threading.Monitor` is used for.
- How the C# `lock` statement works.
- Differences between `lock` and direct `Monitor` APIs.
- `Monitor.TryEnter`, `Monitor.Pulse`, and `Monitor.Wait`.
- Implementing the same critical section using `lock` and `Monitor.Enter` / `Monitor.Exit`.

See `03-LockingResearch/ANSWERS.md` for all five answers and research links.

### 04 — Menu Views

**Main topic:** Atomic Operations and Thread Safety.

Multiple request threads register views for the same menu item.

**Main objective:**
- Identify why the expected and actual view counts differ.
- Fix the shared counter update.
- Choose an appropriate synchronization tool for a single counter.
- Understand when a different synchronization mechanism is needed.

See `04-MenuViews/ANSWERS.md` for the investigation and implementation details.

### 05 — Views and Emails

**Main topic:** Thread-Safe Collections and Concurrent Business Operations.

The application uses:
- A `Dictionary` to store product page view counts.
- A `Queue` to store outgoing email jobs.
- Two background workers to process email jobs.

**Main objective:**
- Fix concurrent access to the view-count collection.
- Ensure email jobs are processed correctly by multiple workers.
- Prevent lost updates and collection-related exceptions.
- Understand why thread-safe collections alone do not guarantee that business operations are correct.

See `05-ViewsAndEmails/ANSWERS.md` for the problems and solutions.

### 06 — Order Processing System (Mini Project)

**Main topic:** Async Programming, Threads, Thread Safety, and Parallel Processing.

A complete e-commerce order processing system using JSON file storage instead of a real database or external APIs.

Each order goes through validation, pricing, payment, persistence, and email confirmation.

The mini project is divided into four phases:

| Phase | Main Focus |
|---|---|
| Phase 1 | Sequential Sync vs Async Processing |
| Phase 2 | Concurrent Order Processing and Task Handling |
| Phase 3 | Thread Safety, Payment Handling, Email Retry, and Sync vs Async |
| Phase 4 | Advanced Concurrency, Parallel Processing, and Performance Comparisons |

**Additional implementation details:**
- JSON-based file storage.
- Manual Dependency Injection.
- Custom exceptions.
- Cancellation support.
- `SemaphoreSlim` for protecting shared operations.
- Performance measurements using `Stopwatch`.
- Console menu for running individual phases.

The `sample-output/` folder contains the final JSON output files, while `screenshots/` contains selected console results.

See `06-OrderProcessing/README.md` for the complete implementation details, performance tables, screenshots, and answers to all mini project questions.

## LinkedIn Posts

The following posts explain selected concepts learned during the assignments:

| Topic | LinkedIn Post |
|---|---|
| Thread vs Task | [Read post](https://lnkd.in/p/d8mDaqSH) |
| SemaphoreSlim | [Read post](https://lnkd.in/p/dWUEk-ac) |
| Interlocked vs lock | [Read post](https://lnkd.in/p/dATrA-tg) |

See `LinkedIn/linked.md` for the post links and topic numbers.

## Key Concepts Learned

Throughout these assignments, I practiced:

- Asynchronous programming with `async` and `await`.
- Understanding `Task` and `Thread`.
- Race conditions and shared-state protection.
- `lock`, `Monitor`, and `SemaphoreSlim`.
- Atomic operations and `Interlocked`.
- Thread-safe collections.
- Exception handling in asynchronous operations.
- Cancellation with `CancellationToken`.
- Concurrent and parallel processing.
- Performance measurement and comparison.

## Final Notes

Each assignment includes its own `ANSWERS.md` file containing the investigation, explanations, and solutions where applicable.

The mini project has a separate README with its complete technical documentation and execution results.