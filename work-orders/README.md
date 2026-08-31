# work-orders

The work-order system for Streets & Public Works. Four intake channels, one of which
is automated.

```bash
dotnet run --project WorkOrders.AppHost
```

Starts everything: the API, two subscriber services, Postgres, and NATS. The Aspire
dashboard opens; `api` is on <http://localhost:5171>.

| | |
|---|---|
| `WorkOrders.Api` | Intake, work orders, dispatch. Most of it |
| `WorkOrders.Routing` | Routes work to a department. Subscribes to nothing yet |
| `WorkOrders.Notifications` | Tells residents things. Does nothing yet |
| `WorkOrders.Contracts` | Types that cross a boundary |
| `WorkOrders.AppHost` | Starts all of the above plus Postgres and NATS |
| `WorkOrders.ServiceDefaults` | Shared configuration, health, telemetry |

**Read `venues/` before the source.** It is short, and it is the part you could not
have guessed.

## The four channels

| Channel | State |
|---|---|
| Website form | Works. `POST /intake/website-form` |
| Shared mailbox | A background adapter polls it. `POST /intake/shared-mailbox/deliver` stands in for a message arriving |
| Phone | Taken down by hand at Village Hall and typed in later. No endpoint |
| Clipboard | Ted writes them in the field. No endpoint |

## Where to start reading

`WorkOrders.Api/Endpoints.cs` is the whole HTTP surface, and it is short.
`WorkOrders.Api/WorkOrder.cs` is the only document type.

If you want to know how a work order comes into existence, those two files are the
answer and there is no third place.
