# When the whole response is wrong

Work in this lab's own folder, under `practice/`. **Every path below is relative to
it**, and nothing here touches the work-order application.

```bash
dotnet run --project Practice.AppHost
```

`orders` is on <http://localhost:5181>, `directory` on <http://localhost:5182>.

> **Builds on:** *When a Field Is Missing.* You have an endpoint that calls another
> service for a list of departments, and you have already dealt with one record in that
> list being wrong.

**This lab is optional and it is about half an hour.** The last one was about a bad
field in a good response. This one is about a response that is not a list of departments
at all — which is a different problem with a different answer, and your code already has
an opinion about it that nobody typed on purpose.

## What we're building

When the department list cannot be produced, the caller should be told **that**. Not
shown an empty list, and not shown our service failing.

That's it.

Read it again and notice what it does **not** say. It says nothing about retrying,
nothing about caching the last good answer, nothing about how long to wait. All
reasonable. Nobody asked.

## The venue

Open `venues/aspire.md`. There is an entry you have not needed until now:

> **The directory service can be told to misbehave.** `POST /mode/{value}` — `ok`,
> `empty`, `html`, `object`, `null`, `emptylist`.

That endpoint exists because this is a practice repository. Real services you do not
control break on their own schedule; this one breaks when you ask it to.

Check where it is before you start:

```bash
curl -s http://localhost:5182/mode
```

You'll be adding to `venues/` later in this lab.

## The roles

- **ask the other service for departments**
- **notice when what came back is not departments**
- **tell our caller something true about which of those happened**

Three parts, and the second and third are the lab. The first one you finished two labs
ago.

Notice what is not on the list: nothing about whose fault it is, and nothing about
making the problem go away. We are deciding what a caller is owed, not fixing the
directory.

## The line you already wrote

Open `Orders/DepartmentDirectory.cs` and look at the last line of the method:

```csharp
var result = await client.GetFromJsonAsync<List<Department>>("/departments", token);
return result ?? [];
```

You pasted that in the first lab of this sequence and it has been there ever since. It
reads as ordinary defensive hygiene — *if there is nothing, hand back an empty list
rather than a null.*

**Write down, before going on: under what circumstances is `result` null?**

Most people answer that quickly and most people are wrong, which is worth knowing before
you rely on the line.

## Break it four ways

For each mode below, **predict the status code and the body your endpoint returns**
before running it. Four predictions, then run all four.

```bash
curl -s -X POST http://localhost:5182/mode/empty
curl -i http://localhost:5181/departments-we-know-about

curl -s -X POST http://localhost:5182/mode/html
curl -i http://localhost:5181/departments-we-know-about

curl -s -X POST http://localhost:5182/mode/object
curl -i http://localhost:5181/departments-we-know-about

curl -s -X POST http://localhost:5182/mode/null
curl -i http://localhost:5181/departments-we-know-about
```

<details>
<summary>What actually happens</summary>

| `directory` returns | Your endpoint returns |
|---|---|
| `204 No Content` | **`500`** — `JsonException: The input does not contain any JSON tokens` |
| `200` and an HTML error page | **`500`** — `JsonException: '<' is an invalid start of a value` |
| `200` and JSON of the wrong shape | **`500`** — `JsonException: The JSON value could not be converted to List<Department>` |
| `200` and the literal `null` | **`200`** and `[]` |

Three of the four are `500`s. One is a `200` that says there are no departments.

Now put `directory` back to normal and try the one case that is not a failure at all:

```bash
curl -s -X POST http://localhost:5182/mode/emptylist
curl -i http://localhost:5181/departments-we-know-about
```

`200` and `[]`. **Identical to the `null` case.** Byte for byte, there is no way for
your caller to tell "the village has no departments" from "the directory service told us
nothing."

</details>

## The half of that `?? []` you can't see

Here's the part that matters.

`result` is null in exactly one situation: the other service sent the literal JSON value
`null`. Not an empty body, not a broken body — a valid JSON document whose content is
"nothing."

And in that one situation, `?? []` **converts *I do not know* into *there are none*.**

That is not a null check. It is a decision, written in the shape of a null check, about
what to tell a caller when the other end has nothing to say. Nobody in this room made
that decision. It arrived with the line, the line looked like hygiene, and it has been
answering that question for three labs.

This is the same shape as a query parameter that will not parse being treated as a
parameter nobody sent. **Whenever a failure and a legitimate empty answer end up in the
same variable, something downstream is going to believe the empty one.**

## The other three, which are louder and not better

The remaining cases give your caller a `500`.

Sit with what that says. `500 Internal Server Error` means *this service has a problem*.
The service does not have a problem. It made a request, got an answer, and the answer
was not usable — which is a true and useful thing that the response does not say.

Two consequences, and the second is worse than the first.

**Your caller cannot tell who is broken.** They will retry against you, page whoever owns
you, and read your logs. All of that is wasted, and the wasted effort is proportional to
how much they trust your status codes.

**In Development, the body is a stack trace naming `System.Text.Json`.** Your internals
are describing themselves to whoever called you, because an unhandled exception is doing
your error reporting.

## Say what actually happened

Two ways to fill the third role. Both are real, and the difference is where the decision
lives.

**Handle it where the call is.** Change `Orders/DepartmentDirectory.cs` so the method can
say *I could not get them*, which is a different answer from an empty list:

```csharp
namespace Orders;

public record Department(string Code, string? Name, string? Contact);

public class DepartmentDirectory(HttpClient client)
{
    // null means "could not be obtained". An empty list means "there are none".
    public async Task<IReadOnlyList<Department>?> GetDepartmentsAsync(CancellationToken token = default)
    {
        try
        {
            return await client.GetFromJsonAsync<List<Department>>("/departments", token);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
```

Add `using System.Text.Json;` at the top. Then in `Orders/Program.cs`:

```csharp
app.MapGet("/departments-we-know-about", async (
    DepartmentDirectory directory, CancellationToken token) =>
{
    var departments = await directory.GetDepartmentsAsync(token);

    return departments is null
        ? Results.Problem(
            title: "Department directory unavailable",
            detail: "The directory service answered with something that was not a department list.",
            statusCode: StatusCodes.Status502BadGateway)
        : Results.Ok(departments);
});
```

Run all five modes again. Four of them now produce `502` with a `problem+json` body, and
`emptylist` produces `200` and `[]` — which now means what it says.

**Or handle it centrally.** Do not write this now; recognise it:

```csharp
// A JsonException from any outbound call becomes a 502, in one place.
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    if (ex is JsonException) { /* write a 502 problem+json */ }
}));
```

They differ on three checkable things. The central one catches calls somebody adds later
and forgets to wrap, which is its whole appeal. It cannot tell *which* dependency failed
without more work, because by the time the exception arrives that context is gone. And it
turns a `JsonException` from **anywhere** into "a dependency is broken" — including one
thrown by parsing something a caller sent you, which is not that at all.

Note also what neither of them fixes: `502` is now returned for a `204`, an HTML page and
a wrong-shaped document alike. Whether those deserve to be distinguished is a real
question, and the answer for most services is no.

## Write the venue note

Open `venues/calling-services.md` and add this beneath the entries from earlier labs:

```md
## A dependency's bad answer is not our failure, and should not look like one

**The role:** decide what our caller is told when another service answers, and the answer
is unusable.

**How we cast it:** the client method returns `null` for "could not be obtained", and the
endpoint turns that into `502 Bad Gateway` with a `problem+json` body. An empty list from
that method means there genuinely are none.

Worth knowing because the default is worse in both directions.

An unhandled `JsonException` becomes a `500`, which tells the caller that **we** are
broken. They retry against us, and in Development they get a stack trace naming our
serialiser. A `502` says the request was fine and something we depend on was not, which
is both true and actionable.

And `?? []` on a deserialised result is not a null check — it is a decision to report
"there are none" when the answer was "I do not know". Those must not share a variable.
Anywhere a failure and a legitimate empty answer collapse into the same value, something
downstream will believe the empty one.

`GetFromJsonAsync` returns null only for the literal JSON `null`. A `204`, an HTML error
page and valid JSON of the wrong shape all throw instead — so the loud cases and the
silent case need different handling, and neither is optional.
```

## Last two questions

**One.** Every failure in this lab arrived quickly. The directory answered; the answer
was wrong.

A slow dependency is a different problem, and you have already seen it once — the
thirty-second hang from the first lab in this sequence. Does anything you wrote today
help with that? Say what a `502` would mean if the directory simply never replied.

**Two.** You now return `502` when the directory misbehaves. The village still has
departments; you just cannot list them right now.

Suppose you had the list from four minutes ago. Would serving it be better than a `502`?
Say what could go wrong if you did, and what you would need to know about the data before
deciding.
