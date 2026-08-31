# practice

Two services under an Aspire AppHost.

```bash
dotnet run --project Practice.AppHost
```

| | |
|---|---|
| `Orders` | Yours. This is where lab work happens |
| `Directory` | Somebody else's. Read it; do not change it |
| `Practice.AppHost` | Starts both and wires them together |
| `Practice.ServiceDefaults` | Shared configuration. Worth reading once, and then again when something surprises you |

`orders` is on <http://localhost:5181>, `directory` on <http://localhost:5182>.

Read `venues/` before the source. It is short and it is the part you could not have
guessed.
