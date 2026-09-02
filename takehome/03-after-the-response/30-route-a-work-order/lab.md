# Route a work order

This one is in the **work-order application**, not the practice repository. Everything
you do here stays in the village's codebase.

Docker Desktop first, then:

```bash
dotnet run --project WorkOrders.AppHost
```

The API is on <http://localhost:5171>.

> **Builds on:** *Publishing a Message*, where you sent one message between two services
> you built for practice. This is the same idea in a codebase somebody else wrote, with
> consequences.

## What we're building

When a report comes in, the resident should hear that we got it, and the right department
should get the work. The person filling in the form should not wait for either.

That's it.

Read it again and notice what it does **not** say. It says nothing about how the
department is decided, and nothing about what happens if part of it fails. The first is a
detail. The second is most of this lab.

## The venue

Open `venues/` in the application.

Two services have been in this solution since the day you arrived and have never done
anything: **`WorkOrders.Routing`** and **`WorkOrders.Notifications`**. Open both. Each is
a `Hello World!` endpoint.

NATS has been in the AppHost the whole time, and nothing has ever published to it. You
read that note on Monday.

Somebody set all of this up, expecting it to be needed, and then left. Today it gets
needed.

You'll be adding to `venues/` later in this lab.

## The roles

- **record the report and answer the person immediately**
- **decide which department owns it**, somewhere else
- **tell the resident we have it**, somewhere else again
- **make sure the announcement happens if and only if the work order was recorded**

Four parts. The first three are the requirement. **The fourth is not in the requirement
and nobody asked for it**, and by the end of this lab you will not be willing to ship
without it.

Notice what is not on the list: nothing about the resident being told which department,
and nothing about what happens if routing is wrong. Both real. Not today.

## Say what happened

Two messages, in **`WorkOrders.Contracts/Messages.cs`**:

```csharp
namespace WorkOrders.Contracts;

/// <summary>A work order has been recorded. Whoever cares can act on it.</summary>

public record WorkOrderRecorded(string Number, string ReportedBy, string Location, string Description);

/// <summary>Routing has decided which department owns it.</summary>

public record WorkOrderRouted(string Number, string Department);
```

Add to **`WorkOrders.Api/WorkOrder.cs`**, in the `WorkOrder` class:

```csharp
/// <summary>Set asynchronously by the routing service. Null until it has decided.</summary>

public string? Department { get; set; }
```

Read that comment when you write it. It is doing more work than it looks like.

## Wire the API up

All three services need the same two packages. From each of `WorkOrders.Api`,
`WorkOrders.Routing` and `WorkOrders.Notifications`:

```bash
dotnet add package WolverineFx.Nats --version 6.30.3
dotnet add package WolverineFx.RuntimeCompilation --version 6.30.3
```

`Routing` and `Notifications` also need the contracts:

```bash
dotnet add reference ../WorkOrders.Contracts/WorkOrders.Contracts.csproj
```

In **`WorkOrders.Api/Program.cs`**, before `builder.Build()`:

```csharp
builder.UseWolverine(opts =>
{
    opts.UseNats(builder.Configuration.GetConnectionString("nats")!);
    opts.PublishMessage<WorkOrderRecorded>().ToNatsSubject("work-orders.recorded");
    opts.ListenToNatsSubject("work-orders.routed");
});
```

And publish, in **`WorkOrders.Api/Endpoints.cs`**, in the website form endpoint. Add
`IMessageBus bus` to the parameters, and after the save:

```csharp
session.Store(order);
await session.SaveChangesAsync(token);

await bus.PublishAsync(new WorkOrderRecorded(
    order.Number, order.ReportedBy, order.Location, order.Description));

return Results.Created($"/work-orders/{order.Number}", order);
```

## Give the two empty services something to do

**`WorkOrders.Routing/Program.cs`** — decide a department and say so:

```csharp
builder.UseWolverine(opts =>
{
    opts.UseNats(builder.Configuration.GetConnectionString("nats")!);
    opts.ListenToNatsSubject("work-orders.recorded");
    opts.PublishMessage<WorkOrderRouted>().ToNatsSubject("work-orders.routed");
});
```

```csharp
public static class RoutingHandler
{
    public static WorkOrderRouted Handle(WorkOrderRecorded message, ILogger<WorkOrderRecorded> logger)
    {
        var text = $"{message.Location} {message.Description}".ToLowerInvariant();

        var department =
            text.Contains("hydrant") || text.Contains("meter") || text.Contains("sewer") ? "WTR" :
            text.Contains("cart") || text.Contains("collection") || text.Contains("bulk") ? "SAN" :
            text.Contains("park") || text.Contains("bench") || text.Contains("fountain") ? "PRK" :
            "STR";

        logger.LogInformation("DECIDED {Number} -> {Department}", message.Number, department);
        return new WorkOrderRouted(message.Number, department);
    }
}
```

Returning a message publishes it. That is worth a second look — there is no `bus` in that
handler and nothing calls `PublishAsync`.

**`WorkOrders.Notifications/Program.cs`** — listen to the same subject and tell the
resident:

```csharp
opts.ListenToNatsSubject("work-orders.recorded");
```

```csharp
public static class NotificationHandler
{
    public static void Handle(WorkOrderRecorded message, ILogger<WorkOrderRecorded> logger)
    {
        logger.LogInformation("TOLD {ReportedBy} that {Number} was received",
            message.ReportedBy, message.Number);
    }
}
```

Two services, one subject, both get it. Nothing coordinates them.

Finally, the API has to record the decision. **`WorkOrders.Api/RoutedHandler.cs`**:

```csharp
using Marten;
using WorkOrders.Contracts;

namespace WorkOrders.Api;

public static class WorkOrderRoutedHandler
{
    public static async Task Handle(WorkOrderRouted message, IDocumentSession session,
        ILogger<WorkOrderRouted> logger, CancellationToken token)
    {
        var order = await session.Query<WorkOrder>()
            .SingleOrDefaultAsync(w => w.Number == message.Number, token);

        if (order is null) return;

        order.Department = message.Department;
        session.Store(order);
        await session.SaveChangesAsync(token);

        logger.LogInformation("ROUTED {Number} to {Department}", message.Number, message.Department);
    }
}
```

## Watch it happen

```bash
curl -s -X POST http://localhost:5171/intake/website-form \
  -H 'content-type: application/json' \
  -d '{"reportedBy":"Harold Mink","location":"Depot St, eastbound","description":"Pothole opened up again"}' | jq

curl -s http://localhost:5171/work-orders | jq '.[-1] | {number, department}'
```

Run the second command immediately, then again a few seconds later.

<details>
<summary>What you should see</summary>

The `201` comes back with:

```json
{ "number": "2026-0820", "department": null, ... }
```

A moment later, the same work order:

```json
{ "number": "2026-0820", "department": "STR" }
```

In the dashboard: `DECIDED` in routing, `TOLD` in notifications, `ROUTED` in the API. The
person filling in the form waited for none of it.

</details>

## The half of that 201 you can't see

Here's the part that matters.

You returned a work order with `"department": null`, and it was **true when you sent it**
and false a second later. Nothing lied. The response is a description of the world at the
moment it was written, and the world kept going.

That is new, and it is not a detail. Everything you have built until this week had one
property: when the response arrived, the work was done. A caller could read the body and
act on it. **That is no longer true of this endpoint**, and no part of the response says
so — same status code, same shape, same `Location` header.

So a caller that reads `department` out of your `201` and puts it on a screen shows the
resident a blank where a department will be. They are not wrong to have tried. Nothing
told them not to.

That is the cost you have just accepted on the village's behalf, and it bought you an
intake form that answers immediately instead of waiting for two services.

## Break it on purpose

One thing on the roles list has not been built, and it is the one nobody asked for.

In **`WorkOrders.Api/Endpoints.cs`**, put a crash between the save and the publish. This
stands in for the process being restarted, the pod being evicted, or NATS being briefly
unreachable — none of which are unusual:

```csharp
session.Store(order);
await session.SaveChangesAsync(token);

if (submission.Description.Contains("CRASH", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("Simulated crash after the write, before the announcement.");
}

await bus.PublishAsync(new WorkOrderRecorded(...));
```

**Write down what you expect**, then:

```bash
curl -s -o /dev/null -w '%{http_code}\n' -X POST http://localhost:5171/intake/website-form \
  -H 'content-type: application/json' \
  -d '{"reportedBy":"Harold Mink","location":"Depot St","description":"CRASH pothole"}'

curl -s http://localhost:5171/work-orders | jq '.[-1] | {number, description, department}'
```

<details>
<summary>What actually happens</summary>

The caller got a `500`, so as far as they know it failed.

```json
{ "number": "2026-0822", "description": "CRASH pothole", "department": null }
```

**The work order exists.** It is in the database with a real number. Routing never heard
about it, so `department` is `null` and will stay `null` for as long as the village keeps
records. Notifications never heard about it, so Harold was never told.

Nothing will fix this. There is no retry, because nothing knows there is anything to
retry. The only evidence is a row that is slightly wrong in a way nobody queries for.

**This is the fourth role**, and this is why it was on the list.

</details>

## Make it all-or-nothing

The write goes to Postgres. The message goes to NATS. Two systems, and no transaction
spans both — which is the actual problem, and it does not go away by being careful about
the order of two lines.

What can be transactional is the database write and *the intention to send*. Marten and
Wolverine already share that database.

In **`WorkOrders.Api/Program.cs`**, add one line to the Marten registration:

```csharp
    .InitializeWith(new WorkOrderSeed())
    .IntegrateWithWolverine();
```

Then in **`WorkOrders.Api/Endpoints.cs`**, take the session from the outbox instead of
injecting it, and publish before you save:

```csharp
app.MapPost("/intake/website-form", async (
    WebsiteFormSubmission submission, IMartenOutbox outbox, CancellationToken token) =>
{
    var session = outbox.Session!;

    var order = new WorkOrder { /* unchanged */ };

    session.Store(order);

    await outbox.PublishAsync(new WorkOrderRecorded(
        order.Number, order.ReportedBy, order.Location, order.Description));

    // leave the crash here, for now
    if (submission.Description.Contains("CRASH", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("Simulated crash before the transaction commits.");
    }

    await session.SaveChangesAsync(token);

    return Results.Created($"/work-orders/{order.Number}", order);
});
```

`PublishAsync` no longer sends anything. It writes the message into the same database
transaction as the work order, and sending happens after the commit succeeds.

Send the crashing one again, then a normal one.

<details>
<summary>What you should see</summary>

The crashing request returns `500`, and **there is no work order.** The count is
unchanged, nothing was recorded, and nothing was announced. The caller's `500` is now
true.

The normal request returns `201`, and a moment later `department` is `STR`, exactly as
before.

Either both or neither. That is the fourth role, and it took one line of registration and
a different session.

</details>

Take the crash out before you go on.

## Write the venue note

Open `venues/` and add this. It is two notes because they are two decisions, and somebody
will want to change one without the other:

```md
## Intake answers before the work is done

**The role:** answer the person filling in the form without waiting for routing or
notification.

**How we cast it:** the API records the work order, publishes `WorkOrderRecorded`, and
returns `201` immediately. Routing and Notifications react. Routing publishes
`WorkOrderRouted` and the API records the department when it arrives.

**The consequence, which is not optional:** the `201` body has `"department": null`, and
that is true when it is sent and false shortly after. A caller cannot read a department
out of the create response. Nothing in the response says so — same status, same shape.

Worth knowing because everything else in this API still answers with the finished truth.
This endpoint does not, and it is the first one that does not.

## The announcement and the record commit together

**The role:** make sure a work order is announced if and only if it was recorded.

**How we cast it:** `IntegrateWithWolverine()` on the Marten store, and the intake
endpoint takes its session from `IMartenOutbox`. `PublishAsync` writes the message into
the same Postgres transaction as the work order; it is sent after that transaction
commits.

Worth knowing because the obvious arrangement is silently wrong. Saving and then
publishing leaves a window — a restart, an evicted pod, a broker blip — in which the work
order exists and no one has been told. It is not retried, because nothing knows there is
anything to retry, and the only evidence is a record with a null department that nobody
queries for.

There is no transaction across Postgres and NATS and there is not going to be. What the
outbox makes atomic is the write and the **intention** to send.
```

## What this generalizes to

The week has been about services that call each other and answer callers, and this lab
changed what an answer is.

An endpoint that does all the work before it replies can tell the caller the truth in one
message, and pays for it by being as slow and as fragile as everything it depends on. An
endpoint that accepts the work and replies immediately is fast and survives its
dependencies being down, and pays for it by returning something that is **already going
out of date as it is sent**.

Neither is more correct. What is not available is having both, and what is dangerous is
choosing the second by accident — because it looks identical from outside until somebody
relies on a field that had not been filled in yet.

Two things follow, and they will be true long after Theoria.

**A response is a claim about a moment, and asynchrony makes that moment shorter.** The
question worth asking about any endpoint is *how long is this answer good for*, and if the
answer is "until something else finishes", the callers need to know that from something
other than reading the body carefully.

**Whenever a system writes to two places, one of them will eventually be missed.** Not
because anybody was careless — because there is no transaction spanning them. The fix is
never to be more careful about ordering; it is to make one of the two writes carry the
other, which is what the outbox does and is the only reason it exists.

## Last two questions

**One.** Harold Mink has now been told his report was received, by a service that logs a
line. He still does not know a department has been assigned, or that a crew is coming.

Trace what would have to exist for him to find out — not the code, the decisions. Who
decides what he is told, when, and how? Name the ones that are not a developer's to make.

**Two.** Routing decides the department with an `if` statement about words in the
description, in a service that has no other reason to exist.

Ted Vosmik has been doing that job in his head for eleven years and gets it right. What
would you need from him before that `if` statement could be trusted, and what would you do
on the days it is wrong?
