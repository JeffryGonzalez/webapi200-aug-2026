# Venue Files

**A short file describing only where a codebase differs from what you'd assume — which makes it the fastest way to stop being confused, and the best context you can hand an assistant.**

## What it is

A `venues/` folder holds one file per *thing that's available to you* — a framework, a
language version, a platform, an agreement the team made. `dotnet-10.md`, `http.md`,
`persistence.md`.

The rule for what goes in one is narrow and it's the whole idea:

> Only write down where we differ from what a competent stranger would assume.
> Everything that matches the average is noise — they were going to do that anyway.

So a venue file is deliberately short and deliberately incomplete. **It is not
documentation and not a rulebook. It's the diff.** The framework's own docs are still
the reference; the venue file is the part the docs can't tell you, because it's about
here.

Entries name the **role** first and then how it's filled: *something has to decide
where another service lives; here it's a service name resolved at runtime, not a URL.*
Written that way an entry is useful even somewhere it doesn't apply — the question
stops being *are they doing it the same way* and becomes *is this role filled at all,
by anyone, on purpose.*

## When you'd reach for it

**Landing in an unfamiliar codebase.** Read `venues/` before the source. Twenty
minutes of reading the diff beats two days of discovering it.

**When something isn't where you're sure it should be.** You know ASP.NET, you run the
project, and `/swagger` is a 404. Nothing errored. The .NET Web API template stopped
shipping Swashbuckle and a Swagger UI — you get an OpenAPI document and no page to look
at it in. That's not a bug and it's not you; it's a different venue, and it's exactly
the kind of thing a venue file exists to say in one line.

**Before asking an assistant about the code.** This is the use that pays fastest.
Pasting the relevant venue file with your question stops it suggesting the thing that's
generally right and locally wrong — the old framework idiom, the library you don't use,
the pattern your team abandoned. It's a few hundred words and it changes the answers
immediately.

## How to tell if you should

Ask what kind of question you actually have.

| Your question sounds like | It's a question about |
|---|---|
| *"Why isn't this the way I know it?"* | **Venue.** Read the file |
| *"How does X work?"* | The docs. Venue files won't help |
| *"Can I even do X here?"* | **Venue.** It's about what's in the set |
| *"Is this a good idea?"* | Neither. That's a conversation |

The first and third are the ones people mistake for being lost.

Worked example, and it's a real one. A student asks: *"Can I point Postman at our
database so I can see whether the POST actually worked?"* That looks like confusion. It
isn't — it's a venue question wearing a bad hat. What they want to know is *what is
available to me for checking that something happened*, and the answer is specific and
useful:

> In .NET you can write a test that makes a real HTTP POST against the running app and
> then reaches into the database directly to check the result — verifying something the
> API itself never exposes. That's available, it's ordinary, and it has a name people
> use: gray-box testing.

Nobody is lost. They asked what's in the set, and the set turned out to contain
something better than the thing they asked for.

## The question it answers best

Not *how does one do X* — the docs have that. **Why do *we* do it this way.**

That question usually surfaces as irritation rather than curiosity. Someone on a
Nest.js project kept having their assistant add a `.env` file, when their organisation
handles configuration through something else entirely. Every session, same suggestion,
same correction. *"So annoying."*

It is annoying. It is also a measurement. **An assistant has no memory of yesterday's
correction and is behaving exactly as any competent stranger would on their first
morning** — which means correcting the same thing repeatedly is a reading of how
undocumented that thing is. The cost is real and it is paid daily, and one file ends it.

Two things worth knowing before you write that file.

**Finishing the sentence is the test.** To write the entry you have to complete *we do
it this way because ___*. Sometimes there is a because — compliance, a platform
constraint, a decision somebody senior made about licensing — and writing it down stops
it costing you. Sometimes there is not, and nobody remembers, in which case the
suggestion you have been overriding for a month may have been trying to tell you
something. *"This is how it is and we no longer know why"* is a legitimate entry and
more useful than silence.

**Say where the constraint came from.** It is the cheapest thing an entry can carry and
the most useful:

- *Chosen here* — a preference. Arguable.
- *Chosen here, expensive to change* — bring numbers.
- *Decided outside this venue* — compliance, platform, org policy. Not yours, and not
  worth anyone's energy.

That last one ends the conversation cleanly, with a colleague or with a tool. It is not
a technical claim, so there is nothing to argue with.

## How it goes wrong

**Treating it as documentation.** It is not complete and it is not trying to be. If it
starts reproducing the framework's own docs, it has failed — and it becomes unreadable
at exactly the length where nobody reads it.

**Treating it as rules.** A venue file says *how it is here*, not *what you must do*.
That distinction matters when you find code that contradicts it, because the honest
first question is whether the file is out of date rather than whether the code is
wrong.

**It goes stale, and this one has teeth.** A venue file is a claim about the present.
It can be wrong, which is its best property — but nothing announces it. The failure
looks like a file that says something confidently and hasn't been true for a year, and
you only find out by checking against the code. Which you can, in about thirty seconds,
which is more than an architecture decision record will ever offer you.

**Writing down everything.** The instinct on a new project is to document the stack.
Resist it. If a competent stranger would have guessed it, leaving it out is what keeps
the rest readable.

## Try it

Two minutes, on a project you actually work on.

Write three lines under the heading *where we differ from what a competent stranger
would assume.* Not more than three.

<details>
<summary>What to do if you can't think of any</summary>

That is a result, and an interesting one. Either your project genuinely is the
default — rare, and worth knowing — or the deviations are so familiar to you that
you've stopped seeing them as deviations.

The test for the second: hand your `Program.cs` to someone who has never seen it and
ask what surprised them. Their first three reactions are your first three lines.

The other useful result is discovering that you don't know *why* one of them is the
way it is. That question has an answer and somebody on your team has it, and it is
usually the most interesting thing you'll learn that week.

</details>

## Ask about

**Verify the model.** *"I think a short file describing only where a project differs
from framework defaults is more useful than a full architecture document, because it
can be checked against the code and a decision record can't. Is that right, or are
there things a decision record captures that a description of the current state
genuinely loses?"*

**Break it.** *"Give me two concrete cases where writing down 'how things are here'
instead of 'why we decided this' caused a real problem later — where losing the
reasoning mattered more than having a checkable description."*

**Map around it.** *"Here is a venue file for my project. What questions would you
still have before writing code in this codebase — what did I leave out that you'd
actually need?"*

That third one is worth running for real. It tells you what's missing by having
somebody try to use it.

## If you remember one thing

It's the diff, not the docs. If a competent stranger would have guessed it, it doesn't
belong in there.
