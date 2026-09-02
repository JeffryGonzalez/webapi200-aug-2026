# Messaging

Everything below is where this project **differs from what you'd assume**. If you'd
have guessed it, it isn't here.

## NATS is running and nothing uses it

**The role:** something has to carry a message from one service to another.

**How we cast it:** NATS, started by the AppHost.

Worth knowing because **no code publishes to it or listens on it yet.** It has been
provisioned in advance, which is a normal thing to find in a codebase and an odd thing
to notice.

## Neither service references the other

`Orders` and `Crew` are independent. They share `Practice.Contracts`, and that is the
only thing they have in common.

Worth knowing because it is the arrangement that makes messaging worth the trouble. If
one referenced the other you would call a method and be done.

## Ports are pinned

`orders` on 5191, `crew` on 5192, fixed in `launchSettings.json` so instructions can
name a real URL. Only one lab runs at a time.

## Messages are fire-and-forget, and that is a choice

**The role:** something has to carry an announcement from one service to another.

**How we cast it:** Wolverine over core NATS, subject `work-assigned`. The publisher
does not wait and does not learn whether anyone received it.

Worth knowing because delivery is **at most once**. If no service is listening at the
moment a message is published, it is gone — no queue, no retry, no error, and a `202`
returned to the caller either way. A deployment that restarts a listener is enough to
lose one.

That is not a defect. It is what core NATS is for, and it is the right choice when
losing a message costs nothing. When it costs something, the answer is JetStream, or an
outbox, or both — and that is a decision somebody has to make on purpose rather than
discover.

## There is a third service, and it has never been started

**The role:** something has to tell residents their work order was assigned, and put
crew chatter on the break-room wallboard.

**How we cast it:** `Notifications`, registered in the AppHost with
`.WithExplicitStart()`. It appears in the dashboard with a Start button rather than
running when everything else does.

Worth knowing because it has been in the solution the whole time. Nobody has started
it, so nothing it listens for has ever reached it.
