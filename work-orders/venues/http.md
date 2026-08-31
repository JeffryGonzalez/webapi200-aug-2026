# HTTP

Reference: the RFCs, and https://developer.mozilla.org/en-US/docs/Web/HTTP

Everything below is where we **differ from, or would surprise, a developer who knows
HTTP but hasn't worked in this codebase.** If you'd have guessed it, it isn't here.

## The terrain

HTTP is request and response. One goes out, one comes back, and the shape of what comes
back is the contract.

Two things follow from that which are worth naming, because most of the entries below
are consequences of one or the other:

- **The response is the only channel.** Anything the caller needs to know — that it
  worked, that it didn't, what to do next — is in the status, the headers, or the body.
  There is nowhere else to put it.
- **The processing between request and response is not always finished when the
  response goes out.** HTTP does not mind. Callers mind a great deal, and how you tell
  them is a decision somebody makes, deliberately or otherwise.

## Errors

**The role:** something has to tell a caller what went wrong in a form they can act on.

**How we cast it:** `application/problem+json`, RFC 9457, everywhere. `Results.Problem`
rather than a hand-built object.

Worth knowing because it is uniform — every failing endpoint in this codebase returns
the same shape, including the ones you have not read. A caller writes one error path.

We do not add fields to it casually. It is an interoperable format and the value is in
other people's tooling recognising it.

## Work that isn't done when the response goes out

**The role:** something has to tell a caller that we accepted their request but have
not finished acting on it.

**How we cast it:** `202 Accepted`, with a `Location` header pointing at somewhere the
caller can look. Not `200` with a body claiming success that has not happened yet.

Worth knowing because the alternative is silent and common: return `200`, do the work
in the background, and the caller believes something that is not true yet. If it then
fails, nobody is listening.

Note what this costs. A `202` means the caller has to come back, which means there has
to be somewhere to come back *to*, which is a second endpoint that would not otherwise
exist. That is the trade and it is a real one.

## Partial updates

**The role:** something has to let a caller change part of a resource without sending
all of it.

**How we cast it:** `PATCH` with a merge-shaped body of our own — send the fields you
want changed, omit the rest. **Not JSON Patch (RFC 6902)**, and not JSON Merge Patch
(RFC 7386) exactly, though ours is closer to the second.

Worth knowing because the name `PATCH` on the method does not tell you which of these
you are looking at, and the three behave differently for the case that matters:
distinguishing *set this field to null* from *leave this field alone*. Read
`docs/patch.md` before writing a client.

## Versioning

**The role:** something has to let this API change without breaking callers who have
not changed.

**How we cast it:** nothing, currently. There is one caller and it is us.

Worth knowing because it is not an oversight and it is not a decision that will hold.
The first external caller makes this urgent, and the cheapest moment to pick a scheme
is before that happens rather than during.

## What we don't do

Constraints are just what is not in the set, so this section is short and everything in
it is a thing somebody has reasonably expected:

- **We don't use HTTP verbs beyond GET, POST, PUT, PATCH, DELETE.** Nothing against
  them; nothing here needed one.
- **We don't negotiate content types.** JSON, in and out. An `Accept` header asking for
  XML gets JSON and does not get an error, which is arguably wrong and has not mattered.
