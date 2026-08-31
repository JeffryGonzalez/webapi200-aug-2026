# .NET and Aspire

Reference: https://learn.microsoft.com/dotnet — https://aka.ms/dotnet/aspire

Everything below is where this project **differs from, or would surprise, a developer
who knows ASP.NET but has not worked here.** If you'd have guessed it, it isn't here.

## Minimal APIs, not controllers

**The role:** something has to turn an HTTP request into a call into our code.

**How we cast it:** minimal APIs. Handlers are functions; their parameters are the
request.

Not a claim that controllers are wrong. They work, plenty of good services use them,
and if your team has standardised on them you should not go home and start a fight
about it.

## The AppHost runs everything

**The role:** something has to start the services and the infrastructure they need.

**How we cast it:** `WorkOrders.AppHost`. Run that one project and you get the API, the
two subscriber services, Postgres and NATS. You never start any of them yourself, and
nothing in the code knows a connection string or a port.

Aspire is a **development** tool here. It is not how this would be deployed.

## Ports are pinned

`api` on 5171, `routing` on 5172, `notifications` on 5173, fixed in each project's
`launchSettings.json` so instructions can name a real URL. Aspire would otherwise
assign them at random.

## Postgres keeps its data between runs

**The role:** something has to decide whether restarting throws your work away.

**How we cast it:** a named Docker volume, so it does not. Stop the AppHost, start it
again, your work orders are still there.

Worth knowing because the seed only runs when the database is empty. If you want the
original five work orders back, delete the volume and restart — that is the reset
button, and it is deliberate that it takes a decision rather than a restart.
