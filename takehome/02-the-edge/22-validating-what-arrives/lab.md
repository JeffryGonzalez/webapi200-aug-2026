# Validating what arrives

Work in this lab's own folder, under `practice/`. **Every path below is relative to
it**, and nothing here touches the work-order application.

```bash
dotnet run --project Practice.AppHost
```

> **Builds on:** *Where parameters come from.* You have an endpoint whose parameters
> bind from the request, and you have seen that binding rejects some bad input and
> cheerfully accepts other bad input.

## What we're building

Dispatchers can create a work order. A work order needs a department code and a
description. A blank description is not a work order.

That's it.

Read it again and notice what it does **not** say. It says nothing about attributes,
filters, or where the check lives. It says what has to be true of the data. Everything
else is our problem.

## The roles

- **describe what a valid work order looks like**, once, somewhere
- **check an incoming one against that**, before any work happens
- **tell the caller what was wrong**, specifically enough to fix it
- **do the work, if it was fine**

Four parts. Notice what's not on the list: nothing about *where* the check runs, and
nothing about what happens if the description is merely stupid rather than absent. We
are checking shape, not judgement.

## Build it

Create `Orders/NewWorkOrder.cs`. Note the `public` — it matters more than you would
guess, and not for the reason you would guess:

```csharp
using System.ComponentModel.DataAnnotations;

namespace Orders;

public record NewWorkOrder
{
    [Required, MinLength(3)]
    public string Department { get; init; } = "";

    [Required, MinLength(10)]
    public string Description { get; init; } = "";
}
```

Turn validation on in `Orders/Program.cs`, before `builder.Build()`:

```csharp
builder.Services.AddValidation();
```

And an endpoint:

```csharp
app.MapPost("/work-orders", (NewWorkOrder order, WorkOrders orders) =>
{
    var created = orders.Add(order.Department, order.Description);
    return Results.Created($"/work-orders/{created.Id}", created);
});
```

Nothing else. No package to install, no filter to register, no MSBuild property.

## Try it

```bash
curl -i -X POST "http://localhost:5181/work-orders" \
  -H 'content-type: application/json' \
  -d '{"department":"STR","description":"Pothole on Depot St, eastbound lane"}'

curl -i -X POST "http://localhost:5181/work-orders" \
  -H 'content-type: application/json' \
  -d '{"department":"S","description":"pothole"}'
```

<details>
<summary>What you should see</summary>

The first returns `201`.

The second returns `400` with a body naming both fields and both rules:

```json
{
  "title": "One or more validation errors occurred.",
  "errors": {
    "Department": ["The field Department must be a string or array type with a minimum length of '3'."],
    "Description": ["The field Description must be a string or array type with a minimum length of '10'."]
  }
}
```

**Both** errors, not just the first. A caller fixing their request gets one round trip
rather than three.

</details>

## Break it on purpose

Now do something that looks harmless.

Delete the word `public` from the record. Nothing else — same file, same attributes,
same everything:

```csharp
record NewWorkOrder      // was: public record NewWorkOrder
```

It is a type only this project uses. Removing a modifier nobody needs is tidying up.

**Write down what you expect** before you run it. Then rebuild and send the invalid
request again.

<details>
<summary>What actually happens</summary>

**`201 Created`.** The invalid work order was accepted.

No error. No warning. It compiled, it ran, and validation simply did not happen. The
attributes are still there, `AddValidation()` is still called, and nothing anywhere
tells you that a check you thought you had is gone.

Put `public` back and confirm the `400` returns.

</details>

## The half of that call you can't see

Here's the part that matters.

`AddValidation()` looks like it registers a runtime service. Mostly, it doesn't. It is
a signal to a **source generator**, which runs at compile time, walks your code looking
for endpoints and the types they bind, and writes the validation code for you.

A generator can only write code that can *see* the type it is validating. The
generated validation lives in its own compilation unit, so an `internal` type is
invisible to it — and a record with no accessibility modifier is `internal`. The
generator finds nothing, writes nothing, and says nothing.

Two consequences worth having straight:

- **The failure mode is silence.** Not an exception, not a startup error, not a build
  warning. The endpoint works, accepts anything, and looks exactly like the one that
  validates. This is the same shape as the missing field you chased earlier — a thing
  that looks like a guarantee, is
  not enforced at runtime, and fails quietly rather than loudly.
- **An accessibility modifier is now a functional decision.** `public` on a type only
  one project uses looks like noise, and removing noise is what careful people do. Here
  it silently removes a validation rule.

## The other thing that just happened

Look again at the `400` body from the working version.

```
Content-Type: application/json
```

Not `application/problem+json`. `venues/http.md` says every error in this codebase is
problem details, and this is the second place that turns out not to be true — the first
was a binding failure, in the previous lab.

Neither one is a bug. Both are defaults somebody else chose, arriving in an API whose
error contract you thought you controlled.

## Write the venue note

Open `venues/http.md` and add this beneath the binding entry you added last time:

```md
## Validation is compile-time discovery, and its output is not problem+json

**The role:** something has to reject a request whose shape is wrong, and say what was
wrong with it.

**How we cast it:** `AddValidation()` plus DataAnnotations attributes on the bound
type. Stock .NET 10 — no package, no MSBuild property, no endpoint filter.

**Validated types must be `public`.** `AddValidation()` drives a source generator, and
the generated code cannot see an `internal` type. A record declared without a modifier
— which is what most people write for a type only one project uses — is silently
unvalidated: no error, no warning, and an endpoint that accepts anything while looking
identical to one that does not.

Worth knowing because the failure is invisible in code review. The attributes are right
there, and the missing word is one nobody looks for.

Also: validation failures return `application/json` with an `errors` dictionary, not
`application/problem+json`. That is the second exception to the rule above, after
binding failures, and both come from defaults rather than from a decision anyone made
here.
```

## Last two questions

**One.** You now know two ways a request can be rejected before your handler runs —
binding and validation — and they return different shapes, neither of them
`problem+json`. If you wanted all three to be consistent, where would you make that
change, and what would it cost?

**Two.** `[MinLength(10)]` on a description is a rule about *shape*. "The description
must not name a resident" is a rule about *content*, and no attribute will express it.

Where does the second kind of rule go? Name a place, and say what makes it different
from where the first kind lives.
