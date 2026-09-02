# Catching a bad parameter yourself

Work in this lab's own folder, under `practice/`. **Every path below is relative to
it**, and nothing here touches the work-order application.

```bash
dotnet run --project Practice.AppHost
```

> **Builds on:** *Where Parameters Come From.* You have a work-orders endpoint whose
> parameters bind from the query string, and you finished it by writing down that
> binding failures come back as a bare `400` with no `problem+json` body — an exception
> to your own error contract that happens before any of your code runs.

**This lab is optional and it is about half an hour.** It answers the question that one
ended on: *if you wanted your errors to be consistent, where would you make the change
and what would it cost?* There are two answers. One of them is a line you would expect
to work, and it does not work where it matters.

## What we're building

Someone asks for a page of work orders and types the page number wrong. They should get
an error that says so, in the same shape as every other error this service returns.

That's it.

Read it again and notice what it does **not** say. It does not say the request should be
rejected — it says the caller should be *told something useful*. Those are different, and
the difference is the whole lab.

## The venue

Open `venues/http.md` and read the entry you added at the end of the last lab, the one
about binding failures. It ends like this:

> If uniformity matters more than the default, it is fixable — but it is a decision
> somebody has to make rather than something that is already true.

You are about to make it, twice, two different ways.

Note the other thing that entry says: **`?page=abc` and a missing `page` both produce the
same `400`.** Hold on to that. It will turn out to matter more than it looks.

You'll be adding to `venues/` later in this lab.

## The roles

- **get a page number out of the request**, if there is one
- **notice when what arrived cannot be one**
- **tell the caller, in the shape this service always uses**
- **decide what "they didn't send one" means**, which is not the same question

Four parts. The fourth is not on the list by accident. Right now `int page` is required
because of its type, and nobody decided that — it is what `int` means. Whether an absent
page number is an error or just means *the first page* is a product question, and the
type system answered it without being asked.

Notice what is not on the list: nothing about validating that the page exists, nothing
about what to do with `page=0` or `page=-4`. Real, not today.

## Candidate one: take the decision back

The binder produces that `400` because parsing failed. If parsing never fails, the binder
has nothing to reject, and the value arrives in your handler where you can decide.

Create **`Orders/PageNumber.cs`**:

```csharp
namespace Orders;

public record PageNumber
{
    public int? Value { get; init; }

    // Always returns true, on purpose. See the note below - this is the mechanism.
    public static bool TryParse(string? value, out PageNumber result)
    {
        result = int.TryParse(value, out var parsed)
            ? new PageNumber { Value = parsed }
            : new PageNumber { Value = null };
        return true;
    }
}
```

Minimal APIs will bind any type that offers a static `TryParse`. There is nothing to
register.

Now change the endpoint in **`Orders/Program.cs`**. Note that `page` is now nullable and
has no default:

```csharp
app.MapGet("/work-orders", (PageNumber? page, string? department, WorkOrders orders) =>
{
    if (page is not null && !page.Value.HasValue)
    {
        return Results.Problem(
            title: "Not a page number",
            detail: "The page parameter has to be a whole number.",
            statusCode: 400);
    }

    var results = orders.Page(page?.Value ?? 1, department);
    return Results.Ok(results);
});
```

## Try all four

```bash
curl -s -o /dev/null -w '%{http_code}\n' "http://localhost:5181/work-orders"
curl -s "http://localhost:5181/work-orders?page=2" | jq -c '[.[].id]'
curl -i "http://localhost:5181/work-orders?page=abc"
curl -s -o /dev/null -w '%{http_code}\n' "http://localhost:5181/work-orders?page="
```

<details>
<summary>What you should see</summary>

**No page: `200`**, and the first page of results. You decided that, one line ago —
`page?.Value ?? 1`. It is no longer an error to leave it out.

**`page=2`: `200`**, second page. Unchanged.

**`page=abc`: `400`**, and this time:

```
Content-Type: application/problem+json
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Not a page number",
  "status": 400,
  "detail": "The page parameter has to be a whole number."
}
```

Your error contract, your words, your shape.

**`page=`: `400`**, the same one. That is the empty-string case from the last lab, and it
is now caught rather than silently accepted.

</details>

## Two nullables, two questions

Look again at what you wrote, because it is doing something easy to miss.

`page` is nullable, and `page.Value` is nullable, and **they mean different things**:

| | |
|---|---|
| `page is null` | there was no `page` in the query string at all |
| `page.Value is null` | there was one, and it was not a number |
| `page.Value.HasValue` | there was one and it was fine |

The framework could not tell you those apart. Both produced the same `400`, which is
exactly what your venue note recorded and why the note was worth writing.

`TryParse` is only called when the parameter is actually present. Absent means the binder
never runs it and the parameter keeps its null. That is where the third state comes from,
and it is free.

## Break it on purpose

Change one word. In **`Orders/PageNumber.cs`**, make the failure path honest:

```csharp
result = null!;
return false;      // was: return true, with a null Value
```

**Write down what you expect** from `?page=abc`, then rebuild and try it.

<details>
<summary>What actually happens</summary>

**The bare `400` is back.** No `problem+json`, no title, no detail — the exception dump
in Development and an empty body in Production, exactly as it was before you started.

`false` means *this could not be parsed*, and the binder does what it has always done
with that: rejects the request before your handler exists.

So the `return true` was not a detail. **It is the entire mechanism.** It is how you take
the decision away from the binder, and it works by telling the binder something that is
not quite true — a method called `TryParse` that never fails to parse.

Put it back to `return true`.

</details>

## The half of that `true` you can't see

Here's the part that matters.

You did not remove a failure. You moved it, and you moved it into code you now have to
remember to write.

Every handler that takes a `PageNumber` is obliged to check `Value.HasValue`. One that
forgets does not get an error, or a warning, or a build failure. It gets `page?.Value ??
1`, treats `abc` as *no page supplied*, and returns `200` and the first page of results
to a caller who asked for something else.

That is the same shape as the missing `public` on a validated record, and the same shape
as a `202` for a message nobody received: **a thing that looks handled, is not, and says
nothing.** You have bought a better error message with an obligation that no tool
enforces.

Worth being deliberate about, rather than discovering later.

## Candidate two: the line you would expect to work

There is a much shorter answer to the same question, and most people reach for it first.

Put `page` back to a plain `int` for a moment — the version from the last lab:

```csharp
app.MapGet("/work-orders", (int page, string? department, WorkOrders orders) =>
{
    var results = orders.Page(page, department);
    return Results.Ok(results);
});
```

Then add one line to **`Orders/Program.cs`**, before `builder.Build()`:

```csharp
builder.Services.AddProblemDetails();
```

**Write down what you expect** from `?page=abc`, then try it:

```bash
curl -i "http://localhost:5181/work-orders?page=abc"
```

<details>
<summary>What you should see, and it is what you wanted</summary>

```
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
```

One line. The framework's own binding failure now comes back in your error contract,
with no custom type, no handler check, and no obligation on anybody.

That looks like the better answer, and you should be suspicious of how easy it was.

</details>

## Now run it the way it will actually run

Stop the AppHost. Start the service on its own, as production:

```bash
ASPNETCORE_ENVIRONMENT=Production dotnet run --project Orders --no-launch-profile --urls http://localhost:5181
```

```bash
curl -i "http://localhost:5181/work-orders?page=abc"
```

**Predict before you run it.** You changed no code.

<details>
<summary>What actually happens</summary>

```
HTTP/1.1 400 Bad Request
Content-Length: 0
```

**No `problem+json`. No content type at all.** The fix that worked a minute ago does
nothing here.

`AddProblemDetails()` registers a service that turns an error into a problem-details
body. It does not put anything in the pipeline that catches this failure. In Development
something else was doing that — the developer exception page, which is happy to format
itself as `problem+json` when that service is available and the caller is not a browser.
In Production there is no developer exception page, so nothing catches the failure and
nothing formats it.

It needs both halves. Add the middleware, in **`Orders/Program.cs`**, straight after
`var app = builder.Build();`:

```csharp
app.UseExceptionHandler();
app.UseStatusCodePages();
```

Run as Production again and it is `application/problem+json`.

</details>

This is worth more than the technique. **A fix that is real in Development and absent in
Production is worse than no fix**, because it is verified by exactly the run you were
going to do and invisible in the one that matters. Nothing warns you. Both environments
return `400`; only the body differs, and nobody reads the body of an error they expected.

## Make it work for more than pages

Back to candidate one, and one loose end from it: `PageNumber` only handles `int`. A
service accumulates optional query parameters, and writing this record again for
`decimal` and `DateOnly` is how a good idea becomes a chore.

The constraint you want is **`IParsable<T>`** — the interface that says *this type knows
how to parse itself from a string*. Not `INumber<T>`: you are not doing arithmetic, and
`IParsable` also gets you `Guid`, `DateOnly` and `TimeSpan`, which numbers would not.

Replace `Orders/PageNumber.cs` with **`Orders/OptionalQuery.cs`**:

```csharp
namespace Orders;

public record OptionalQuery<T> where T : struct, IParsable<T>
{
    public T? Value { get; init; }

    // Always true, on purpose - the binder must not reject this for us.
    public static bool TryParse(string? value, IFormatProvider? provider, out OptionalQuery<T> result)
    {
        result = T.TryParse(value, provider, out var parsed)
            ? new OptionalQuery<T> { Value = parsed }
            : new OptionalQuery<T> { Value = null };
        return true;
    }
}
```

And in **`Orders/Program.cs`**, the parameter becomes `OptionalQuery<int>? page`. Nothing
else in the handler changes.

Two things worth noticing. The `TryParse` now takes an `IFormatProvider` — that is the
overload `IParsable<T>` gives you, and minimal APIs bind against it just as happily. And
`T.TryParse` is a **static abstract** call on a type parameter, which is what makes any
of this possible; before that language feature this generic could not have been written.

Confirm all four cases still behave, then move on.

## Write the venue note

Open `venues/http.md` and add this beneath the binding entry from last time:

```md
## Making binding failures fit our error contract: two ways, and one trap

**The role:** reject a malformed request *and* report it in the shape callers expect.

**How we cast it, globally:** `AddProblemDetails()` **plus** `UseExceptionHandler()` and
`UseStatusCodePages()`. All three. `AddProblemDetails()` on its own appears to work in
Development, because the developer exception page formats itself as problem+json when
that service is registered. In Production there is no such page, nothing catches the
failure, and the response is a bare `400` with no body — the thing we were trying to fix.

**How we cast it, per parameter:** a type with a static `TryParse` that always returns
`true`, so the binder cannot reject the request and the handler decides. This is the only
option that can tell *absent* from *unparseable* — the parameter's nullability carries the
first, the inner value's carries the second — and the only one that can put a
domain-specific message in the response.

What it costs: every handler taking such a type **must** check the inner value. One that
forgets returns `200` and treats bad input as absence, silently. Nothing enforces it.

Generic over `IParsable<T>`, not `INumber<T>` — the requirement is parsing, not
arithmetic, and `IParsable` covers `Guid`, `DateOnly` and `TimeSpan` too.

Neither replaces the other. The global one fixes the shape of every framework error at
once. The per-parameter one is the only way to change what an error *says*, or to decide
that a missing value is not an error at all.
```

## Last two questions

**One.** Making `page` optional changed behaviour nobody asked you to change: a request
with no page number used to be a `400` and is now the first page.

Is that better? Say who benefits, who could be harmed, and how a caller who was relying
on the old behaviour would find out.

**Two.** The developer exception page made a broken fix look like a working one. That is
not unique to problem details.

Name one other thing you rely on that behaves differently in Development, and say how you
would find out if it were wrong — without deploying to production to check.
