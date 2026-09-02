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
