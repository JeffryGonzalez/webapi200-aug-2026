# Publishing a message

Work in this lab's own folder, under `practice/`. **Every path below is relative to
it**, and nothing here touches the work-order application.

Docker Desktop first, then:

```bash
dotnet run --project Practice.AppHost
```

Two services and a message broker come up. `orders` is on
<http://localhost:5191>, `crew` on <http://localhost:5192>.

## What we're building

When work is assigned to a crew, the crew needs to know. Whoever assigned it should
not have to wait for the crew to be told.

That's it.

Read it again and notice what it does **not** say. Nothing about queues, brokers,
subjects or delivery. It says two things have to happen and only one of them is the
caller's business.

## The venue

Skim `venues/` in this folder. One entry matters:

- **NATS is in the AppHost, and nothing publishes to it.** It has been running the
  whole time you have been in this course.

That is the situation you are in. The infrastructure is there, provisioned by somebody
who expected it to be needed, and no code has ever used it.

You'll be adding to `venues/` later in this lab.

## The roles

- **say that work was assigned**, without waiting for anyone to react
- **carry that announcement** to whoever cares
- **do something about it**, somewhere else, later
- **answer the caller** before any of the above has finished

Four parts. Notice what's not on the list: nothing about what happens if the crew's
service is down, nothing about ordering, nothing about the same assignment arriving
twice. All real. Nobody asked yet.

Notice also that the fourth role exists at all. Somebody made a decision about what the
caller is owed, and it is not *"the crew has been told."*

## Build it

**`Practice.Contracts/WorkAssigned.cs`** — the message. It goes in `Contracts` because
two services need to agree on it:

```csharp
namespace Practice.Contracts;

public record WorkAssigned(string Number, string Crew, string Location);
```

**`Orders/Program.cs`** — publish:

```csharp
builder.UseWolverine(opts =>
{
    opts.UseNats(builder.Configuration.GetConnectionString("nats")!);
    opts.PublishAllMessages().ToNatsSubject("work-assigned");
});
```

```csharp
app.MapPost("/assign", async (WorkAssigned assignment, IMessageBus bus) =>
{
    await bus.PublishAsync(assignment);
    return Results.Accepted();
});
```

**`Crew/Program.cs`** — listen:

```csharp
builder.UseWolverine(opts =>
{
    opts.UseNats(builder.Configuration.GetConnectionString("nats")!);
    opts.ListenToNatsSubject("work-assigned");
});
```

**`Crew/WorkAssignedHandler.cs`** — handle. There is no interface to implement and
nothing to register; Wolverine finds it by its shape.

```csharp
using Practice.Contracts;

namespace Crew;

public static class WorkAssignedHandler
{
    public static void Handle(WorkAssigned message, ILogger<WorkAssigned> logger)
    {
        logger.LogInformation("Crew {Crew} has {Number} at {Location}",
            message.Crew, message.Number, message.Location);
    }
}
```

<details>
<summary>If it will not start, it is almost certainly this</summary>

```
Wolverine is running in TypeLoadMode.Dynamic ... but no IAssemblyGenerator
(Roslyn) is registered. Core WolverineFx no longer ships the runtime compiler.
```

Wolverine writes code at startup and, as of version 6, the thing that compiles it moved
into a separate package. Add `WolverineFx.RuntimeCompilation` to both services.

**Pin it to the same version as `WolverineFx.Nats`.** `dotnet add package` takes the
newest, and the Wolverine packages are not always published in step — a mismatched pair
fails to restore with `NU1102` and a message about a version that does exist.

</details>

## Watch it cross

```bash
curl -i -X POST http://localhost:5191/assign \
  -H 'content-type: application/json' \
  -d '{"number":"2026-0819","crew":"Ted","location":"N. Salyer at the culvert"}'
```

Then open the Aspire dashboard, go to **crew**, and look at its logs.

<details>
<summary>What you should see</summary>

In `orders`: a `202 Accepted`, immediately, with an empty body.

In `crew`, a moment later:

```
Crew Ted has 2026-0819 at N. Salyer at the culvert
```

Two services, no reference between them — `Crew` does not know `Orders` exists and
never will. What they share is the shape of a message, in a project they both reference.

</details>

## Break it on purpose

In the Aspire dashboard, **stop `crew`**.

Now assign some work while nobody is listening:

```bash
curl -i -X POST http://localhost:5191/assign \
  -H 'content-type: application/json' \
  -d '{"number":"2026-0817","crew":"Dale","location":"Depot St"}'
```

**Write down what you expect** to happen — both what `orders` returns now, and what
`crew` will do when you start it again.

Then start `crew` and look at its logs.

<details>
<summary>What actually happens</summary>

`orders` returned **`202 Accepted`**. Same as before. No error, no warning, nothing
different at all.

`crew` starts, listens, and **the message is not there.** It was never delivered and
it is not waiting anywhere. It is gone.

Send another one now and it arrives normally, which is the part that makes this
dangerous — the system looks completely healthy, because it is. Nothing is broken.

</details>

## The half of that 202 you can't see

Here's the part that matters.

`202 Accepted` means *we have taken your request*. It does not mean the work is done,
and here it does not even mean the message will ever be delivered. The caller was told
something true and heard something else.

Two separate things are going on and they are worth keeping apart.

**Asynchrony was the point.** The caller should not wait for a crew to be notified, and
now it doesn't. That is the role that was asked for and it is filled.

**At-most-once delivery was not asked for, and you have it.** Core NATS delivers to
whoever is listening at that moment. Nobody listening means nobody gets it. There is no
queue holding your message, because you did not ask for one and this is not one.

Neither of those is a bug. The second is a **default you inherited by choosing a
transport**, and it is exactly the kind of decision that is invisible until a service
restarts during a deployment and a resident never hears back about their pothole.

NATS can do the other thing — JetStream persists messages and redelivers them. That is
a different configuration and a different set of costs, and it is not what is running.

## Write the venue note

Open `venues/` and add this:

```md
## Messages are fire-and-forget, and that is a choice

**The role:** something has to carry an announcement from one service to another.

**How we cast it:** Wolverine over core NATS, subject `work-assigned`. The publisher
does not wait and does not learn whether anyone received it.

Worth knowing because delivery is **at most once**. If no service is listening at the
moment a message is published, it is gone — no queue, no retry, no error, and a `202`
returned to the caller either way. A deployment that restarts a listener is enough to
lose one.

That is not a defect. It is what core NATS is for, and it is the right choice when
losing a message costs nothing. When it costs something, the answer is JetStream, or an
outbox, or both — and that is a decision somebody has to make on purpose rather than
discover.
```

## Last two questions

**One.** Harold Mink filed the work order and expects to hear back. Suppose the message
that triggers his notification is the one lost during a deployment.

Who finds out? Trace it: which person, by what route, how long afterwards.

**Two.** You returned `202 Accepted` to the caller. Look at `venues/http.md` — it says a
`202` should carry a `Location` header pointing somewhere the caller can look.

Yours doesn't. Should it? What would the caller find there, and what would have to exist
for that to be true?
