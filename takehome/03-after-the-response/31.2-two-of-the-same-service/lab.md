# Two of the same service

Work in this lab's own folder, under `practice/`. **Every path below is relative to
it**, and nothing here touches the work-order application.

Docker Desktop first, then:

```bash
dotnet run --project Practice.AppHost
```

`orders` is on <http://localhost:5191>, `crew` on <http://localhost:5192>.

> **Builds on:** *Publishing a Message.* You have a service that publishes when work is
> assigned and a service that reacts to it, over NATS.

**This lab is optional and it is about twenty-five minutes.** It is the shortest of these
and it is about one word in one line of configuration that decides how many times the
work gets done.

## What we're building

One copy of the crew service is not going to keep up. Run two.

That's it.

Read it again and notice what it does **not** say. It says nothing about the messages, or
the broker, or delivery. It is a capacity decision — the sort of thing that gets made in a
deployment file by somebody who is not thinking about NATS at all.

## The venue

Open `venues/messaging.md` and read the note from the last lab. It records that messages
are fire-and-forget over core NATS, and what that costs.

It says nothing about how many services are listening, because until now there has only
ever been one of everything.

You'll be adding to `venues/` later in this lab.

## The roles

- **run more than one copy** of the service that does the work
- **make sure a given piece of work is done once**, not once per copy
- **keep working if one copy stops**

Three parts. The second one is the lab, and the reason it is on the list at all is that
nothing about the first one implies it.

Notice what is not on the list: nothing about which copy should do which work, and
nothing about doing them in order. Both are real. Not today.

## Run two of them

One line, in **`Practice.AppHost/AppHost.cs`**:

```csharp
builder.AddProject<Projects.Crew>("crew")
    .WithReplicas(2)
    .WithReference(nats).WaitFor(nats);
```

Restart. In the dashboard, `crew` now expands into two: `crew-0` and `crew-1`, each with
its own logs.

**Write down what you expect** before assigning any work. Specifically: if you send four
work orders, how many log lines will there be in total across both?

## Watch what happens

```bash
for n in 1 2 3 4; do
  curl -s -o /dev/null -X POST http://localhost:5191/assign \
    -H 'content-type: application/json' \
    -d "{\"number\":\"2026-08$n\",\"crew\":\"Dale\",\"location\":\"Depot St\"}"
done
```

Open `crew-0`'s logs in the dashboard, then `crew-1`'s.

<details>
<summary>What actually happens</summary>

**Four log lines in each. Eight in total, for four work orders.**

Both copies handled every message. Not some each — all of them, each.

In this practice repo that is a log line printed twice. In the village it is two crews
dispatched to the same pothole, two notifications sent to Harold Mink, and two entries
against a budget. Adding capacity **doubled the work done** rather than sharing it.

And nothing failed. Both services are healthy, the broker is healthy, and every message
was delivered successfully — twice, exactly as asked.

</details>

## Why

Core NATS is publish–subscribe. A message published to a subject goes to **every**
subscriber listening on it at that moment.

Two copies of your service are not one service that happens to have two processes. They
are two subscribers. NATS has no idea they are related, because you have not told it.

That is worth separating from the decision you made:

- **How many copies to run** is your team's decision, made in a deployment file, on the
  basis of load.
- **What happens when two subscribers listen to one subject** is the broker's rule, and
  it is the same whether you run one copy or forty.

Neither side knows about the other, and the gap between them is where the duplicate
dispatch lives.

## Make them share the work

One line, in **`Crew/Program.cs`**:

```csharp
opts.ListenToNatsSubject("work-assigned")
    .UseQueueGroup("crew");
```

A queue group is a name. Subscribers that give the same name are treated as one logical
subscriber, and NATS delivers each message to exactly one member of the group.

Restart, and send four more.

<details>
<summary>What you should see</summary>

**Four log lines in total**, split across the two instances.

The split will not be even. In one run here it was two and six out of eight; in another it
was four and four. A queue group balances by which member is ready, not by taking turns,
and nothing promised you a fair share.

That matters if you were planning to reason about which instance did what. You cannot.

</details>

## The half of that string you can't see

Here's the part that matters.

The number of times your side effects happen is decided by **whether two processes
supplied the same string**. Not by a type, not by an interface, not by anything the
compiler can check. A typo in `"crew"` makes a second queue group of one, and that
instance quietly goes back to handling everything.

Three consequences worth having straight.

**The failure is a duplicate, not an error.** Nothing throws. The logs look busy and
healthy. You find out from the second crew arriving at the pothole, or from a resident
asking why they got two texts.

**The default is the dangerous one.** Leaving the queue group off gives you fan-out, which
is right for *notify everyone who cares* and catastrophic for *do this work*. The
mechanism does not know which of those your handler is, and the safe-looking choice —
writing nothing — is the one that duplicates.

**Scaling exposed it; scaling did not cause it.** The bug was there with one instance and
was invisible, because one subscriber getting everything is also correct. It became real
the day somebody edited a number in a deployment file for reasons that had nothing to do
with messaging.

## If you did *Messages That Wait*

Skip this if you have not — it will not make sense.

JetStream has the same rule wearing different clothes. Consumers are named, and **the
consumer name plays the part the queue group plays here**:

- Two instances with **different** consumer names are two independent readers of the
  stream. Each gets its own copy of everything. That is what made `notifications` able to
  catch up without taking anything away from `crew`.
- Two instances sharing **one** consumer name are one reader in two processes, and the
  work is split between them. Verified: four messages, two instances, four handled.

So the same string does the same job in both worlds, and the same typo causes the same
duplication. What changes is only whether the message waits for you.

## Write the venue note

Open `venues/messaging.md` and add this:

```md
## How many copies of the work happen is decided by a string

**The role:** make sure a piece of work is done once, no matter how many copies of the
service are running.

**How we cast it:** `UseQueueGroup("crew")` on the listener. Subscribers sharing a queue
group name are one logical subscriber and each message goes to exactly one of them.
Under JetStream the durable consumer name does the same job.

Worth knowing because **the default fans out.** Core NATS delivers to every subscriber,
so two instances of a service each handle every message — two dispatches, two
notifications, two of whatever the handler does. Nothing errors, both instances look
healthy, and the delivery was successful in every case.

The check is not in the type system. Two processes agreeing is two identical strings, and
a typo in one of them silently restores the duplication.

Also: the split across a queue group is **not even and not round-robin.** Do not reason
about which instance handled what.

**Where the two decisions live.** How many replicas to run is ours, in the AppHost or a
deployment file. What happens when two subscribers share a subject is the broker's, and
it does not change with our replica count. A handler that must run once needs a queue
group whether or not anyone has scaled anything yet, because the day somebody does is not
a day anybody will be thinking about this.
```

## Last two questions

**One.** Everything above assumes the two copies are interchangeable. Suppose one of them
handles a message and dies halfway through, before the work is finished but after NATS
delivered it.

What happens to that work order? Say what you would need for the answer to be "another
copy picks it up" — and whether anything you have built this week provides it.

**Two.** The queue group name is `"crew"` and it is in the source of the crew service, so
every copy compiles with the same string.

Name a realistic way two copies of the same service end up with different strings anyway.
Then say what you would do to make it impossible rather than unlikely.
