# Where parameters come from

Work in this lab's own folder, under `practice/`. **Every path below is relative to
it**, and nothing here touches the work-order application.

```bash
dotnet run --project Practice.AppHost
```

## What we're building

An endpoint that lists work orders. A caller should be able to ask for one page of
them, optionally filtered by which department they belong to.

That's it.

Read it again and notice what it does **not** say. It says nothing about query strings,
nothing about attributes, nothing about how any of those values get from a request into
your code. It's what somebody wants. Everything else is our problem.

## The venue

Skim `venues/` again. One entry matters here:

- **We use minimal APIs.** Handlers are functions, and their parameters are the
  request.

That second sentence is the whole lab. In a controller, a request is an object you
reach into. Here, the parameters of your handler *are* the request, and something has
to decide which part each one comes from.

You'll be adding to `venues/` later.

## The roles

The same paragraph as parts that have to be filled:

- **know which page was asked for**
- **know whether a filter was supplied, and cope when it wasn't**
- **reject a request that doesn't make sense, before doing any work**
- **get the data**
- **hand it back**

Five parts. Notice what's not on the list: nothing about authentication, nothing about
rate limits, nothing about what a caller is allowed to see. All reasonable. Nobody
asked, so we're not building them.

## Build it

Add this to `Orders/Program.cs`:

```csharp
app.MapGet("/work-orders", (int page, string? department, WorkOrders orders) =>
{
    var results = orders.Page(page, department);
    return Results.Ok(results);
});
```

Run it:

```bash
curl -s "http://localhost:5181/work-orders?page=1" | jq
curl -s "http://localhost:5181/work-orders?page=1&department=STR" | jq
```

Three parameters, three different origins, and you declared none of them.

## Work out where each one came from

Before reading on, decide for yourself: for each of `page`, `department` and `orders`,
where did the value come from and what decided that?

<details>
<summary>The rules being applied</summary>

- **`orders`** is a registered service, so it comes from dependency injection. Anything
  the container knows about is resolved rather than read from the request.
- **`page`** and `department` are simple types that are not services and do not appear
  in the route pattern, so they are read from the **query string** by name.
- If `page` had appeared in the route as `/work-orders/{page}`, it would have come from
  the **route** instead — the route wins.
- A complex type that isn't a registered service would have been read from the
  **body**, and only one parameter may be.

Nothing here is configured. The rules are positional and by-type, and they are applied
in an order that mostly does what you meant.

</details>

## Break it on purpose

Three requests. **Write down what you expect** for each — status code and body —
before running any of them.

```bash
curl -i "http://localhost:5181/work-orders"
curl -i "http://localhost:5181/work-orders?page=abc"
curl -i "http://localhost:5181/work-orders?page=1&department="
```

<details>
<summary>What actually happens</summary>

**No `page` at all: `400`.** `int page` is not nullable and has no default, so it is
required, and nothing said so out loud — the type did.

**`page=abc`: `400`.** It could not be parsed as an `int`.

**`department=`: `200`.** An empty string is a value. `string?` accepted it happily,
and your filter is now looking for a department whose code is `""`.

The first two are the binder doing its job. The third is the binder doing its job too,
and it is the one that will reach production.

</details>

## The half of that 400 you can't see

Here's the part that matters.

Look at what came back in the body of the first two responses. In development you get
an exception dump. **In production you get a `400` with an empty body.**

Every failing endpoint you write returns `application/problem+json` — it's in
`venues/http.md`, it's uniform, and a caller writes one error path. Except this one.
A binding failure happens *before* your handler runs, before any filter you added,
before anything you wrote has an opinion. There is no code of yours in the path at all.

So the API has two kinds of error: the ones you produce, which are consistent, and the
ones the framework produces on your behalf, which are not — and callers meet both.

Nothing in our five roles asked about this. Role three said *reject a request that
doesn't make sense*, and it turns out something was already doing part of that, in a
shape nobody chose.

## Write the venue note

Open `venues/http.md` and add this:

```md
## Binding failures do not look like our other errors

**The role:** something has to reject a malformed request before the handler runs.

**How we cast it:** we don't. Minimal API parameter binding does it for us, and returns
a bare `400` — no `problem+json` body, and an exception dump in Development that is not
there in Production.

Worth knowing because every other error in this codebase is `problem+json` and a caller
can reasonably assume all of them are. A missing required query parameter is the
exception, it happens before any of our code runs, and it is not something a handler
can catch.

If uniformity matters more than the default, it is fixable — but it is a decision
somebody has to make rather than something that is already true.
```

## Last two questions

**One.** `department=` returned `200` and searched for an empty department code. Which
of the five roles was supposed to prevent that, and where would you put the check?
Name at least two places it could go and what each one costs.

**Two.** `int page` is required because of its type. `string? department` is optional
because of its type. Nobody wrote the words "required" or "optional" anywhere.

Is that a good thing? Argue both sides for one minute.
