# Background work

Everything below is where this project **differs from what you'd assume**. If you'd
have guessed it, it isn't here.

## Notifying a resident is slow, and the caller waits for it

**The role:** somebody who files a work order has to be told it was received.

**How we cast it:** a call inside the request handler, before the response is
returned.

Worth knowing because the notification goes over somebody else's network and takes
about three seconds. Every caller pays that, including the ones who never look at the
response.

## There is no queue and no broker

Nothing is provisioned. This is one process, and anything that happens later has to
happen inside it.

Worth knowing because it is the ordinary starting condition. Reaching for a broker is
a decision with costs, and it is not the only way to stop making the caller wait.

## Ports are pinned

`intake` on 5193, fixed in `launchSettings.json` so instructions can name a real URL.
Only one lab runs at a time.
