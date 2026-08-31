# Tools on this machine

Everything below is where this environment **differs from what you'd assume** on a
Windows developer machine. If you'd have guessed it, it isn't here.

## The terminal, and what `curl` means in it

**The role:** something has to run the commands in the labs.

**How we cast it:** Windows Terminal running **PowerShell 7**. Git Bash is also on this
image and is fine for most things, but PowerShell 7 is the one to default to.

Two reasons, pointing in opposite directions, which is why the answer is specific
rather than *"whichever you like"*:

- **Windows PowerShell 5.1 — the blue one — aliases `curl` to `Invoke-WebRequest`.**
  Different flags, returns an object rather than text, and lab commands fail there in
  ways that look like the lab is wrong. PowerShell 7 removed that alias, so `curl` is
  the real `curl`.
- **The `aspire` CLI does not work under Git Bash.** It works in PowerShell 7. You are
  unlikely to need it — labs start things with `dotnet run --project ...`, which works
  anywhere — but if you are following along with something your instructor is doing,
  this is why it might behave differently for you.

If a command produces something strange, check which shell you are in before you check
anything else.

## `jq`

**The role:** something has to make a JSON response readable.

**How we cast it:** `jq`, installed on this image, used as `curl -s ... | jq`.

Worth knowing because it is not part of Windows and is not part of .NET — it is a
small tool for slicing JSON, and the labs use it only to pretty-print. If you drop the
`| jq`, every command still works; you just get one long line.

Worth ten minutes of your own time at some point. It is the fastest way to answer
"what does this API actually return" and it is on most servers you will ever log into.

## The class root

**The role:** something has to be a stable place for lab instructions to point at.

**How we cast it:** `c:\Users\Student\class`, always, in every course. Lab text names
paths relative to it without hedging.
