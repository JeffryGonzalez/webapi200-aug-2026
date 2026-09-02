# When a field is missing

Work in this lab's own folder, under `practice/`. **Every path below is relative to
it**, and nothing here touches the work-order application.

```bash
dotnet run --project Practice.AppHost
```

You already have an endpoint that lists departments. It works. Somebody has reported a
problem with it.

## What went wrong

A colleague says the department list is showing a blank name for one of the entries.
Not an error, not a failure — one row where the name should be, and isn't.

That's the whole report. It's a real one, in the sense that it is exactly how much
information you usually get.

Read it again and notice what it does **not** say. It doesn't say the other service is
broken, it doesn't say your code is wrong, and it doesn't say what a blank name means.
Any of those could be true. Finding out which is the lab.

## Confirm it before you explain it

```bash
curl -s http://localhost:5181/departments-we-know-about | jq
```

Find the entry they're talking about, and answer one question before you go any
further: **is the name empty, or is it `null`?**

<details>
<summary>Why that distinction is the whole lab</summary>

An empty string is data. Somebody typed nothing, or a system wrote `""`, and that
travelled to you intact.

A `null` in a field your code declares as `string` — not `string?` — is something else.
It means a value you were told would always be there wasn't, and nothing stopped it.
Those have completely different causes and completely different fixes.

</details>

## Look at what the other service actually sent

Your service is not the first place to look. Go one step upstream:

```bash
curl -s http://localhost:5182/departments | jq
```

Compare that response to what your endpoint returned. Then compare it to the shape your
code expects, in `Orders/DepartmentDirectory.cs`:

```csharp
public record Department(string Code, string Name, string? Contact);
```

## The half of that type you can't see

Here's the part that matters.

`string Name` looks like a guarantee. It is not one. It is a **compile-time annotation**
and nothing enforces it while the program is running.

`System.Text.Json` deserialised a payload with no `name` property, passed `null` for
that constructor parameter, and returned you a `Department` whose `Name` is `null` — a
value the type system says cannot exist. No exception. No warning. Your code compiled
with nullable reference types enabled and every check passed, because every check
happens before the program meets any actual JSON.

Two consequences worth having straight:

- **The failure surfaces far from its cause.** Nothing went wrong at the boundary where
  the data arrived. It went wrong wherever somebody eventually used `Name`, which may
  be a different file, a different service, or a template that renders blank rather
  than throwing.
- **This is not a bug in the other service** unless its contract said the field was
  required. If it never promised, it never broke a promise. What broke is that you
  wrote a type that claimed more than the wire did.

Nullable reference types are a very good tool for reasoning about code you control. At
the edge, where JSON comes in, they describe your intentions rather than the world.

## Make the absence visible

Two ways to fill the role, and they differ on a question you'll meet again everywhere:
**do you fail at the boundary, or accept it and cope downstream?**

**Fail at the boundary.** Rewrite the record so deserialisation refuses a payload that
is missing the field:

```csharp
public record Department
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Contact { get; init; }
}
```

`System.Text.Json` honours `required` — a missing `name` now throws a `JsonException`
naming the property, at the moment the bad data arrives.

Run it. Your endpoint now returns a 500 instead of a blank name.

Then try one more payload against it before you decide you've fixed anything:

```json
{ "code": "STR", "name": null, "contact": null }
```

<details>
<summary>What happens, and it is not what most people expect</summary>

**It does not throw.** You get a `Department` whose `Name` is `null`, exactly as
before.

`required` is a **presence** check, not a null check. The property was present. That it
was present and empty is a different question, and `required` does not ask it.

So this option moves the failure earlier for one of the two cases and leaves the other
exactly where it was. That is still worth having — absence is the more common case by
a distance — but it is not the guarantee the keyword's name suggests, and a codebase
that believes it is has a hole in a place nobody is looking.

</details>

<details>
<summary>That looks worse. Is it?</summary>

It is louder, which is not the same thing.

Before, one row was quietly wrong and everything downstream believed it. Now the
request fails, at the boundary, with a message naming the field. Nobody has to find it.

Whether louder is *better* depends on what the caller can do about it — which is the
next option, and the actual decision.

</details>

**Accept it and cope.** Keep the field nullable, and make every consumer deal with the
absence:

```csharp
public record Department(string Code, string? Name, string? Contact);
```

Now the compiler is on your side again: anywhere you use `Name` without handling null,
you get a warning. The type finally tells the truth about the wire.

Note what this one does that `required` doesn't: it covers **both** cases. Absent and
explicitly null arrive at your code the same way, and the compiler makes you deal with
that once, in every place it matters.

## How you'd choose

Not a rule, and this is the part worth arguing about with whoever is nearby.

`required` is right when absence means the record is meaningless and continuing would
produce nonsense — you cannot dispatch work to a vendor whose identity you do not have.
It fails loudly, early, and names the field. Just remember it does not cover an explicit
null, so it narrows the hole rather than closing it.

Nullable is right when the absence is survivable and the caller has something reasonable
to do — show the code instead of the name, and carry on serving nine departments
correctly rather than none. It is also the only one of the two where the compiler helps
you afterwards, for every case.

They combine, and often should: `required` at the boundary for the fields that must be
there, nullable for the ones that might not be, and neither one pretending to be a
guarantee it isn't.

**The wrong answer is the one you started with**: a type that says the field is always
there, and a runtime where it isn't.

## Write the venue note

Open `venues/` in the practice repository and add this to the file about calling
services:

```md
## Types at the edge describe the wire, not our intentions

**The role:** something has to decide what happens when a payload is missing a field we
expected.

**How we cast it:** `required` on fields whose absence makes the record meaningless, so
deserialisation fails at the boundary. Nullable on fields whose absence is survivable,
so the compiler makes every consumer handle it.

Worth knowing because the default is neither. A non-nullable `string` on a
deserialisation target is a compile-time annotation with no runtime effect —
`System.Text.Json` will set it to `null` without an exception, and the failure surfaces
wherever somebody later reads it. Nullable reference types describe code we control;
at the edge they describe our hopes.

Also worth knowing: **`required` checks presence, not null.** A payload sending
`"name": null` satisfies it and still produces a null. Absent and explicitly null are
different on the wire and identical by the time they reach us, unless we do something
about it.

Applies to anything crossing a boundary: HTTP responses, message payloads, configuration
binding, rows from a database that allows nulls in columns our model does not.
```

## Last two questions

Take two minutes. There's no submission.

**One.** The other service never promised `name` would be there — there is no published
contract for it. Now suppose there had been one, and it said the field was required.
Would that change what you should write in your own code? Why?

**Two.** You are about to call a service that *does* publish a specification, and it
says a particular field is always present. How much would you like to bet?

Hang on to that one.
