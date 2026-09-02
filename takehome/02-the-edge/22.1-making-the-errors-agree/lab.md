# Making the errors agree

Work in this lab's own folder, under `practice/`. **Every path below is relative to
it**, and nothing here touches the work-order application.

```bash
dotnet run --project Practice.AppHost
```

`orders` is on <http://localhost:5181>.

> **Builds on:** *Validating What Arrives.* You have an endpoint that accepts a work
> order, validation driven by attributes and a source generator, and two venue notes
> recording that this service does not return errors in one consistent shape.

**This lab is optional and it is about forty minutes.** It is the one where you find out
that the obvious tool does not work, why, and what to do instead.

## What we're building

A caller who sends us a bad work order gets one kind of error, whatever was wrong with
it.

That's it.

Read it again and notice what it does **not** say. It says nothing about which shape
wins. That is a decision, and it is yours.

## The venue

Open `venues/http.md` and read the entries you have written across this stretch. Between
them they record that this service has **three** different error shapes, and that at
least two of them were nobody's decision.

Confirm it rather than trusting the notes. Three requests:

```bash
# 1. the body is not JSON at all
curl -i -X POST http://localhost:5181/work-orders \
  -H 'content-type: application/json' -d '{"department":'

# 2. the body is fine, the data is not
curl -i -X POST http://localhost:5181/work-orders \
  -H 'content-type: application/json' -d '{"department":"S","description":"short"}'

# 3. a query parameter that will not parse
curl -i "http://localhost:5181/work-orders?page=abc"
```

<details>
<summary>The three shapes</summary>

| Request | Status | Content type |
|---|---|---|
| malformed body | `400` | `text/plain` — an exception dump in Development, nothing in Production |
| failed validation | `400` | `application/json`, with an `errors` dictionary and no `type` or `title` |
| unparseable query parameter | `400` | `text/plain`, same as the first |

Same status code, three bodies. A caller writing one error path for your API cannot.

</details>

You'll be adding to `venues/` later in this lab.

## The roles

- **notice that a request cannot be accepted**
- **say what was wrong with it**, specifically enough to fix
- **say it the same way every time**
- **do it for the rules an attribute cannot express**, as well as the ones it can

Four parts. The third is why we are here. The fourth is not on the list by accident —
you were left with it at the end of the last lab, and it turns out to be the same job.

Notice what is not on the list: nothing about *which* shape. Consistency is the
requirement; the choice of shape is ours.

## The obvious tool

An endpoint filter runs around a handler and can inspect what it produced. That is
exactly the shape of this problem, so start there.

Add this to the `POST` endpoint in **`Orders/Program.cs`**:

```csharp
app.MapPost("/work-orders", (NewWorkOrder order, WorkOrders orders) =>
{
    var created = orders.Add(order.Department, order.Description);
    return Results.Created($"/work-orders/{created.Id}", created);
})
.AddEndpointFilter(async (ctx, next) =>
{
    Console.WriteLine("[filter] entered");
    var result = await next(ctx);
    Console.WriteLine($"[filter] saw: {result?.GetType().Name}");
    return result;
});
```

**Write down what you expect** to see in the console for a valid request and for an
invalid one. Then send both and look at `orders`' logs in the dashboard.

```bash
curl -s -o /dev/null -X POST http://localhost:5181/work-orders \
  -H 'content-type: application/json' -d '{"department":"STR","description":"Pothole on Depot St eastbound"}'

curl -s -o /dev/null -X POST http://localhost:5181/work-orders \
  -H 'content-type: application/json' -d '{"department":"S","description":"short"}'
```

<details>
<summary>What actually happens</summary>

The valid request logs both lines. **The invalid one logs nothing at all.**

Your filter did not run. Not "ran and saw the wrong thing" — never entered.

</details>

## Why it never ran

`AddValidation()` puts a filter on the endpoint too, and **it runs outside yours.** When
validation fails it returns its result immediately and never calls the next thing in the
chain, which is you.

It is worth knowing how far that goes, because the usual fix does not help. Filters
attached to a route group run outside the endpoint's own filters, so that is the natural
next thing to try:

```csharp
var group = app.MapGroup("").AddEndpointFilter(/* ... */);
```

**It also does not run.** The generated validation filter is outside that as well.

So the rule to take away is not about groups or ordering syntax:

> **A filter cannot change a decision that was made before it ran.** Wrapping something
> only works if you are on the outside of it, and here you are not.

## Take the job instead

If you cannot wrap the built-in validation, do the validation.

Create **`Orders/ValidationFilter.cs`**:

```csharp
using System.ComponentModel.DataAnnotations;

namespace Orders;

public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var candidate = context.Arguments.OfType<T>().FirstOrDefault();

        if (candidate is not null)
        {
            var results = new List<ValidationResult>();
            var ok = Validator.TryValidateObject(
                candidate, new ValidationContext(candidate), results, validateAllProperties: true);

            if (!ok)
            {
                var errors = results
                    .SelectMany(r => r.MemberNames.DefaultIfEmpty(""), (r, name) => (name, r.ErrorMessage))
                    .GroupBy(x => x.name)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage ?? "Invalid").ToArray());

                return Results.ValidationProblem(errors,
                    title: "That work order cannot be accepted",
                    statusCode: StatusCodes.Status400BadRequest);
            }
        }

        return await next(context);
    }
}
```

Note `Validator.TryValidateObject` — the same DataAnnotations attributes, checked at
runtime by you rather than at compile time by a generator. The attributes on
`NewWorkOrder` do not change.

Now replace the probe filter in **`Orders/Program.cs`** and turn the built-in one off for
this endpoint:

```csharp
})
.DisableValidation()
.AddEndpointFilter<ValidationFilter<NewWorkOrder>>();
```

Send the invalid work order again.

<details>
<summary>What you should see</summary>

```
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "That work order cannot be accepted",
  "status": 400,
  "errors": {
    "Department": ["The field Department must be a string or array type with a minimum length of '3'."],
    "Description": ["The field Description must be a string or array type with a minimum length of '10'."]
  }
}
```

Both fields still named, both rules still reported — and now `problem+json`, with a
`type`, a `title`, and a sentence a person wrote.

</details>

## The rule no attribute can express

You were left with a question at the end of the last lab: `[MinLength(10)]` is a rule
about **shape**, and *"the description must not name a resident"* is a rule about
**content**, which no attribute will express.

It goes here, and that is the point of having taken the job. Add this to
`ValidationFilter<T>`, just before `return await next(context);`:

```csharp
if (candidate is NewWorkOrder order)
{
    var residents = new[] { "Mink", "Vosmik", "Kuchenbrod", "Amankwah", "Prill" };
    var named = residents.FirstOrDefault(r =>
        order.Description.Contains(r, StringComparison.OrdinalIgnoreCase));

    if (named is not null)
    {
        return Results.ValidationProblem(
            new Dictionary<string, string[]>
            {
                ["Description"] = [$"Do not name residents in a work order description. Found: {named}."]
            },
            title: "That work order cannot be accepted",
            statusCode: StatusCodes.Status400BadRequest);
    }
}
```

```bash
curl -s -X POST http://localhost:5181/work-orders \
  -H 'content-type: application/json' \
  -d '{"department":"STR","description":"Harold Mink called again about the hole"}' | jq
```

A perfectly well-formed work order, rejected for what it says, in the same shape as one
rejected for how it is built. A caller cannot tell which kind of rule they broke, and
does not need to.

<details>
<summary>A note on that check, because it is worse than it looks</summary>

The `is NewWorkOrder` test inside a class that is generic over `T` is a smell, and you
should see it. A shape rule generalises across every type; a content rule is about one
type and belongs with that type, not in a generic filter that happens to be nearby.

A better arrangement is a second, non-generic filter for the content rules, composed
after this one. It is left as it is here so that both kinds of rule are visible in one
place while you are learning what the place is for. Do not copy this arrangement into
something real without moving the resident check out.

</details>

## The one you cannot fix here

Send a malformed body again:

```bash
curl -i -X POST http://localhost:5181/work-orders \
  -H 'content-type: application/json' -d '{"department":'
```

Still `text/plain`. Still no `problem+json`. Your filter never sees it, and no filter
ever will — **binding happens before the filter chain exists.** There is nothing to wrap.

That is the same wall as the built-in validation filter, one level further out, and it
means the three shapes need two different mechanisms rather than one:

- **Validation and content rules** are fixed by a filter, because they happen inside the
  endpoint, where filters live.
- **Binding failures** are fixed by `AddProblemDetails()` together with
  `UseExceptionHandler()` and `UseStatusCodePages()`, because they happen in the
  pipeline, before the endpoint. If you did the optional lab on bad parameters you have
  already done this and it is already consistent; if not, the venue note below records
  what is left.

Two shapes fixed, one still open, and knowing which tool reaches which is the whole
answer.

## The half of that filter you can't see

Here's the part that matters.

`.DisableValidation()` turned off a source generator that was finding validatable types
**automatically**, everywhere, for free. You replaced it with a filter you attach by hand,
one endpoint at a time.

So the next `POST` somebody adds to this service is not validated. Not because they did
anything wrong — because the thing that used to do it without being asked is off, and the
thing that replaced it has to be asked. There is no error, no warning, and the endpoint
works.

That is the third time this stretch that turning something on or off has changed
behaviour silently: a record without `public`, a fix that only works in Development, and
now a validation strategy that is opt-in instead of automatic. The tools differ; the
shape does not.

**You bought a consistent error contract with an obligation to remember.** That is a
real trade and it is often worth making. It is not free, and the cost lands on somebody
who was not in the room.

## Write the venue note

Open `venues/http.md` and add this beneath the entries from the earlier labs:

```md
## One error shape, and what it took

**The role:** tell a caller a request cannot be accepted, the same way every time.

**How we cast it:** `Results.ValidationProblem` from our own `ValidationFilter<T>`,
attached per endpoint, with `.DisableValidation()` turning off the built-in validation
for that endpoint.

**Why not just wrap the built-in one:** you cannot. Its filter runs outside anything you
attach, including route group filters, and it returns without calling the rest of the
chain. A filter cannot change a decision that was made before it ran.

**What this reaches, and what it does not.** A filter runs inside the endpoint, so it can
see validation and content rules. **Binding failures happen before any filter exists** and
are not reachable from here — those need `AddProblemDetails()` plus
`UseExceptionHandler()` and `UseStatusCodePages()`, which is a different mechanism in a
different place.

**What it cost:** validation is now opt-in. The source generator found validatable types
by itself; our filter is attached by hand, per endpoint. A new `POST` added later is
unvalidated until somebody remembers, and nothing reports it.

**Where content rules live:** the same filter, and that is deliberate — a caller should
not be able to tell whether they broke a rule about shape or a rule about meaning. Shape
rules generalise across types; content rules belong to one type and should be composed as
their own filter rather than type-tested inside a generic one.
```

## Last two questions

**One.** Validation is now opt-in and nothing reports a `POST` that forgot it.

Describe something that would catch it — not a process, a thing that fails. Say where it
would live and what would make it run.

**Two.** The resident check rejects a work order because of a name in the description.
The dispatcher on the phone typed what the caller said.

Is a `400` the right answer? Say what else could happen instead, and who should decide —
and if your answer is that somebody at the village should decide, say what you would need
to show them to get an answer.
