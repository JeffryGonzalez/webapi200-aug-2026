# Add A Work Order

```http
POST http://localhost:5171/intake/phone
content-type: application/json

{
  "location": "Depot St",
  "description": "caller reports a hole"
}
```


```http

POST http://localhost:5171/intake/phone
content-type: application/json

{
  "reportedBy": "Dolores Ankney",
  "location": "Depot St at the alley",
  "description": "Same hole as before, she says"
}
```


```http

POST http://localhost:5171/intake/phone
content-type: application/json

{
  "location": "N. Salyer at the culvert",
  "description": "Water standing in the road"
}
```

```http
GET http://localhost:5171/work-orders
```



```http
GET https://localhost:7012/start-work/3
```