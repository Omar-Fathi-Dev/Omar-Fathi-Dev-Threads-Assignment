# Assignment 04 — Menu Views

**Student Name:** Omar Fathy  
**Group / ID:** 3

## R1 — Test Results

The expected number is:

`8 × 50,000 = 400,000`

The actual number was different from the expected number before the fix.

## R2 — What Was Wrong?

The code used `Views++`.

Many threads changed the same counter at the same time.

Some updates were lost. This is called a **Race Condition**.

## R3 — The Fix

I used `Interlocked.Increment(ref Views)`.

It safely increases the counter by one.

This prevents lost updates when many threads use the counter.

## R4 — When Do We Need Another Solution?

`Interlocked.Increment` is good for one counter.

If we need to protect many steps together, we can use `lock`.

## Final Result

The expected number is `400,000`.

After the fix, the actual number should be `400,000` on every run.