# Boundaries

Everything below is what we **have agreed** about which project may reference which. If
you'd have guessed it, it isn't here.

## The shape

- **`WorkOrders.Api`** — intake, work orders, dispatch. Most work happens here.
- **`WorkOrders.Routing`** — routes work to a department. Subscribes; nearly empty.
- **`WorkOrders.Notifications`** — tells residents things. Exists; does nothing yet.
- **`WorkOrders.Contracts`** — types that cross a boundary between those three.
- **`WorkOrders.ServiceDefaults`** — shared configuration, health, telemetry.

## The rule

**Nothing references `WorkOrders.Api`.** It is a deployable, not a library. If Routing
needs a type the Api also uses, that type belongs in `Contracts` — as a decision, when
the second consumer actually turns up, not in anticipation of one.

`Contracts` references nothing.

## Nothing checks any of this

It is a convention held up by whoever happens to be reading the diff, which makes it a
preference rather than a boundary. Tools exist that turn the same rules into build
errors. We do not use one, and that is a decision somebody could revisit.

## Why three deployables for one system

Because they have different reasons to change and different reasons to fail. Routing
being down should not stop intake accepting a pothole report.

It is one repository because they are one thing, they ship together, and they share
types. **The question worth asking is not "microservices or monolith" — it is "what has
to ship together?"** These do.
