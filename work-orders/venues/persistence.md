# Persistence

Reference: https://martendb.io (IDocumentSession comes from here)

Everything below is where we **differ from what a developer who has used an ORM would
assume.** If you'd have guessed it, it isn't here.

## Documents, not tables

**The role:** something has to store a work order and get it back.

**How we cast it:** Marten, on Postgres. A `WorkOrder` is serialised whole and stored
as JSON in a table Marten manages. There is no mapping to write and no migration to
run — the schema follows the class.

Worth knowing because it changes what a "change to the model" costs. Adding a property
is a property; there is no second place to update.

## Sessions, and when anything is written

**The role:** something has to decide the unit of work.

**How we cast it:** `IDocumentSession` for writing, `IQuerySession` for reading, both
injected. **Nothing is written until `SaveChangesAsync()`** — `Store()` only queues it.

`UseLightweightSessions()` means no identity map and no automatic change tracking: if
you load a document, change it, and do not `Store()` it, nothing happens. That is a
deliberate choice for a service that mostly reads.

## What is not a document

Work orders are documents. Nothing else is, yet. If something starts to feel like it
wants a row and a foreign key, that is worth a conversation rather than an assumption
— Marten does relational things perfectly well and this codebase does not do them
anywhere, so you would be the first.
