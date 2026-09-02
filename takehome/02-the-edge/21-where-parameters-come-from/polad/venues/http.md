# HTTP

How this codebase handles requests and what it promises callers.

## We use minimal APIs

**The role:** something has to turn an HTTP request into a call into our code.

**How we cast it:** endpoints declared with `app.MapGet` and friends, in
`Orders/Program.cs`. Handlers are functions.

Worth knowing because of the consequence: **a handler's parameters are the request.**
There is no request object to reach into and no model to bind onto. Whatever a handler
declares, something has to work out where each one comes from.

## Errors are problem+json

**The role:** something has to tell a caller what went wrong in a form they can rely on.

**How we cast it:** `Results.Problem` and `Results.ValidationProblem`, so every failure
we return is `application/problem+json` with the same fields in it.

Worth knowing because it is a promise to callers, not a preference. One error path,
written once, works for every endpoint here.

Uniform error shapes are the kind of claim worth checking rather than trusting. If you
find a response from this service that is *not* problem+json, that is a finding, and it
belongs in this file.

## The work orders live in memory

**The role:** something has to hold the data.

**How we cast it:** `Orders/WorkOrders.cs`, a singleton with a list in it. Seeded at
startup, gone when the process stops.

Worth knowing because it is not a database and is not pretending to be one. It is here
so the labs have something to page through. Nothing about how it stores work orders is
a recommendation.
