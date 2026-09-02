# practice — the catalog client

One service under an Aspire AppHost, and a specification.

```bash
dotnet run --project Practice.AppHost
```

| | |
|---|---|
| `Dispatch` | Yours. This is where lab work happens |
| `catalog-openapi.yaml` | The purchasing catalog's published specification. Not ours |
| `Practice.AppHost` | Starts it |
| `Practice.ServiceDefaults` | Shared configuration |

`dispatch` is on <http://localhost:5181>.

The catalog itself is not in this solution and is not run by the AppHost. It is a real
service, running at
<https://theoria.hypertheory-labs.com/clerk-records/purchasing>, and you will call it
over the internet.

Read `venues/` before the source.
