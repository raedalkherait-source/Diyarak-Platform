# Coding Standard

- Nullable reference types are mandatory.
- Warnings and formatting failures block CI.
- Value objects are immutable and valid by construction.
- Entities expose behavior rather than public setters.
- Exceptions represent invalid construction or unexpected failures; expected application outcomes use `Result`.
- Use `DateTimeOffset` in UTC for timestamps.
- Avoid primitive obsession where a stable business concept exists.
