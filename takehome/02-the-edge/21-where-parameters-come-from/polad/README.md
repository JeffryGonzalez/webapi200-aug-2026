# practice — intake

One service under an Aspire AppHost.

```bash
dotnet run --project Practice.AppHost
```

| | |
|---|---|
| `Orders` | Yours. This is where lab work happens |
| `Practice.AppHost` | Starts it |
| `Practice.ServiceDefaults` | Shared configuration. Worth reading once, and then again when something surprises you |

`orders` is on <http://localhost:5181>.

There are thirteen work orders seeded in memory, across five departments, in
`Orders/WorkOrders.cs`. Read `venues/` before the source.
