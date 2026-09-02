# A client you did not write

Work in this lab's own folder, under `practice/`. **Every path below is relative to
it**, and nothing here touches the work-order application.

```bash
dotnet run --project Practice.AppHost
```

`dispatch` is on <http://localhost:5181>. The purchasing catalog is **not** in this
solution — it is a real service at
<https://theoria.hypertheory-labs.com/clerk-records/purchasing>, and you will call it
over the internet.

> **Builds on:** *Dispatch Checks Standing*, where you wrote a client for this service by
> hand, and *When a Field Is Missing*, where you decided how a type at the edge should
> describe a payload you do not control.

**This lab is optional and it is about forty minutes.** You will need a tool installed:

```bash
dotnet tool install --global Microsoft.OpenApi.Kiota
```

## What we're building

Dispatch needs a vendor's standing from the purchasing catalog. The catalog publishes a
specification. Do not write the client by hand.

That's it.

Read it again and notice what it does **not** say. It says nothing about which generator,
and nothing about what to do with what comes out. Both are ours.

## The venue

Open `venues/the-catalog.md`. Two entries matter here.

The catalog **is not ours and is not in this solution.** You cannot restart it and you do
not have its source. Everything else you have practised against this week was a service
you could stop.

There is a **published specification, and it is in this repository** —
`catalog-openapi.yaml` at the root. That note ends with a sentence worth rereading:

> It is a document, not a program. Nothing enforces it — not on their side, and not on
> ours unless we make it.

You'll be adding to `venues/` later in this lab.

## The roles

- **know the shape of what the catalog returns**
- **make the call and turn the answer into our types**
- **know where the catalog is**
- **decide what our code believes about the answer**

Four parts. You filled all four by hand once already. Three of them are tedious and a
generator does them better. The fourth is the lab.

Notice what is not on the list: nothing about what to do when the catalog is down. You
have done that elsewhere and it does not change here.

## Read the specification first

Before generating anything, open `catalog-openapi.yaml` and find `VendorStanding` under
`components/schemas`. Read its `required` list.

**Write down the six field names it says are required.** You will want them shortly.

<details>
<summary>What it says</summary>

```yaml
VendorStanding:
  type: object
  required: [vendorId, status, effectiveDate, authority, reason, reviewedOn]
```

Six fields the catalog promises are always present. Note `reason` in particular — the
spec says it is required, and it is `type: string`, not nullable.

`expiresOn` is deliberately outside that list and typed `[string, 'null']`, so the
document is capable of saying "this one might be absent". It said it for that field and
not for `reason`.

</details>

## Generate the client

From the `Dispatch` folder:

```bash
cd Dispatch
dotnet add package Microsoft.Kiota.Bundle --version 2.0.0
kiota generate -d ../catalog-openapi.yaml -l CSharp -n Dispatch.Catalog -o ./Catalog --clean-output
```

It will report the base URL it picked up. It got that from the `servers` block in the
specification — you did not tell it where the catalog is, and now neither does your code.

## Read what it wrote

Open `Dispatch/Catalog/Models/VendorStanding.cs`.

**Before you look, predict:** the spec named six required fields. How will they appear in
the generated C#, and how will they differ from `expiresOn`, which the spec says is
optional?

<details>
<summary>What it actually generated</summary>

```csharp
public StandingAuthority? Authority { get; set; }
public Date? EffectiveDate { get; set; }
public Date? ExpiresOn { get; set; }
public string? Reason { get; set; }
public Date? ReviewedOn { get; set; }
public StandingStatus? Status { get; set; }
public Guid? VendorId { get; set; }
```

**Every one of them is nullable.** The six required fields and the one optional field are
indistinguishable in the generated code.

`required` appears nowhere. The generator read it, and did not encode it.

</details>

## Use it

Replace **`Dispatch/Program.cs`** with:

```csharp
using Dispatch.Catalog;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Bundle;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddSingleton(_ =>
    new ApiClient(new DefaultRequestAdapter(new AnonymousAuthenticationProvider())));

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapGet("/", () => "dispatch is running");

app.MapGet("/standing/{vendorId:guid}", async (Guid vendorId, ApiClient catalog) =>
{
    var standing = await catalog.Vendors[vendorId].Standing.GetAsync();

    return Results.Ok(new
    {
        status = standing?.Status?.ToString(),
        authority = standing?.Authority?.ToString(),
        reason = standing?.Reason
    });
});

app.Run();
```

There is no `HttpClient`, no base address, no JSON handling and no records. All three of
the tedious roles are filled by code you did not write.

Three vendors:

```bash
curl -s http://localhost:5181/standing/7ff65213-fe55-5938-02e9-35c599b82f4d | jq
curl -s http://localhost:5181/standing/ae50c76e-716a-c769-764a-b92507596344 | jq
curl -s http://localhost:5181/standing/6bbb881d-61c4-06a8-f8c7-9cdcffbe940c | jq
```

<details>
<summary>What you should see</summary>

```json
{ "status": "Approved",  "authority": "ClerkReview",         "reason": "Approved on initial review. Insurance and W-9 on file." }
{ "status": "Suspended", "authority": "VillageOrdinance",    "reason": "Suspended six months pending resolution of certified payroll findings..." }
{ "status": "Debarred",  "authority": "StateDebarmentList",  "reason": null }
```

All three `200`. **Nothing threw.**

The third vendor is Rademacher Traffic Control, debarred by the state, and `reason` came
back `null` — for a field the specification lists as required and types as a plain
`string`.

</details>

## The half of that generator you can't see

Here's the part that matters.

The specification made a promise. The service broke it. **And your generated client did
not notice, because it never encoded the promise in the first place.**

That is not a bug in the generator. It is a policy, and it is a defensible one: a
generated client that trusted `required` would throw on every server that is slightly
wrong, which is most servers. Kiota chose to tolerate anything and hand you the problem.

But notice what that policy cost you here.

**You cannot discover the defect from the generated code.** The one artefact in your
repository that could have recorded "the catalog says this is always present" is the one
that deliberately does not. If you had started here rather than by hand, `reason` being
null would be an ordinary nullable field you null-check without thinking, and you would
never have learned that the published contract is wrong.

**You must now null-check everything**, including `vendorId`, which genuinely is always
there. The generator could not tell you which of the seven are real guarantees, so it
made all seven your problem.

**The policy is not written in your file, or in the spec.** It is a decision made by
somebody who has never seen this API, applied uniformly, and discoverable only by reading
the output.

Two labs, two opposite failures, and it is worth holding them side by side:

| | What the type claimed | What went wrong |
|---|---|---|
| Hand-written (`12-`) | `string Name` — always there | The runtime handed you a null anyway, silently |
| Generated (here) | `string? Reason` — might be absent | Nothing. And you learn nothing |

The hand-written type made a promise it could not keep. The generated one refuses to make
any promise at all. **Neither of them tells you what is actually true**, and only one of
them ever makes you ask.

## Two decisions this leaves you

Neither has a right answer and both are yours.

**Does the generated code go into source control?** It is a build output, so no. It is
code you ship and are responsible for, so yes. Teams split on this, and the split is
usually about whether everyone can run the generator on demand.

**What do you do about `reason`?** You know the spec is wrong. You could write your own
type over the generated one that says what is really true. You could file it with the
Clerk's office. You could do both, and note in your code which of your fields are
enforced by them and which are your own belief.

The one thing not available to you is not knowing, which is where you would be if you had
started with the generator.

## Write the venue note

Open `venues/the-catalog.md` and add this:

```md
## The generated client does not enforce the specification

**The role:** know the shape of what the catalog returns.

**How we cast it:** Kiota, generating from `catalog-openapi.yaml` into
`Dispatch/Catalog/`. Regenerate with
`kiota generate -d ../catalog-openapi.yaml -l CSharp -n Dispatch.Catalog -o ./Catalog --clean-output`.

**Every generated property is nullable, including the six the specification lists as
required.** The generator discards `required` on purpose — a client that enforced it
would fail against any server that is slightly wrong. The consequence for us is that the
generated model cannot tell us which fields are promises and which are hopes, so all of
them are our problem.

Worth knowing because it is the reverse of the mistake we made by hand. A hand-written
`string Reason` claims a guarantee the service does not keep. The generated `string?
Reason` claims nothing, so nothing breaks, and nothing tells us the published contract is
wrong either.

**It is wrong.** `reason` is required in the specification and comes back `null` when
`authority` is `state-debarment-list`. Confirmed against the live service. Any belief we
want to hold about that field has to be written by us, because neither the spec nor the
generated code will hold it for us.

**Where the address comes from:** the `servers` block of the specification, read at
generation time. Nothing in our code names a host, which is good until the spec is
regenerated from a copy with a different one.
```

## Last two questions

**One.** The work-order application has a hand-written catalog client in it, from the
application lab. You now have a generated one that is better at three of the four roles.

Should the application switch? Argue both sides in two minutes, and say specifically what
would be lost — not "control", something you could point at in a diff.

**Two.** The generator turned a wrong specification into code that silently agrees with a
broken service.

Suppose the specification had been right, and the service correct. The generated code
would be identical. Say what that tells you about what a generated client is evidence
for — and what you would have to add to your repository for anyone to be able to tell the
difference six months from now.
