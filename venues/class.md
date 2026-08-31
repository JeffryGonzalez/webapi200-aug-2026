# This class folder

Everything below is where this arrangement **differs from what you'd assume** about a
directory full of code. If you'd have guessed it, it isn't here.

Reference: `../README.md` names the folders. This says what is surprising about them.

## Two codebases, one folder

**The role:** something has to hold both the code you practise in and the code you
actually change.

**How we cast it:** two top-level folders that do not know about each other —
`practice/` and the engagement application.

Worth knowing because they would be separate repositories anywhere else, and they are
side by side here only so you don't finish the week with twenty of them. **Nothing in
one may reference the other, and nothing stops you.** There is no build error waiting
if you try; there is just a boundary that exists because we say so.

If you find yourself adding a reference across it, that is worth a second thought
rather than a compiler message.

## `practice/` is disposable and nothing depends on it

**The role:** something has to be safe to break.

**How we cast it:** a folder nobody delivers, nothing imports, and no grade touches.

Worth knowing because your instincts about not breaking things are correct everywhere
else and wrong here. When a lab says to comment out a line and watch something fail,
that is the lab working. Delete anything you like.

## Labs can only be created, never overwritten

**The role:** something has to get lab code onto your machine without destroying what
you already wrote.

**How we cast it:** your instructor pushes a folder, and the push **refuses if the
folder already exists.** Not "asks first" — refuses.

Worth knowing for two reasons. Nothing you write can be clobbered by a push, so you
never have to save work somewhere else before one. And if you would rather start a lab
over, **delete its folder and ask for it again** — you are the only one who can remove
anything here.

Once you have a folder, you can also pull updates into it yourself, which is how a
correction reaches you after a lab has already been handed out.

## The application is not yours

**The role:** something has to stand in for the codebase you'd actually join.

**How we cast it:** it arrives working, with history you weren't part of, and decisions
made by people who aren't in the room.

Worth knowing because most training code is written in front of you, which is nothing
like the job. Read its `venues/` before you read its source.

## Venue files, including this one

**The role:** something has to tell a newcomer what is different here.

**How we cast it:** short files describing **only** where we differ from what a
competent stranger would assume. Not documentation, not rules — the diff. The
framework's own docs are still the reference.

Worth knowing because they are useful somewhere they don't apply. Back in your own
codebase the question isn't *"do they do it this way?"* — it's **"is this role cast at
all, by anyone, on purpose?"** Different casting is fine. **Nothing cast is a finding**,
and often nobody knows, because the decision got made by whoever typed first.

Some labs will have you add to these. That is the point: you leave with notes you
wrote.

There is a longer piece on all of this at `sidebars/venue-files.md`, if it turns out to
be your kind of thing.
