# Work that runs later

Work in this lab's own folder, under `practice/`. **Every path below is relative to
it**, and nothing here touches the work-order application.

Docker Desktop is not needed. There is no broker and no database.

```bash
dotnet run --project Practice.AppHost
```

One service comes up. `intake` is on <http://localhost:5193>.

## What we're building

Somebody files a work order. We record it, and we tell them we got it. Telling them
should not be the reason they are still waiting.

That's it.

Read it again and notice what it does **not** say. Nothing about queues, threads,
brokers or retries. It says one of the two things we do is the caller's business and
the other one isn't.

## The venue

Skim `venues/` in this folder. Two entries matter:

- **Notifying a resident is slow, and the caller waits for it.**
- **There is no queue and no broker.** One process. Anything that happens later has to
  happen inside it.

## Feel it first

```bash
curl -i -X POST http://localhost:5193/work-orders \
  -H 'content-type: application/json' \
  -d '{"number":"2026-0817","resident":"Harold Mink","location":"Depot St"}'
```

Count the seconds. Three of them, every time, for every caller.

## The roles

- **record the work order**
- **tell the resident**, eventually
- **answer the caller** as soon as the first one is done, without waiting for the
  second

Three parts. Notice what's not on the list: nothing about what happens if the
notification fails, nothing about what happens if the service stops with work still
waiting. Both real. Nobody asked yet.

## Build it

**`Intake/NotificationQueue.cs`** — somewhere to put work that will happen later.
`Channel<T>` is the in-process queue that ships with .NET:

```csharp
using System.Threading.Channels;

public class NotificationQueue
{
    private readonly Channel<WorkOrder> _channel = Channel.CreateUnbounded<WorkOrder>();

    public ValueTask EnqueueAsync(WorkOrder order) => _channel.Writer.WriteAsync(order);

    public IAsyncEnumerable<WorkOrder> ReadAllAsync(CancellationToken token) =>
        _channel.Reader.ReadAllAsync(token);
}
```

**`Intake/ResidentNotifier.cs`** — something that drains it. A `BackgroundService`
starts when the host starts and runs until the host stops:

```csharp
public class ResidentNotifier(NotificationQueue queue, ILogger<ResidentNotifier> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var order in queue.ReadAllAsync(stoppingToken))
        {
            if (string.IsNullOrWhiteSpace(order.Resident))
            {
                throw new InvalidOperationException($"{order.Number} has nobody to notify");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            logger.LogInformation("Notified {Resident} about {Number}",
                order.Resident, order.Number);
        }
    }
}
```

That `throw` is not defensive programming and it is not there to be tidy. It is the
lab. Leave it exactly as written.

**`Intake/Program.cs`** — register both, and stop waiting:

```csharp
builder.Services.AddSingleton<NotificationQueue>();
builder.Services.AddHostedService<ResidentNotifier>();
```

```csharp
app.MapPost("/work-orders", async (WorkOrder order, NotificationQueue queue, ILogger<Program> logger) =>
{
    logger.LogInformation("Recorded {Number}", order.Number);
    await queue.EnqueueAsync(order);
    return Results.Accepted($"/work-orders/{order.Number}");
});
```

The old `NotifyResident` method and its call are gone.

## Watch it

```bash
curl -i -X POST http://localhost:5193/work-orders \
  -H 'content-type: application/json' \
  -d '{"number":"2026-0817","resident":"Harold Mink","location":"Depot St"}'
```

<details>
<summary>What you should see</summary>

Back immediately — about fifty milliseconds instead of three seconds — with
`202 Accepted` and an empty body.

Three seconds later, in the Aspire dashboard under **intake**:

```
Notified Harold Mink about 2026-0817
```

The work still takes three seconds. Nobody is waiting for it. That is the whole of
what was asked for, and it is done.

</details>

## Break it on purpose

Ted's clipboard entries arrive days late, in his handwriting, and sometimes without a
name on them. Here is one.

**Write down what you expect** before you run it — what this call returns, and what
the service does afterwards.

```bash
curl -i -X POST http://localhost:5193/work-orders \
  -H 'content-type: application/json' \
  -d '{"number":"2026-0819","resident":"","location":"N. Salyer at the culvert"}'
```

Then wait a few seconds and try to use the service at all:

```bash
curl -i http://localhost:5193/
```

<details>
<summary>What actually happens</summary>

The `POST` returned **`202 Accepted`**, instantly, exactly like a good one.

Then the whole service stopped.

```
BackgroundService failed
System.InvalidOperationException: 2026-0819 has nobody to notify
The HostOptions.BackgroundServiceExceptionBehavior is configured to StopHost.
A BackgroundService has thrown an unhandled exception, and the IHost instance
is stopping.
```

`GET /` does not answer, because there is nothing there to answer it. **One work order
with a missing name took the intake API offline.**

Nothing about the request that caused it was rejected. It was accepted, with a `202`,
and the caller was told everything was fine — which at that moment it was.

</details>

## Now fix it the way everybody fixes it

The message names the setting. Take it at its word:

```csharp
builder.Services.Configure<HostOptions>(o =>
    o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);
```

Restart, send the same clipboard entry, and then send a good one:

```bash
curl -i -X POST http://localhost:5193/work-orders \
  -H 'content-type: application/json' \
  -d '{"number":"2026-0819","resident":"","location":"N. Salyer at the culvert"}'

curl -i -X POST http://localhost:5193/work-orders \
  -H 'content-type: application/json' \
  -d '{"number":"2026-0821","resident":"Harold Mink","location":"Depot St"}'
```

**Predict again first.** You have configured it not to stop the host. What happens to
Harold's notification?

<details>
<summary>What actually happens</summary>

The service stays up. `GET /` answers `200`. Both posts return `202`. The API is
healthy by every measure you have.

**Harold is never notified, and neither is anyone else, ever again.**

`Ignore` means the host does not stop. It does not mean the background service
restarts — it ended when it threw, and nothing brings it back. The queue still accepts
everything you put in it. Nothing has drained it since the clipboard entry.

There is one line in the log. There is nothing in any response, nothing in the health
check, and nothing a caller could observe.

</details>

## What just happened

You had a loud failure and you turned it into a silent one, using the setting that the
error message suggested.

**Neither of those was wrong.** They are two different answers to a question nobody
asked out loud: *when the work that runs later fails, who is supposed to find out?*

- **`StopHost`** answers *everybody, immediately, whether they can act on it or not*.
  A missing name in one channel takes down intake for all four.
- **`Ignore`** answers *nobody*. The system reports itself healthy while doing half
  its job.

The real answer is neither, and you cannot write it with what is in this folder. It
needs the work to survive the process, some idea of how many times to try again, and
somewhere to put the thing that will never succeed. **That is a queue with delivery
guarantees**, and you have just spent forty minutes discovering the shape of the hole
it fills.

Notice what you *did* get for free: the caller stopped waiting. That part worked, cost
almost nothing, and needs no broker at all. **Getting work off the request thread and
making sure the work survives are two different problems**, and this folder solves
exactly one of them.

## Write the venue note

Open `venues/` and add this:

```md
## Background work is in-process, and it does not survive anything

**The role:** telling a resident their work order was received, without making the
caller wait.

**How we cast it:** a `Channel<T>` and a `BackgroundService`, in the same process as
the API.

Worth knowing because **nothing here is durable and nothing retries.** Work waiting in
the channel is lost if the process stops, for any reason — a deploy, a crash, a
scale-down. A notification that throws is not tried again. And the behaviour when one
throws is a single setting with two bad answers: stop the whole host, or continue with
the notifier dead and nothing draining the queue.

That is not a defect. It is what an in-process queue is, and it is the right choice
when the work is cheap to lose. When it is not cheap to lose, the answer is a broker
with delivery guarantees, or an outbox, or both — and that is a decision somebody has
to make on purpose rather than discover.
```

## Last two questions

**One.** You returned `202 Accepted` and then the notification never happened. In the
messaging lab you returned `202 Accepted` and the message was never delivered.
Different mechanism, same `202`, same outcome for Harold.

What would have to be true for a `202` to be worth more than it is worth here?

**Two.** Suppose the clipboard entry had been rejected at the edge — no name, no
work order, `400`, before anything was queued.

Does that fix this? Say what it fixes and what it does not.
