# The phone at Village Hall

**This one is in the work-order application, not in `practice/`.** Everything you do
here stays in the app, and the village keeps it.

Start it if it isn't running. Docker Desktop first, then:

```bash
dotnet run --project WorkOrders.AppHost
```

The api is on <http://localhost:5171>.

## What we're building

Somebody calls Village Hall about a pothole. Whoever picks up needs to put it in the
same place everything else goes.

That's it.

Read it again and notice what it does **not** say. Nothing about forms, fields, or
what the caller is required to tell you. Those are decisions, and they are yours.

## Look at what happens today

```bash
curl -s http://localhost:5171/work-orders | jq '.[] | {number, channel, reportedBy}'
```

Eight work orders. One of them says this:

```json
{
  "number": "2026-0818",
  "channel": "phone",
  "reportedBy": "caller did not give name"
}
```

A phone work order already exists. `Channel` in `WorkOrders.Contracts` has a `Phone`
member. Both have been there since before you arrived.

Now try to create one:

```bash
curl -i -X POST http://localhost:5171/intake/phone \
  -H 'content-type: application/json' \
  -d '{"location":"Depot St","description":"caller reports a hole"}'
```

`404`. **The channel exists in the data and in the type system, and there is no way to
put anything into it.** Somebody at Village Hall is writing these down and somebody
else is typing them in somewhere you cannot see.

## The venue

Open `venues/` in the work-order app. `persistence.md` and `boundaries.md` are the two
that matter here; you have read `http.md` already.

Nothing in `venues/` says anything about intake channels. That is not an oversight you
need to fix yet — but notice it, because you are about to add one.

## The roles

- **accept what the person on the phone actually has**
- **give it a work order number**, in the same sequence as everything else
- **record which channel it came from**
- **answer the caller** — the one at Village Hall, not the resident

Four parts, and only the first is interesting. The other three are already solved
somewhere in this codebase.

## Read the reference first

`WorkOrders.Api/Endpoints.cs` has `POST /intake/website-form`. It is the only channel
that works end to end, and it is the one you should read to learn how this application
does things.

**Do not start typing yet.** Read it, and answer two questions before you write
anything:

1. Where does the work order number come from?
2. What does the website form require the resident to give it?

## Build it

The shape is the website form's shape. What differs is the submission type, and the
difference is the whole lab:

```csharp
public record PhoneCallReport(string? ReportedBy, string Location, string Description);
```

That `?` is a decision. **Make it deliberately.** On the website form the resident
typed their name into a box before the form would submit. On the phone, people hang
up, refuse, or are calling about someone else's street.

Then the endpoint, in `Endpoints.cs`, beside the website form:

```csharp
// The phone at Village Hall.
app.MapPost("/intake/phone", async (
    PhoneCallReport report, IDocumentSession session, CancellationToken token) =>
{
    var order = new WorkOrder
    {
        Id = Guid.CreateVersion7(),
        Number = await Numbering.NextAsync(session, token),
        Channel = Channel.Phone,
        Status = WorkOrderStatus.Open,
        ReportedBy = string.IsNullOrWhiteSpace(report.ReportedBy)
            ? "caller did not give name"
            : report.ReportedBy,
        Location = report.Location,
        Description = report.Description,
        ReportedOn = DateOnly.FromDateTime(DateTime.UtcNow)
    };

    session.Store(order);
    await session.SaveChangesAsync(token);

    return Results.Created($"/work-orders/{order.Number}", order);
});
```

## Watch it

```bash
curl -s -X POST http://localhost:5171/intake/phone \
  -H 'content-type: application/json' \
  -d '{"reportedBy":"Dolores Ankney","location":"Depot St at the alley","description":"Same hole as before, she says"}' | jq

curl -s -X POST http://localhost:5171/intake/phone \
  -H 'content-type: application/json' \
  -d '{"location":"N. Salyer at the culvert","description":"Water standing in the road"}' | jq
```

<details>
<summary>What you should see</summary>

Two `201 Created`, with consecutive numbers continuing the same sequence the website
form uses — not a separate phone sequence.

The second one comes back with:

```json
"reportedBy": "caller did not give name"
```

Which is exactly what 2026-0818 says, and 2026-0818 was written by whoever was doing
this before you. You have just reproduced a convention that already existed in the data
without anyone documenting it.

</details>

## Now go and look at the reference again

You made a decision about a missing name. The website form made one too. Find out what
it was:

```bash
curl -i -X POST http://localhost:5171/intake/website-form \
  -H 'content-type: application/json' \
  -d '{"location":"Depot St","description":"no name given"}'
```

**Predict first.** `WebsiteFormSubmission` declares `string ReportedBy` — no question
mark. What happens?

<details>
<summary>What actually happens</summary>

`201 Created`, and the work order is stored with:

```json
"reportedBy": null
```

The type says `string`. The property on `WorkOrder` says `string` and initialises to
`""`. Neither of those is a runtime check. **Nullable reference type annotations are
compile-time information, and JSON deserialisation is not the compiler.** A field the
caller simply omits arrives as `null`, is stored as `null`, and nothing anywhere
objects.

So the reference channel — the one that works end to end, the one you were told to
read to learn the codebase — has been quietly accepting nameless work orders the whole
time. Yours handles it on purpose. Theirs handles it by not noticing.

</details>

This is what `22-validating-what-arrives` was for. **Go and fix it if you want it
fixed** — you know how, and the decision about which channels require a name is a real
one rather than an exercise.

## Write the venue note

Nothing in `venues/` describes intake. Add it:

```md
## Intake channels do not agree about who reported a work order

**The role:** every work order records who reported it.

**How we cast it:** the website form takes a name from the resident. The phone
endpoint accepts a call without one and records `"caller did not give name"`, which is
the string already used by whoever was entering these before us.

Worth knowing because **the two channels enforce different things.** The phone endpoint
decides deliberately. The website form does not check at all — a submission that omits
`reportedBy` is stored with `null`, because nullable reference annotations are not
runtime validation. Any code reading `ReportedBy` has to cope with a null it was told
could not happen.
```

## The part that only matters because you did this

**Take this if you have time; skip it without guilt if you don't.**

Until this morning, one code path created work orders. Now there are two, and they can
run at the same time.

Open `WorkOrders.Api/Numbering.cs` and read the comment at the top. Somebody wrote it,
it was true, and **you just changed whether it is still true.**

Find out:

```bash
for i in $(seq 1 8); do
  curl -s -X POST http://localhost:5171/intake/phone \
    -H 'content-type: application/json' \
    -d "{\"location\":\"probe $i\",\"description\":\"concurrent probe\"}" \
    -o /tmp/probe$i.json &
done
wait
grep -oh '"number":"[^"]*"' /tmp/probe*.json | sort | uniq -c
```

**Write down what you expect before you run it.**

<details>
<summary>What actually happens</summary>

All eight get the same work order number.

```
   8 "number":"2026-0826"
```

Eight work orders, eight rows in the database, one number between them. `NextAsync`
reads the highest number, adds one, and returns it — and eight requests read the same
highest number before any of them had written.

Nothing failed. Every request returned `201`. The next person to say a work order
number out loud on the phone is naming eight different holes.

The comment in `Numbering.cs` says *"Nothing here is safe under concurrency; nobody has
needed it to be."* Both halves were true when it was written. **You have just made the
second half false, and the comment still says it.**

</details>

Clean up after yourself — those eight are real:

```bash
curl -s http://localhost:5171/work-orders | jq -r '.[] | select(.description=="concurrent probe") | .number'
```

There is no delete endpoint. Deciding what to do about that is part of the exercise;
so is deciding whether it is worth fixing the numbering at all today, for a village
that takes maybe forty work orders a month.

## Last two questions

**One.** 2026-0817 and 2026-0818 are the same pothole, reported twice through two
channels. You have just made the second of those channels much easier to use.

Does your endpoint make that better or worse? What would it take to make it better,
and who would have to decide what "the same hole" means?

**Two.** You copied the website form's shape. Look at what you copied and find one
thing in it that you would not have written if you were starting from nothing.

Say whether it should change, and what it would cost to change it now that there are
two callers of it instead of one.
