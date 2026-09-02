# Aspire

Reference: https://aka.ms/dotnet/aspire

Everything below is where this project **differs from, or would surprise, a developer
who knows ASP.NET but has not used Aspire.** If you'd have guessed it, it isn't here.

## The AppHost runs everything

**The role:** something has to start the services with the right configuration.

**How we cast it:** `Practice.AppHost`. Run that one project and everything comes up.
You never start `Orders` yourself.

There is only one service in this solution. The AppHost is still how it starts, because
that is how every other practice solution starts and because the dashboard is where the
logs are.

## Ports are pinned, and only one lab runs at a time

**The role:** something has to decide which port each service listens on.

**How we cast it:** fixed in `Orders/Properties/launchSettings.json` — `orders` on 5181
— so lab instructions can name a real URL instead of a placeholder.

Aspire would otherwise assign these at random. The cost is that **two labs cannot run
at once**: the second one fails to bind. Stop one before starting another.

## Failure behaviour is already configured

**The role:** something has to decide what a service does when one it calls is slow or
absent.

**How we cast it:** `AddStandardResilienceHandler()` in
`Practice.ServiceDefaults/Extensions.cs`, applied to every `HttpClient`. Retries, a
per-attempt timeout, a thirty-second total request timeout, and a circuit breaker.

Nothing in this solution calls anything, so none of it fires here. It is in the venue
because it is in `ServiceDefaults`, and `ServiceDefaults` is shared.
