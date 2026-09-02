# Calling another service

Work in this lab's own folder, under `practice/`. **Every path below is relative to
it**, and nothing here touches the work-order application.

```bash
dotnet run --project Practice.AppHost
```

Two services are here: `orders`, which you will edit, and `directory`, which you will
not. `directory` belongs to somebody else — treat it as a service you cannot change,
because that is the situation this lab is about.

## What we're building

The orders service needs to show which departments exist. It doesn't have that list.
Another service does.

That's it. That's the whole thing.

Read that again and notice what it does **not** say. It says nothing about HTTP,
nothing about clients, nothing about JSON. It's the thing somebody wants. Everything
else is our problem.

Click below when you've read it.

## The venue

Before building anything, the first question is what's already available and what that
rules in or out.

Open `venues/` in the practice repository and skim the files. Don't study them — you're
looking for the shape of what's there.

Two things matter for this lab:

- **Aspire runs our services and resolves them by name.** Nothing in our code knows a
  port number.
- **`ServiceDefaults` is already referenced by every service**, and service discovery
  comes with it. You don't have to add it.

You'll be adding to `venues/` later in this lab.

If it isn't already running, start it and leave it running. The Aspire dashboard opens;
`orders` and `directory` should both be green.

## The roles

Here's the same paragraph as a list of parts that have to be filled:

- **know the other service's shape** — what it returns, and as what
- **hold something that can make HTTP calls**, without making a new one each time
- **know where that service is right now**
- **turn its response into our types**
- **hand the result to whoever asked**

Five parts. Notice what's not on the list: nothing about what to do when the other
service is slow, nothing about retrying, nothing about caching the answer. Those are
all reasonable things to want. Nobody asked for them yet, so we're not building them.

Read the list against the paragraph and satisfy yourself that it covers it.

## Look at what you're calling

From the dashboard, open `directory` and call it:

```bash
curl -s http://localhost:5182/departments | jq
```

Two questions worth answering now rather than during debugging:

- What shape comes back — an array, or an object with the array inside it?
- Is every field present on every record?

<details>
<summary>Why the second question matters more than it looks</summary>

Every field being present in the three records you just looked at is not the same as
every field being present. You are reading a sample, not a contract.

If there is a published spec, that is the contract. If there is not, the sample is all
you have and you should assume it is incomplete.

</details>

## Build it

Create `Orders/DepartmentDirectory.cs` and paste this in:

```csharp
namespace Orders;

public record Department(string Code, string Name, string? Contact);

public class DepartmentDirectory(HttpClient client)
{
    public async Task<IReadOnlyList<Department>> GetDepartmentsAsync(CancellationToken token = default)
    {
        var result = await client.GetFromJsonAsync<List<Department>>("/departments", token);
        return result ?? [];
    }
}
```

Register it in `Orders/Program.cs`:

```csharp
builder.Services.AddHttpClient<DepartmentDirectory>(client =>
{
    client.BaseAddress = new Uri("https+http://directory");
});
```

And an endpoint, in the same file:

```csharp
app.MapGet("/departments-we-know-about", async (
    DepartmentDirectory directory, CancellationToken token) =>
{
    var departments = await directory.GetDepartmentsAsync(token);
    return Results.Ok(departments);
});
```

Run it and confirm it works before you go on — the rest of this lab is about code
that's already in front of you.

```bash
curl -s http://localhost:5181/departments-we-know-about | jq
```

## Which line is doing which job

Go back to the five roles and match them against what you pasted. Most map cleanly:
the record and `GetFromJsonAsync` know the shape and turn the response into our types,
the endpoint hands the result over.

Two are worth pausing on.

**Nowhere did you write `new HttpClient()`.** The class takes one in its constructor
and something else decides where it comes from. That's the "without making a new one
each time" part of role two, and it is not obvious that role two had that clause until
you see what happens without it.

**`https+http://directory` is not a URL.** It's a service name. Something resolves it
at runtime — prefer HTTPS, fall back to HTTP — and nothing in your code knows a port.

## Break it on purpose

With everything running, stop `directory` from the Aspire dashboard.

**Write down what you think will happen** to your endpoint before you call it. Then
call it.

```bash
curl -i http://localhost:5181/departments-we-know-about
```

<details>
<summary>What you should see, and it is probably not what you wrote down</summary>

**Nothing, for about thirty seconds.** Then a `500`.

Not a connection error, not an immediate failure. The request sits there. From a
browser the tab would spin; if a hundred callers had sent this, you would now have a
hundred requests waiting on a service that is never going to answer.

**A hang is a worse failure than an error.** An error tells the caller something. A
hang holds their connection, their thread and their patience, and tells them nothing.

Two things worth separating.

The fault is **inherited**. Nothing in your code is wrong — you took a dependency and
the dependency stopped. That is not a bug you can fix, only one you can decide what to
do about.

And the thirty seconds came from **somewhere**. You did not write it. Finding out where
is the next step.

</details>

Bring `directory` back up before you move on.

## Where the thirty seconds came from

Open `Practice.ServiceDefaults/Extensions.cs` and find this:

```csharp
builder.Services.ConfigureHttpClientDefaults(http =>
{
    // Turn on resilience by default
    http.AddStandardResilienceHandler();

    // Turn on service discovery by default
    http.AddServiceDiscovery();
});
```

Most people read that file once, when the template wrote it, and never again.

`AddStandardResilienceHandler()` applies to **every** `HttpClient` in this service,
including the one you registered a few steps ago. It brings retries, a per-attempt
timeout, a total request timeout of thirty seconds, and a circuit breaker — none of
which you asked for, all of which were running during the previous step.

So the honest description of what happened is not *"nothing handled the failure."* It
is: **something handled it thoroughly, on a policy nobody in this room chose.** It
retried. It waited. It gave up on a schedule set by a default.

Whether thirty seconds is right for your callers is a question you now have and could
not have had ten minutes ago.

## Two ways you'll see this done instead

You will meet both in real codebases. **Don't write either one now.** They're here so
you recognise them, and so that what you pasted is a choice rather than a ritual.

One of them is a defect. The other is a different way to fill the same role.

```csharp
// ANTI-PATTERN. DO NOT COPY THIS INTO REAL CODE.
// A new HttpClient per call exhausts sockets under load: each instance holds its
// connections open after disposal. The failure arrives weeks later as intermittent
// connection errors under traffic, a long way from this line.
public async Task<List<Department>> GetDepartmentsAsync()
{
    using var client = new HttpClient();
    client.BaseAddress = new Uri("http://localhost:5182");
    return await client.GetFromJsonAsync<List<Department>>("/departments") ?? [];
}
```

Two separate faults in six lines, and both work perfectly this morning. Note the
address: it is the **correct** one. That is what makes it insidious — nothing about it
looks wrong until somebody else runs this.

```csharp
// NOT AN ANTI-PATTERN. A named client is a real option with different properties.
builder.Services.AddHttpClient("directory", c => c.BaseAddress = new Uri("https+http://directory"));

var client = factory.CreateClient("directory");
```

Fine, and you'll meet it. It differs from what you pasted in three checkable ways: the
name is a **string**, so a typo fails at runtime rather than at build; registration and
use are tied together by nothing except that string; and the dependency doesn't appear
in any constructor signature, so what a class talks to isn't visible from its shape.

Whether that trade is worth making is a judgement. Your instructor has one, and so does
your team.

These three aren't the whole set. If you want the rest, ask in a way that extends what
you now know:

> *"I've seen three ways to get an HTTP client in ASP.NET Core — constructing one
> directly, a named client from `IHttpClientFactory`, and a typed client — and I
> understand they differ on whether a mistake fails at build time or runtime, on where
> configuration lives, and on whether the dependency is visible in a constructor
> signature. What other approaches exist, and what does each change about those three
> things?"*

## The half of that address you can't see

Here's the part that matters.

`https+http://directory` was doing **two** jobs, and you only saw one of them.

The job you saw is ours. Aspire assigns ports at random, so hardcoding one wouldn't
survive a restart on your own machine. Service discovery solves a local problem and you
watched it solve it.

The other job you can't see at all. That same line is what makes this code work in a
container, in a cluster, on a colleague's machine, and in an environment nobody has
built yet — because it never claimed to know where `directory` lives. **A base URL
written in code is a deployment decision recorded in the wrong place.** It will be
wrong everywhere except the machine it was typed on, and it will be wrong *silently*,
because a URL is always syntactically fine.

Nothing in our five roles asked for that. Role three said *know where that service is
right now*, and the honest reading of "right now" is that your code should not be the
thing that knows.

That's worth writing down, because it isn't about this directory service and it'll be
true of every service you call for the rest of your career.

Open `venues/` and add this to the file about how we call services — create
`venues/calling-services.md` if it isn't there:

```md
## Addresses come from outside the code

**The role:** something has to know where another service is right now. Every app fills
it, including the ones where nobody noticed they were filling it.

**How we cast it:** a service *name*, resolved at runtime. `https+http://directory`,
never a URL and never a port.

Worth knowing because the alternative fails silently. A hardcoded base address is
syntactically valid everywhere and correct in exactly one place, so it survives review,
survives the build, and is wrong the first time anyone else runs it.

Configuration files and environment variables cast the same role differently and are
also fine. What they have in common is that the *code names the service* and something
outside the code decides where it is today.

## Failure behaviour is already configured, in ServiceDefaults

**The role:** something has to decide what this service does when one it calls is slow
or absent.

**How we cast it:** `AddStandardResilienceHandler()` in
`Practice.ServiceDefaults/Extensions.cs`, applied to every `HttpClient` via
`ConfigureHttpClientDefaults`. Retries, a per-attempt timeout, a thirty-second total
request timeout, and a circuit breaker.

Worth knowing because it is invisible at the call site. A dead dependency produces a
thirty-second hang and then a `500`, and nothing in the endpoint, the client class or
the registration says so. The defaults are reasonable; the point is that they are
defaults, and the behaviour of this service under failure is set in a file most people
read once.
```

## Last two questions

Take two minutes. There's no submission — they're for you.

**One.** Look back at the five roles. Which lines of what you pasted are there because
of one of them, and which are there for some other reason? For each line in the second
group, what is it there for?

**Two.** The `Department` record has `string? Contact` — nullable — and `string Name`,
which isn't. You didn't choose that; it was in the code you pasted.

What happens if the other service stops sending `Name` one day? Not *should* it — what
actually happens, at which line, and what does the caller see?

Hang on to your answer. It's the next lab.
