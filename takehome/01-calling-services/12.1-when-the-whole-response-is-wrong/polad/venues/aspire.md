# Aspire

Reference: https://aka.ms/dotnet/aspire

Everything below is where this project **differs from, or would surprise, a developer
who knows ASP.NET but has not used Aspire.** If you'd have guessed it, it isn't here.

## The AppHost runs everything

**The role:** something has to start the services, in the right order, with the right
addresses.

**How we cast it:** `Practice.AppHost`. Run that one project and everything comes up.
You never start `Orders` or `Directory` yourself.

## Services are named, not addressed

**The role:** something has to know where another service is.

**How we cast it:** a service *name*, resolved at runtime — `https+http://directory`.
Nothing in the code knows a port.

Worth knowing because this is unusual. Most projects put a URL in configuration; here
the AppHost knows the addresses and the code only knows names.

## Ports are pinned, and only one lab runs at a time

**The role:** something has to decide which port each service listens on.

**How we cast it:** fixed in each project's `launchSettings.json` — `orders` on 5181,
`directory` on 5182 — so lab instructions can name a real URL instead of a placeholder.

Aspire would otherwise assign these at random. The cost is that **two labs cannot run
at once**: the second one fails to bind. Stop one before starting another.

## Failure behaviour is already configured

**The role:** something has to decide what a service does when one it calls is slow or
absent.

**How we cast it:** `AddStandardResilienceHandler()` in
`Practice.ServiceDefaults/Extensions.cs`, applied to every `HttpClient`. Retries, a
per-attempt timeout, a thirty-second total request timeout, and a circuit breaker.

Worth knowing because it is invisible at the call site and it is doing a lot. A dead
dependency produces a thirty-second hang and then a `500` — not an immediate error —
and nothing in the calling code says so.

## The directory service can be told to misbehave

**The role:** something has to stand in for a service you do not control, including on
the days it is not working.

**How we cast it:** `Directory` has a `POST /mode/{value}` endpoint. `ok` is the normal
list. `empty` returns `204`, `html` returns a gateway error page with a `200`, `object`
returns valid JSON of the wrong shape, `null` returns the literal `null`, and
`emptylist` returns `[]`.

`GET /mode` says which one is current. It resets to `ok` when the service restarts.

Worth knowing because **nothing outside a practice repository should have an endpoint
like this.** It exists so you can see what your own code does when the other end
answers badly, which is otherwise a thing you find out in production.
