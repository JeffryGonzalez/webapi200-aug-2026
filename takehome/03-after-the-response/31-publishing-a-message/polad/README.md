# practice — messaging

Two services and a message broker.

```bash
dotnet run --project Practice.AppHost
```

| | |
|---|---|
| `Orders` | Yours. Assigns work |
| `Crew` | Yours too. Finds out about it |
| `Practice.Contracts` | The one thing both agree on |
| `Practice.AppHost` | Starts both, and NATS |

`orders` is on <http://localhost:5191>, `crew` on <http://localhost:5192>.

Read `venues/` before the source.
