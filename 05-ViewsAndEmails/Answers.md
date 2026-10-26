# Assignment 05 — Views and Emails

**Student Name:** Omar Fathy  
**Group / ID:** 3

## R1 — Problems
- `Dictionary` was not safe for multiple threads.
- `Queue` could cause errors when workers accessed it together.
- `List` was not safe for multiple workers.

## R2 — Fixes
- Used `ConcurrentDictionary` and `AddOrUpdate` for views.
- Used `ConcurrentQueue` and `TryDequeue` for email jobs.
- Used `lock` when adding sent emails.

## R3 — Business Rules
Thread-safe collections do not protect all business rules. We must still protect shared data and make sure each email is sent only once.

## Test Results
- Expected views: 50,000; actual: 50,000.
- Email jobs: 20; unique sent: 20; total sends: 20.