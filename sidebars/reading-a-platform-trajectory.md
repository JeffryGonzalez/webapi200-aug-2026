# Reading a Platform's Trajectory

**Two supported ways to do something are rarely equally supported, and the difference is visible before it hurts you.**

## What it is

Large platforms — .NET, Angular, anything with a vendor behind it — routinely ship a
new way to do something alongside the old way, and support both. Officially, both are
fine. Nothing is deprecated. Nothing warns you.

But attention is finite, and over a few releases it concentrates. One of the two
starts getting the new capabilities and the other starts getting only the fixes needed
to keep it working. That divergence is observable long before anything is announced,
and reading it is a normal part of choosing between two things that both work today.

## When you'd reach for it

When you are picking between two supported approaches and the technical comparison
comes out roughly even — which is most of the time, because if one were plainly better
the other would already be gone.

Also when you inherit a codebase built on the older of two options and have to decide
whether that is a problem, a project, or nothing.

## How to tell if you should

Five checks, all public, none requiring insider knowledge. Run them on both options.

| Check | What you are looking for |
|---|---|
| **New features** | Do capabilities announced in the last two releases work in both? Or does the older one appear in the "not supported for" list |
| **Templates** | What does the default project template use when you run the CLI with no arguments |
| **Documentation order** | Which one does the getting-started page lead with. Which is filed under "also supported" |
| **Release notes** | Count the paragraphs. Attention is measurable in words |
| **Fixes or features** | The discriminator. Is the older thing receiving *corrections*, or *capabilities* |

**That last row decides it.** A thing receiving fixes is being **maintained** — kept
working, indefinitely, by people doing their jobs. A thing receiving features is being
**invested in**. Both are supported. They are not the same bet.

If four of five point the same direction, you are not guessing any more.

## How it goes wrong

**It reads as cynicism if you skip the evidence.** *"Microsoft always does this"* is a
sentiment. *"This feature shipped in the last two releases and only works in one of
them"* is a fact. Bring the second to a design discussion; the first loses the room
and deserves to.

**It is probabilistic, and it is sometimes wrong.** New alternatives get abandoned as
well as promoted. The signal is real and it is not a guarantee, and anyone claiming
certainty here is selling something.

**"Not invested in" is not "broken".** This is the misread that does actual damage —
someone runs these checks, concludes the old thing is dying, and proposes rewriting a
working system. It will keep working for years. What changes is that the cost of
staying rises slowly: each new capability you cannot use, each sample you have to
translate, each answer that assumes the other thing.

**The cost is invisible and deferred**, which is why it needs a check rather than an
alarm. Nothing will ever tell you that the gap widened this quarter. You will notice
it as a slowly increasing friction that feels like the framework getting worse, and it
is not — you are standing still while it moves.

## Try it

Pick any two supported alternatives in a platform you use. Run row one and row five.

```bash
# What does the default template actually produce?
dotnet new webapi -o /tmp/trajectory-probe
cat /tmp/trajectory-probe/Program.cs
rm -rf /tmp/trajectory-probe
```

<details>
<summary>What the template tells you, and what it does not</summary>

The default is a statement of what the vendor wants a new project to look like. That
is genuinely informative — it is the recommendation with nobody's opinion in the way.

What it does not tell you is whether the other option is in trouble. Templates change
slowly and conservatively, and a template that has not changed is weaker evidence than
release notes that have.

</details>

## Ask about

**Verify the model.** *"I think a platform feature receiving only bug fixes while its
alternative receives new capabilities is a signal that investment has shifted, even
when both are officially supported. Is that a sound way to read it, or are there
common cases where a mature thing legitimately stops changing because it is finished?"*

**Break it.** *"Give me two concrete historical cases where the newer alternative in a
major platform was abandoned and the older one remained the right choice. What was
visible at the time that should have warned someone off the new thing?"*

**Map around it.** *"Beyond release notes and templates, what other public signals
indicate where a platform vendor is putting engineering effort — governance changes,
repository activity, conference content, hiring? Which are reliable and which are
noise?"*

## If you remember one thing

Maintained and invested-in are both called supported, and only one of them is going
somewhere.
