
```http
GET http://localhost:5181/work-orders2?page=1&department=STR
```
GET http://localhost:5181/work-orders?page=1&department=

```http
GET http://localhost:5181/work-orders?page=1&favorite-food=tacos
```


```http
GET http://localhost:5181/work-orders2?page=abc
```


```http
POST http://localhost:5171/work-orders/2026-0819/dispatch
content-type: application/json

{
  "vendor": "Bilyeu Paving & Sealcoat"
}
```


```http
GET https://theoria.hypertheory-labs.com/clerk-records/purchasing/vendors?status=approved&page=1&pageSize=25
```