# Dispatch checks standing

**This one is in the work-order application, not in `practice/`.** Everything you do
here stays in the app, and the village keeps it.

Start it if it isn't running. Docker Desktop first, then:

```bash
dotnet run --project WorkOrders.AppHost
```

The api is on <http://localhost:5171>.

## What we're building

Before work goes to anyone outside the village, somebody has to check that we are
allowed to use them.

That's it.

Read it again and notice what it does **not** say. It says nothing about HTTP, nothing
about the Clerk's office, and nothing about what "allowed" means. Marguerite's letter
says purchasing rules are statutory and that Dale is the one to ask. Everything else is
our problem.

## Look at what happens today

```bash
curl -s -X POST http://localhost:5171/work-orders/2026-0819/dispatch \
  -H 'content-type: application/json' \
  -d '{"vendor":"Rademacher Traffic Control"}'
```

That worked.

Rademacher Traffic Control is on the state debarment list. The village may not use
them, at all, and just did.

Nothing in this codebase is wrong — nobody wrote a check that fails. There simply is no
check.

## The venue, and what isn't in it

Open `venues/` in the work-order app and look for anything about the purchasing
catalog.

There isn't any.

That is worth a moment. Four venue files describe HTTP, .NET, persistence and
boundaries — and the service this feature depends on, which belongs to a different
department and cannot be changed by anyone here, is not mentioned. You will be fixing
that at the end of this lab.

What you need is on the portal instead: **API Catalog → Purchasing & Approved Vendor
Catalog**. Read the standing section before you go on, and download the OpenAPI
specification while you're there.

## The roles

- **find the vendor** somebody typed the name of
- **ask what their standing is**
- **decide whether that standing permits this dispatch**
- **refuse, and say why, if it doesn't**
- **dispatch, if it does**

Five parts. Notice what's not on the list: nothing about caching the answer, nothing
about what to do if the catalog is unavailable, and nothing about who is allowed to
override a refusal. All real questions. Nobody asked for them yet.

Notice also which role is **ours**: the third one. The catalog reports standing; it
does not authorise a purchase. The API catalog page says so in as many words.

## Build the client

Create `WorkOrders.Api/PurchasingCatalog.cs`:

```csharp
namespace WorkOrders.Api;

public class PurchasingCatalog(HttpClient client)
{
    public async Task<VendorStanding?> GetStandingAsync(Guid vendorId, CancellationToken token = default)
    {
        var response = await client.GetAsync($"vendors/{vendorId}/standing", token);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<VendorStanding>(token);
    }

    public async Task<Vendor?> FindVendorAsync(string name, CancellationToken token = default)
    {
        var page = await client.GetFromJsonAsync<VendorPage>(
            $"vendors?q={Uri.EscapeDataString(name)}", token);

        return page?.Items.FirstOrDefault();
    }
}

public record Vendor(Guid Id, string Name);
public record VendorPage(List<Vendor> Items, int Page, int PageSize, long Total);

public record VendorStanding(
    Guid VendorId,
    string Status,
    DateOnly EffectiveDate,
    DateOnly? ExpiresOn,
    string Authority,
    string Reason,
    DateOnly ReviewedOn);
```

Register it in `WorkOrders.Api/Program.cs`:

```csharp
builder.Services.AddHttpClient<PurchasingCatalog>(client =>
{
    client.BaseAddress = new Uri("https+http://catalog");
});
```

And tell the AppHost what `catalog` is. In `WorkOrders.AppHost/AppHost.cs`, above the
api registration:

```csharp
var catalog = builder.AddExternalService("catalog",
    "https://theoria.hypertheory-labs.com/clerk-records/purchasing/");
```

...then add `.WithReference(catalog)` to the api.

<details>
<summary>Two things in there that are easy to get wrong</summary>

**The trailing slash on the external service URL, and the missing leading slash on the
request paths.** `HttpClient` treats a request path beginning with `/` as absolute from
the host root — it throws away the base address's path. So `/vendors/...` against a
base of `.../clerk-records/purchasing/` would ask for `https://host/vendors/...`, and
you would get a `404` that looks like the catalog is broken.

Base ends with a slash, request paths don't start with one.

**`AddExternalService` rather than a URL in configuration.** The catalog is a service
we depend on and do not run. Modelling it in the AppHost keeps the address out of our
code, exactly like `directory` in the earlier lab — the difference being that this one
is somebody else's for real.

</details>

## Make the decision

This is the part that is yours. In `WorkOrders.Api/Endpoints.cs`, in the dispatch
handler, before anything is written: find the vendor, get their standing, and refuse if
it does not permit the dispatch.

The API catalog page tells you what the three statuses mean for purchasing. Read it
again if you need to — the meanings are not guessable from their names.

<details>
<summary>One way to write it</summary>

```csharp
var vendor = await catalog.FindVendorAsync(request.Vendor, token);
if (vendor is null)
    return Results.Problem(statusCode: 422,
        title: $"'{request.Vendor}' is not a registered vendor");

var standing = await catalog.GetStandingAsync(vendor.Id, token);
if (standing is null)
    return Results.Problem(statusCode: 422,
        title: $"No standing on record for {vendor.Name}");

if (standing.Status is not "approved")
    return Results.Problem(statusCode: 422,
        title: $"{vendor.Name} is {standing.Status}",
        detail: $"Effective {standing.EffectiveDate}. {standing.Reason}");
```

`422` rather than `400`: the request was well-formed and we understood it perfectly.
We are refusing it for a reason that has nothing to do with its shape.

`standing is null` is its own case. A vendor can be registered and never reviewed, and
that is not approval.

</details>

## Try all five

```bash
for v in "Dutcher" "Kerns" "Rademacher" "Sable Ridge" "Nonexistent"; do
  echo "--- $v"
  curl -s -X POST http://localhost:5171/work-orders/2026-0819/dispatch \
    -H 'content-type: application/json' -d "{\"vendor\":\"$v\"}"
  echo
done
```

**Write down what you expect for each before you run it.**

<details>
<summary>What you should see</summary>

- **Dutcher** — dispatched. Approved.
- **Kerns** — refused, suspended, with a full explanation of why and since when.
- **Rademacher** — refused, debarred. Look at this one closely.
- **Sable Ridge** — refused. Registered, never reviewed. Not the same as debarred, and
  not approval.
- **Nonexistent** — refused. Not a vendor at all.

</details>

## The half of that refusal you can't see

Here's the part that matters.

Put Kerns and Rademacher side by side:

```
Kerns Excavating is suspended
  Effective 5/19/2026. Suspended six months pending resolution of certified payroll
  findings on the 2025 Depot St. resurfacing.

Rademacher Traffic Control is debarred
  Effective 5/4/2026.
```

**The second one has no reason.** A dispatcher reads that and learns that they may not
use this vendor and nothing about why. If they ring the Clerk's office to ask, somebody
has to go and look it up.

Nothing crashed. Nothing logged an error. You get a sentence with a hole in it.

Now open the OpenAPI specification you downloaded and find `VendorStanding`. `reason`
is in the `required` list and its type is `string` — not `string?`. The document you
were given says that field is always there.

It isn't. The catalog returns `null` for it whenever the standing came from the state
debarment list, because a state debarment carries no village-authored narrative and the
Clerk's office does not write one.

Two things follow, and the second is the one to carry out of here.

**Your `VendorStanding` record declared `Reason` as a non-nullable `string`, and the
runtime handed you a null anyway** — the same thing you met in the practice repo,
against a real service this time, and behind a published specification that promised
otherwise.

**A contract is a claim, not a guarantee.** It was written by people doing their best
about a service that then changed, or never quite did what the document said. Nobody
lied. Checking cost you one request, and the checking is the job.

## Write the venue note that wasn't there

The gap you found at the start. Create `venues/the-catalog.md`:

```md
# The purchasing catalog

Reference: the API catalog on the village portal, and the OpenAPI document it links.

## We depend on a service we do not own

**The role:** something has to say whether a vendor may be used before we commit money.

**How we cast it:** the Clerk's office purchasing catalog, called over HTTP. Registered
as an external service in the AppHost, so its address is not in our code.

**Provenance: decided outside this venue.** Purchasing rules are statutory —
Res. 2026-14 and the state debarment list underneath it. Not a preference of ours, not
negotiable by anyone in this building, and not something to argue about in a pull
request.

We cannot change that service, we cannot see its source, and it can change without
telling us.

## Its specification is wrong about one field

`VendorStanding.reason` is marked required and non-nullable in the published OpenAPI
document. It comes back `null` whenever `authority` is `state-debarment-list`.

Worth knowing because nothing fails when it does — you get a refusal message with the
explanation missing. Treat `reason` as optional regardless of what the document says.

## Standing reports; it does not authorise

The catalog answers *what is this vendor's standing*. Whether that permits a particular
dispatch is our decision, made here, because we are the ones committing the funds.
```

## Last two questions

**One.** The catalog is a service you do not control, and it is now on the path of
every dispatch. What happens to dispatch when the catalog is slow? When it is down?

You know more about this than you did on Monday. Do not build anything — just write
down what you expect to happen, and what you think *should*.

**Two.** You found that a published specification was wrong about a field. Somewhere in
your own work is an API somebody else depends on, with a document describing it.

How would you find out whether that document is still true?
