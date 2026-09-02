# The message nobody can handle

Work in this lab's own folder, under `practice/`. **Every path below is relative to
it**, and nothing here touches the work-order application.

Docker Desktop first, then:

```bash
dotnet run --project Practice.AppHost
```

`orders` is on <http://localhost:5191>, `crew` on <http://localhost:5192>.

> **Builds on:** *Messages That Wait.* You have a JetStream stream called `WORK`, work
> assignments published to `work.assigned`, and a crew service reading them as a durable
> consumer — so a message survives the crew being switched off.

**This lab is optional and it is about thirty-five minutes.** Durability got the message
to a handler. This is about the message the handler cannot do anything with.

## What we're building

Some work cannot be done. When that happens, whoever assigned it should find out.

That's it.

Read it again and notice what it does **not** say. It does not say the message should be
retried, or kept, or thrown away. It says a person needs to know, which turns out to be
a different requirement from any of those.

## The venue

Open `venues/messaging.md`. Two entries matter, and the second is new.

Durability is per message — the `WORK` stream holds work assignments, and a durable
consumer means the crew service gets them even if it was down.

And: **there is no durable message storage in this solution.** Wolverine's error queue,
inbox and outbox are all features of a database Wolverine owns, and there is not one
here. Remember that entry; it will explain something that is about to look like a lie.

You'll be adding to `venues/` later in this lab.

## The roles

- **notice that this work cannot be done**
- **stop trying**, at some point, on purpose
- **tell somebody who can act on it**
- **not do the work several times on the way to giving up**

Four parts. The last one is not obvious until you have watched it happen.

Notice what is not on the list: nothing about fixing the work order, and nothing about
who decides what "cannot be done" means. Both are the village's problem, not ours.

## Make one that cannot be done

Some work needs a certification the crew does not have. Replace
**`Crew/WorkAssignedHandler.cs`**:

```csharp
using Practice.Contracts;

namespace Crew;

public static class WorkAssignedHandler
{
    public static void Handle(WorkAssigned message, ILogger<WorkAssigned> logger)
    {
        logger.LogInformation("ATTEMPT {Number} at {Location}", message.Number, message.Location);

        if (message.Location.Contains("culvert", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("No crew is certified for culvert work.");
        }

        logger.LogInformation("DONE {Number}", message.Number);
    }
}
```

Restart, and assign two pieces of work — one ordinary, one at the culvert:

```bash
curl -i -X POST http://localhost:5191/assign \
  -H 'content-type: application/json' \
  -d '{"number":"2026-0817","crew":"Dale","location":"Depot St"}'

curl -i -X POST http://localhost:5191/assign \
  -H 'content-type: application/json' \
  -d '{"number":"2026-0819","crew":"Ted","location":"N. Salyer at the culvert"}'
```

**Write down what you expect** in `crew`'s logs, and what each caller got.

<details>
<summary>What actually happens</summary>

Both callers got **`202 Accepted`**.

`crew`'s logs show `ATTEMPT` twice and `DONE` once. The culvert work order was attempted
exactly **once**, threw, and then:

```
Envelope Envelope #... (Practice.Contracts.WorkAssigned) from Orders to
nats://subject/work.assigned was moved to the error queue
```

One attempt. No retry. No error to the caller. The work order for the culvert is not
going to happen, nobody has been told, and the log line says the envelope went somewhere.

</details>

## Where the error queue is

That log line is reassuring and you should not be reassured.

Wolverine's error queue is part of its **durable message storage** — a database Wolverine
owns, where envelopes live so they can be retried, inspected and replayed. This solution
does not have one. That is in your venue notes, and it was true before you read the log
line.

So the sentence is accurate about intent and wrong about outcome. **The envelope was
moved to a place that does not exist here.** It is not in the stream, it is not in a
queue, and no amount of looking will find it.

You can confirm the shape of that yourself: reach for the dead-letter configuration on
the listener, which is what most people try next.

```csharp
opts.ListenToNatsSubject("work.assigned")
    .UseJetStream("WORK", "crew")
    .DeadLetterTo("work.rejected");
```

Restart, send the culvert work order again, and look for anything on `work.rejected`.

<details>
<summary>What you will find</summary>

Nothing. The stream still holds one subject and one message, and no envelope arrives on
`work.rejected`.

That configuration describes where dead letters go **once you have somewhere to put
them.** Without durable storage there is no dead-letter machinery to point at a subject,
so pointing it is free and does nothing.

Take that line back out. It is not the answer here and leaving it in makes the code claim
something untrue.

</details>

## Try harder, on purpose

Perhaps it just needs another go. Add a retry policy, above the listener in
**`Crew/Program.cs`**, and add `using Wolverine.ErrorHandling;` at the top:

```csharp
opts.Policies.OnException<InvalidOperationException>()
    .RetryWithCooldown(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3));
```

**Predict the number of `ATTEMPT` lines** for one culvert work order, then restart and
send one.

<details>
<summary>What actually happens</summary>

**Four.** The original attempt and three retries, at one, two and three seconds.

Then the same *moved to the error queue* line, and the same nothing.

Read the handler again with that number in mind. It logs, and then it throws. **Anything
it had done before throwing, it did four times.** In this lab that is a log line. Put a
database write, an email, or a dispatch above that `throw` and you have done it four
times to find out three more times what you already knew after the first.

Retrying is right for a failure that might not happen again — a timeout, a deadlock, a
service that was restarting. It is exactly wrong for *no crew is certified for culvert
work*, which will be just as true on the fourth attempt as the first.

**Nothing in the code says which kind of failure this is.** `OnException<T>` matches a
type, and one exception type covers both.

</details>

## Say it out loud instead

The infrastructure cannot tell anybody, because telling somebody is not an
infrastructure problem. Whoever assigned the work needs a message, and messages are
something you already know how to send.

Take the retry policy back out, and add a contract —
**`Practice.Contracts/WorkRejected.cs`**:

```csharp
namespace Practice.Contracts;

public record WorkRejected(string Number, string Location, string Reason);
```

Now make the handler **return** it rather than throw. In
**`Crew/WorkAssignedHandler.cs`**:

```csharp
public static WorkRejected? Handle(WorkAssigned message, ILogger<WorkAssigned> logger)
{
    logger.LogInformation("ATTEMPT {Number} at {Location}", message.Number, message.Location);

    if (message.Location.Contains("culvert", StringComparison.OrdinalIgnoreCase))
    {
        logger.LogWarning("CANNOT DO {Number}", message.Number);
        return new WorkRejected(message.Number, message.Location,
            "No crew is certified for culvert work.");
    }

    logger.LogInformation("DONE {Number}", message.Number);
    return null;
}
```

A handler that returns a message publishes it. Returning `null` publishes nothing.

Route it, in **`Crew/Program.cs`**:

```csharp
opts.PublishMessage<WorkRejected>().ToNatsSubject("work.rejected");
```

And have dispatch listen for it. In **`Orders/Program.cs`**, inside `UseWolverine`:

```csharp
opts.ListenToNatsSubject("work.rejected")
    .UseJetStream("WORK", "dispatch-rejections");
```

And at the bottom of the same file:

```csharp
public static class RejectedWorkHandler
{
    public static void Handle(WorkRejected message, ILogger<WorkRejected> logger)
    {
        logger.LogWarning("REJECTED {Number} at {Location} - {Reason}",
            message.Number, message.Location, message.Reason);
    }
}
```

Restart and send both work orders again.

<details>
<summary>What you should see</summary>

`crew`: `DONE 2026-0817`, and `CANNOT DO 2026-0819`. One attempt each.

`orders`:

```
REJECTED 2026-0819 at N. Salyer at the culvert - No crew is certified for culvert work.
```

The work that cannot be done is now a fact in the system, held in the same durable stream
as everything else, delivered to the service that assigned it, with a reason a person
wrote.

</details>

## The half of that exception you can't see

Here's the part that matters.

Throwing said *something went wrong*. It did not say *this work order is impossible*, and
those are not the same claim. The framework can only act on the first one, so it did the
only thing it can do with an unknown failure: it stopped, and it told the log.

**An exception is a message with exactly one recipient, and that recipient is the
infrastructure.** It is the right shape for *the database was unreachable*, because the
infrastructure can genuinely help — retry, back off, move on. It is the wrong shape for
*no crew is certified for this*, because that is not a fault. It is an outcome, and it has
a person waiting on it.

Two things follow.

**A rejection is domain data.** It has a reason somebody chose, it goes in a contract, it
gets published, and it can be handled, stored and counted. None of that is true of a
`throw`, whose payload is a stack trace aimed at whoever reads the logs, which is nobody.

**Retry policies cannot tell the two apart.** `OnException<InvalidOperationException>`
matched a type, and the type does not know whether trying again could ever help. If your
handler throws for both reasons — and most do — then your retry policy is doing the
right thing for one of them and multiplying side effects for the other.

## Write the venue note

Open `venues/messaging.md` and add this:

```md
## Work that cannot be done is a message, not an exception

**The role:** tell somebody that a piece of work will not happen, and why.

**How we cast it:** the handler returns a `WorkRejected`, published to `work.rejected`
and captured by the `WORK` stream. Dispatch reads it under its own consumer name. The
reason is a sentence somebody wrote.

Worth knowing because throwing does not do this, and looks like it might. An unhandled
exception is reported as *"moved to the error queue"*, and **we have no durable message
storage, so there is no error queue.** The envelope is gone. Configuring `DeadLetterTo`
does not change that; it names a destination for machinery we have not enabled.

**Retry policies cannot distinguish a fault from an outcome.** `OnException<T>` matches a
type. A timeout and *no crew is certified for culvert work* can both be an
`InvalidOperationException`, and a retry helps only the first. Verified: three retries
means the handler body runs four times, so every side effect above the throw happens four
times.

**Rule we are adopting:** if a person needs to know, it is a message. If only the
infrastructure needs to know, it is an exception. Anything that has a reason worth writing
in a sentence is the first kind.
```

## Last two questions

**One.** `WorkRejected` goes onto the same stream as `WorkAssigned` and is read by
dispatch. Nothing in this lab looks at it after that.

If the village asked *"how much work did we fail to do last month, and why?"*, could you
answer from what you have built? Say what you would need, and where it should live.

**Two.** The crew handler decides what "cannot be done" means, in an `if` statement, in
the crew service.

Is that the right place? Say who else could own that rule, what would have to move, and
what you would lose by moving it.
