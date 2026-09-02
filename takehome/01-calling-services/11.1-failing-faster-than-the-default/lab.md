# Failing faster than the default

Work in this lab's own folder, under `practice/`. **Every path below is relative to
it**, and nothing here touches the work-order application.

```bash
dotnet run --project Practice.AppHost
```

`orders` is on <http://localhost:5181>, `directory` on <http://localhost:5182>.

> **Builds on:** *Calling Another Service.* You have an endpoint that calls the directory
> service, and you finished by finding out that a dead dependency produces a
> thirty-second hang and then a `500` — on a policy set in `ServiceDefaults` that nobody
> in the room chose.

**This lab is optional and it is about half an hour.** It is about that number. By the
end you will have changed it, found out that changing it in the obvious place does
nothing, and written down what it is and why — which is the part that outlives the lab.

## What we're building

When the directory service is slow, the caller should find out quickly instead of
waiting.

That's it.

Read it again and notice what it does **not** say. It does not say how quickly. That is
the whole lab, it has no default right answer, and by the end you will understand why
nobody can hand you one.

## The venue

Open `venues/aspire.md` and read the entry you wrote in the last lab, the one about
failure behaviour already being configured. It names a file:
`Practice.ServiceDefaults/Extensions.cs`.

Open that file now and read the whole thing. It is short. Most people read it once, when
a template wrote it, and never again — and it is the file that decides how every service
in this solution behaves when something it depends on goes wrong.

One more entry, new for this lab:

> **The directory service can be told to be slow.** `POST /delay/{seconds}` makes
> `/departments` take that long. `GET /delay` says what it is now. It resets to zero when
> the service restarts.

You'll be adding to `venues/` later in this lab.

## The roles

- **decide how long we are willing to wait** for an answer
- **decide how many times to ask** before giving up
- **decide what our caller gets** when we do give up
- **decide where that is written down**, and who has to agree to change it

Four parts, and the fourth is not padding. The first three are numbers, and a number
without a recorded reason is a number the next person will change because it seemed
high.

Notice what is not on the list: nothing about caching an old answer, nothing about
queueing the work for later. Both are real answers to this problem. Neither is this lab.

## Feel it first

Make the directory slow and time an ordinary request.

```bash
curl -s -X POST http://localhost:5182/delay/30
curl -s -o /dev/null -w '%{http_code} in %{time_total}s\n' \
  http://localhost:5181/departments-we-know-about
```

<details>
<summary>What you should see</summary>

```
500 in 30.016039s
```

Thirty seconds, then a `500`. The same shape as the dead dependency in the last lab,
because to your service they are the same event: no usable answer arrived in the time
allowed.

Thirty seconds is `TotalRequestTimeout`, and it is the default. Nobody typed it.

</details>

Try a shorter delay before moving on, so you know the pipeline is not simply broken:

```bash
curl -s -X POST http://localhost:5182/delay/3
curl -s -o /dev/null -w '%{http_code} in %{time_total}s\n' \
  http://localhost:5181/departments-we-know-about
```

`200` in about three seconds. Everything is working; the number is just wrong for us.

## Change it where the client is

The obvious place is where the client is registered. In **`Orders/Program.cs`**:

```csharp
builder.Services.AddHttpClient<DepartmentDirectory>(client =>
{
    client.BaseAddress = new Uri("https+http://directory");
})
.AddStandardResilienceHandler(options =>
{
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(5);
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
    options.Retry.MaxRetryAttempts = 1;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
});
```

Set the delay back to thirty, restart, and time it. **Write down what you expect first.**

```bash
curl -s -X POST http://localhost:5182/delay/30
curl -s -o /dev/null -w '%{http_code} in %{time_total}s\n' \
  http://localhost:5181/departments-we-know-about
```

<details>
<summary>What actually happens</summary>

**About thirty seconds. Again.**

It compiled. It ran. The options are exactly what you wanted. Nothing changed.

Nothing warned you, either — no exception, no log line, no startup error. The code reads
as though it works, and the only way to find out it does not is to time it.

</details>

## Why it did nothing

`ServiceDefaults` calls `ConfigureHttpClientDefaults`, which applies
`AddStandardResilienceHandler()` to **every** `HttpClient` in the service. Yours
included. Adding another one did not replace it.

You now have two resilience pipelines, one inside the other. Yours fails fast at five
seconds, exactly as configured — and the outer one, still on its defaults, treats that
failure as something worth retrying, and keeps going until its own thirty seconds is up.

**Your fast failure became the outer pipeline's retryable event.**

Take that block back out of `Orders/Program.cs` before going on.

## Change it where the decision lives

The number is in `ServiceDefaults` because that is where it was decided. Change it there.

In **`Practice.ServiceDefaults/Extensions.cs`**:

```csharp
http.AddStandardResilienceHandler(options =>
{
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(5);
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
    options.Retry.MaxRetryAttempts = 1;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
});
```

Restart and time it again.

<details>
<summary>What you should see</summary>

```
500 in 5.009162s
```

Five seconds. The caller finds out six times sooner, and you can say why the number is
five.

Note what you just did, because it is the point of the lab. **You changed the failure
behaviour of every service in this solution**, from a file neither of them mentions, by
editing four lines. That is the correct place for it — a shared decision belongs in the
shared project — and it is also why the file deserves a comment saying who decided and
when.

</details>

## The safety feature that cannot fire

There is a circuit breaker in that pipeline. Make the directory slow and call six times.

```bash
curl -s -X POST http://localhost:5182/delay/30
for n in 1 2 3 4 5 6; do
  curl -s -o /dev/null -w "call $n -> %{http_code} in %{time_total}s\n" \
    http://localhost:5181/departments-we-know-about
done
```

**Predict:** does it open?

<details>
<summary>What actually happens</summary>

Six calls, five seconds each. **The breaker never opens.**

`MinimumThroughput` defaults to **100**. The breaker will not act until it has seen a
hundred requests inside its sampling window, and six is not a hundred.

That default is not wrong — a breaker that trips on three requests would trip constantly
on a busy service. But it means **on a low-traffic service the circuit breaker is
decoration.** The village takes about forty work orders a month. A hundred requests in
ten seconds is not a thing that will ever happen here, so the protection is present,
configured, running, and incapable of firing.

Worth knowing about anything described as a safety feature: *at what volume does it start
working, and are we above it?*

</details>

## Make it fire

Add three lines to the same block in **`Practice.ServiceDefaults/Extensions.cs`**:

```csharp
options.CircuitBreaker.MinimumThroughput = 3;
options.CircuitBreaker.FailureRatio = 0.5;
options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
```

Restart, set the delay to thirty, and run the same six calls.

<details>
<summary>What you should see</summary>

```
call 1 -> 500 in 5.083950s
call 2 -> 500 in 2.254697s
call 3 -> 500 in 0.002174s
call 4 -> 500 in 0.001958s
call 5 -> 500 in 0.002036s
call 6 -> 500 in 0.001782s
```

After the third call it stops trying. Two milliseconds. It is not calling the directory
at all any more — it is failing on your side, on the strength of what it learned from the
first two.

That is the thing a circuit breaker buys: when a dependency is down, you stop spending
your own threads and connections finding out again.

</details>

Now the part worth the whole lab. Make the directory healthy and call again immediately.

```bash
curl -s -X POST http://localhost:5182/delay/0
curl -s -o /dev/null -w '%{http_code} in %{time_total}s\n' \
  http://localhost:5181/departments-we-know-about
```

<details>
<summary>What actually happens</summary>

**`500`, in two milliseconds.** The directory is fine. Your service will not call it.

The breaker is open for fifteen seconds and it does not know the dependency recovered,
because it is not looking — that is what being open means. Wait it out and try again:

```
200 in 0.024072s
```

So a circuit breaker converts a slow failure into a fast one, and buys that by **being
wrong for a while after the problem is over.** That window is `BreakDuration`, it is
another number nobody chose, and it is the exact interval during which your service
reports a healthy dependency as broken.

</details>

## The half of that number you can't see

Here's the part that matters.

You set a retry count in a file that applies to every `HttpClient` in every service that
references `ServiceDefaults`. Right now the only call is a `GET`, and retrying a `GET` is
free — ask twice, get the same list.

**That will not stay true.** The first time somebody adds a client that `POST`s, this
same pipeline retries it, and a retried `POST` is not a repeated question. It is a second
attempt at doing something, and whether that is safe is a property of the other service,
not of yours. Two work orders. Two dispatches. Two of whatever the other end does.

Nothing about the file you edited says any of that. It is four lines about timeouts,
sitting in a shared project, quietly deciding a correctness question for code that has
not been written yet.

Three things follow, and only the first is technical:

- **Retry belongs to the operation, not to the service.** A blanket retry policy is a bet
  that every call under it is idempotent, made in advance, on behalf of people who are
  not in the room.
- **Every one of these numbers is a trade you cannot make from a template.** Five seconds
  is right if your caller waits ten and wrong if it waits three. That is a fact about
  your callers, and it is why nobody outside your team can give you the number.
- **A number without a recorded reason gets changed.** Somebody will see thirty seconds,
  think it looks high, and set it to five — for a caller that was happy to wait, in a
  service where five is not long enough to get a real answer. They will be doing their
  best with what the file told them, which is nothing.

## Write the venue note

Open `venues/aspire.md` and add this. **It is a form, and it is a form on purpose** — a
note that says *it depends* cannot be filled in.

```md
## How long we wait, and why

**The numbers:** 5 seconds total, 2 seconds per attempt, 1 retry, breaker opens after 3
requests in 10 seconds with half of them failing, and stays open 15 seconds.

**Where:** `Practice.ServiceDefaults/Extensions.cs`, in `ConfigureHttpClientDefaults`, so
it applies to every `HttpClient` in every service that references the shared project.

**The fact about our callers that justifies it:** _(fill this in. If you cannot, the
number is a guess and should be labelled as one.)_

**What the caller gets when it is exceeded:** a `500`, currently. Whether that is right
is a separate decision and is not made here.

**What we gave up to get it:** a legitimate slow answer that would have arrived at
second 6 is now a failure. If the directory is sometimes slow but correct, we have traded
correctness for latency on purpose.

**Before changing this, know:** it applies to every client in every service, including
ones added later, including `POST`s. Retrying a non-idempotent request is not a repeat of
a question, it is a second attempt at doing something. The circuit breaker's
`MinimumThroughput` decides whether it can fire at all at our traffic; at defaults, on a
service this quiet, it cannot.
```

Fill in the blank line before you move on. If you cannot fill it in, that is the finding,
and *"we do not know what our callers tolerate"* is a legitimate and useful thing to have
written down.

## Last two questions

**One.** Everything in this lab makes the caller wait less for bad news. None of it gets
them the departments.

What if the work did not have to finish while the caller waited at all — if the request
were accepted, the answer produced later, and the retries happened somewhere the caller
is not standing? Say what would have to change about the endpoint's promise, and what new
problem you would have instead.

Hold on to that. It is the last day of this course.

**Two.** You set five seconds. A colleague on another team sets thirty for the same kind
of call and is confident about it.

Assume you are both right. What would have to be different about your two situations for
that to be true? Name three things, and say which of them you actually know about your
own service.
