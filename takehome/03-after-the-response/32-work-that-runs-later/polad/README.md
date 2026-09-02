# practice — work that runs later

One service. No broker, no database, nothing to provision.

```bash
dotnet run --project Practice.AppHost
```

| | |
|---|---|
| `Intake` | Yours. Takes work orders and notifies the resident |
| `Practice.AppHost` | Starts it |

`intake` is on <http://localhost:5193>.

Read `venues/` before the source.
