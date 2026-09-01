# Add A Work Order

```sh
curl -s -X POST http://localhost:5171/work-orders/2026-0819/dispatch \
  -H 'content-type: application/json' \
  -d '{"vendor":"Rademacher Traffic Control"}'

```

```http
POST http://localhost:5171/work-orders/2026-0819/dispatch
Content-Type: application/json

{"vendor":"Rademacher Traffic Control" }
```