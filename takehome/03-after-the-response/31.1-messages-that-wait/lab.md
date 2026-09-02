# Messages that wait

Work in this lab's own folder, under `practice/`. **Every path below is relative to
it**, and nothing here touches the work-order application.

Docker Desktop first, then:

```bash
dotnet run --project Practice.AppHost
```

`orders` is on <http://localhost:5191>, `crew` on <http://localhost:5192>. There is a
third service, `notifications`, which does not start with the others.

> **Builds on:** *Publishing a Message.* You have a service that publishes when work is
> assigned, a service that reacts to it, and a break step where you stopped the listener,
> published anyway, and the message was never seen again.

## What we're building

The crew still needs to know when work is assigned. This time, a crew whose service was
switched off should find out when it comes back.

That's it.

Read it again and notice what it does **not** say. It says nothing about queues,
persistence, or replay. It says a message should survive nobody being there to hear it.

## The venue

Open `venues/messaging.md` and read the note you wrote at the end of the last lab —
the one about fire-and-forget being a choice. It ends like this:

> When it costs something, the answer is JetStream, or an outbox, or both — and that is
> a decision somebody has to make on purpose rather than discover.

This lab is somebody making it on purpose.

There is a second entry worth reading now: **there is a third service in this solution
and it has never been started.** `Notifications` is registered in the AppHost with
`.WithExplicitStart()`, so it shows up in the dashboard with a Start button instead of
running with everything else. Open `Notifications/Program.cs` and
`Notifications/NotificationHandler.cs` and read them. You will not edit them for a
while.

You'll be adding to `venues/` later in this lab.

## The roles

- **hold an announcement** somewhere, after it is published and before anyone takes it
- **decide which announcements are worth holding**, because not all of them are
- **remember what each interested service has already seen**
- **still answer the caller immediately**, exactly as before

Four parts. Notice the second one. Nobody asked for *everything* to be durable, and the
list would be simpler if they had.

Notice also what is not on the list: nothing about ordering, nothing about the same
message arriving twice, nothing about how long to hold anything. All real. Not today.

## Turn the stream on

Two edits, and the first one is not in your service.

**`Practice.AppHost/AppHost.cs`** — JetStream is a NATS server feature and it is off by
default:

```csharp
var nats = builder.AddNats("nats").WithJetStream();
```

**`Orders/Program.cs`** — declare the stream, and route the message to a subject that
falls inside it:

```csharp
builder.UseWolverine(opts =>
{
    opts.UseNats(builder.Configuration.GetConnectionString("nats")!)
        .DefineStream("WORK", stream => stream
            .WithSubject("work.>")
            .WithLimits(maxMessages: 1_000, maxAge: TimeSpan.FromDays(1)));

    opts.PublishMessage<WorkAssigned>().ToNatsSubject("work.assigned");
});
```

Two things changed that are easy to read past.

`PublishAllMessages()` became `PublishMessage<WorkAssigned>()`. You will need the
per-message form shortly, and the reason is the point of this lab.

The subject went from `work-assigned` to `work.assigned`. Dots are how NATS builds
hierarchies: `work.>` means *`work.` followed by anything*, and the stream captures
every subject that matches. `work-assigned`, with a hyphen, does not match it.

Restart the AppHost.

## Make the crew a durable consumer

**`Crew/Program.cs`** — the subject changes to match, and one line is added:

```csharp
opts.ListenToNatsSubject("work.assigned")
    .UseJetStream("WORK", "crew");
```

Two names, and they are different kinds of thing. `WORK` is the stream — the thing
holding messages. `crew` is this consumer's name, and it is how the server remembers
what this service has already seen.

Restart, and confirm it still works normally before breaking anything:

```bash
curl -i -X POST http://localhost:5191/assign \
  -H 'content-type: application/json' \
  -d '{"number":"2026-0819","crew":"Ted","location":"N. Salyer at the culvert"}'
```

Check `crew`'s logs in the dashboard. Same log line as last lab.

## Watch it survive

This is the break step from the last lab, run again against the new arrangement.

In the dashboard, **stop `crew`**. Then assign work while nobody is listening:

```bash
curl -i -X POST http://localhost:5191/assign \
  -H 'content-type: application/json' \
  -d '{"number":"2026-0817","crew":"Dale","location":"Depot St"}'
```

**Write down what you expect** before you start `crew` again.

<details>
<summary>What actually happens</summary>

`orders` returned `202 Accepted`, exactly as it did before. Nothing about the caller's
experience changed, which is worth noticing on its own — the fix was invisible from
outside.

Start `crew`. In its logs, a moment after it comes up:

```
Crew Dale has 2026-0817 at Depot St
```

The message was held while the service was gone and delivered when it returned. The
same sequence that lost a message in the last lab now does not.

</details>

## A message nobody minds losing

Not everything deserves that. `Practice.Contracts` already has a second message,
unused so far:

```csharp
public record ShiftNoteAdded(string Crew, string Note);
```

Crew board chatter for the break-room wallboard. If one is lost, nothing happens to
anybody.

Add it to **`Orders/Program.cs`**, alongside the existing route:

```csharp
opts.PublishMessage<ShiftNoteAdded>().ToNatsSubject("shift-notes");
```

And an endpoint, in the same file:

```csharp
app.MapPost("/shift-note", async (ShiftNoteAdded note, IMessageBus bus) =>
{
    await bus.PublishAsync(note);
    return Results.Accepted();
});
```

`shift-notes` has no dot, so it is outside `work.>`, so the stream never sees it. That
is deliberate. This is the message we are choosing not to protect.

This is why `PublishAllMessages()` had to go. It said *everything goes to one subject*,
and you now have two messages that want different guarantees.

Restart, and send one:

```bash
curl -i -X POST http://localhost:5191/shift-note \
  -H 'content-type: application/json' \
  -d '{"crew":"Ted","note":"Culvert grate is loose, flag it"}'
```

Nothing is listening for it yet. That is fine.

## Start the service that has never run

`Notifications` listens for **both** messages. Read
`Notifications/Program.cs` again now that the subjects mean something to you.

Give it one line so its work listener uses the stream, in **`Notifications/Program.cs`**:

```csharp
opts.ListenToNatsSubject("work.assigned")
    .UseJetStream("WORK", "notifications");
```

Leave the `shift-notes` listener exactly as it is.

Note the consumer name: `notifications`, not `crew`. Restart the AppHost.

Now, **before starting it**, send a few more of each while it is still switched off:

```bash
curl -s -X POST http://localhost:5191/assign \
  -H 'content-type: application/json' \
  -d '{"number":"2026-0821","crew":"Dale","location":"Third and Miami"}'

curl -s -X POST http://localhost:5191/shift-note \
  -H 'content-type: application/json' \
  -d '{"crew":"Dale","note":"Back gate is chained, use Sixth"}'
```

**Write down what you expect** `notifications` to show when it starts. It has never run
in its life. Be specific: which of the two message types, and how many of each.

Then find `notifications` in the dashboard and press Start.

<details>
<summary>What actually happens</summary>

Its log shows **every work assignment you have sent in this lab** — including ones
published before this service had ever been running:

```
NOTIFIED resident: 2026-0819 assigned to Ted
NOTIFIED resident: 2026-0817 assigned to Dale
NOTIFIED resident: 2026-0821 assigned to Dale
```

And **not one shift note.** Those are gone.

Same service. Same start. Two subjects, two outcomes.

Now send a shift note while it is running:

```bash
curl -s -X POST http://localhost:5191/shift-note \
  -H 'content-type: application/json' \
  -d '{"crew":"Ted","note":"Wallboard test"}'
```

It appears immediately. The listener was never broken. It simply cannot be given
anything that happened before it arrived.

Two details worth having. `crew` already handled those same assignments, and handling
them again here took nothing away from it — each consumer name has its own position in
the stream, and each gets its own copy. And the replayed messages may not arrive in the
order you sent them; nothing here promised they would.

</details>

## Break it on purpose

Now take one line back out. In **`Crew/Program.cs`**, drop the JetStream line and leave
everything else — same subject, same stream, same publisher:

```csharp
opts.ListenToNatsSubject("work.assigned");
```

Restart. Stop `crew` in the dashboard. Assign work while it is down:

```bash
curl -s -X POST http://localhost:5191/assign \
  -H 'content-type: application/json' \
  -d '{"number":"2026-0822","crew":"Ted","location":"Kossuth Ave"}'
```

**Write down what you expect.** The stream still exists. The subject still matches. The
message is still being published to a stream that is holding it.

Then start `crew`.

<details>
<summary>What actually happens, and where the message actually was</summary>

`crew` receives **nothing**. It behaves exactly as it did in the last lab.

Here is the part worth sitting with: **the message was never lost.** It is in the
`WORK` stream right now. NATS accepted it, wrote it down, and is still holding it.
`crew` cannot see it, because a plain listener has no position in a stream and no way
to ask for anything that happened before it connected.

You can prove it is there. In `Notifications/Program.cs`, change the consumer name —
just the name — to something that has never been used:

```csharp
.UseJetStream("WORK", "audit");
```

Restart and start `notifications`. Every assignment in this lab arrives, including
`2026-0822`, the one `crew` just failed to receive.

The message was sitting in the broker the whole time and one of your services could not
reach it.

Put `crew`'s `.UseJetStream("WORK", "crew")` back, and set the consumer name in
`Notifications` back to `notifications`, before moving on.

</details>

## The half of that stream you can't see

Here's the part that matters.

It is tempting to summarise this lab as *"we turned on durability."* That is not what
happened, and the difference will cost somebody a weekend one day.

**Durability is not a property of the broker.** NATS was running the whole course. It
is not a property of the service either — `notifications` got a backlog for one message
type and nothing for the other, in the same process, in the same second.

It is a property of **a subject being captured by a stream**, and **a consumer having a
name that the server remembers**. Both, together. Miss either one and you have exactly
the behaviour from the last lab, with a broker that is durable, a stream that is
holding your message, and a service that never sees it.

Which means the interesting question is no longer *"is our messaging durable?"* It is
*"which messages are we protecting, and what did that decision cost?"* Streams hold
data on disk, replay costs time at startup, limits have to be chosen, and every
consumer name is a position somebody has to reason about. You paid for the work
assignments. You deliberately did not pay for the shift notes.

That was a choice, and it was yours, and it should be written down.

## Write the venue note

Open `venues/messaging.md` and add this beneath the fire-and-forget note you wrote last
time. It does not replace that note — it is the other half of it:

```md
## Durability is per message, not per system

**The role:** hold an announcement between publishing it and somebody being ready to
take it.

**How we cast it:** a JetStream stream, `WORK`, capturing `work.>`, with each
interested service reading it under its own consumer name. Work assignments are routed
to `work.assigned` and are held. Shift notes go to `shift-notes`, which no stream
captures, and are not.

Two things have to be true together, and either one alone gives you nothing:

- the subject has to be **captured by a stream**
- the listener has to be a **named consumer** of that stream

A plain listener on a subject that a stream is capturing still receives nothing that
was published while it was down. The message is there. It cannot ask for it.

Worth knowing because "we use a durable broker" is not a statement about whether any
particular message survives. Each message type is a separate decision, and the answer
for shift notes is deliberately no.

What this costs: stream storage, a replay at startup for a consumer that has fallen
behind, retention limits somebody has to choose, and one more name per service that has
to be got right. Replayed messages are not guaranteed to arrive in the order they were
sent, and a consumer that has fallen a long way behind may take a while to catch up.
```

## Last two questions

**One.** `notifications` replayed every work assignment from the beginning of the
stream the first time it started. In this lab that was four messages and it was the
point.

Suppose the stream holds six months of work assignments and you deploy a new service
that listens to it. What happens on its first start, who notices, and what would you
want to have decided in advance?

**Two.** Harold Mink's work order was assigned while `crew` was down, and now the
message survives and he gets his notification. Something still has to be true for that
to work: `orders` had to successfully publish in the first place.

Suppose `orders` writes the work order to its database, and then the publish fails —
broker unreachable, process killed, network gone. The work order exists. The
announcement was never made, and nothing is holding one.

Does anything in this lab help? If not, what would have to change, and where would it
have to live?
