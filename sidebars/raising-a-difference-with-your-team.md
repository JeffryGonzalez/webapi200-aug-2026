# Raising a Difference With Your Team

**You have found a gap between what you learned and what your team's guidance says. Being right about it is a separate skill from raising it well, and only one of those changes anything.**

## What it is

Some document at work — an ADR, a wiki page, a linter config, a thing a senior person
said in 2021 that became true by repetition — says to do something one way. You now
have reason to think another way is better, or at least that the question is open.

Raising that is a normal part of the job and it goes badly more often than it needs
to, usually for reasons that have nothing to do with whether you are right.

## When you'd reach for it

After you have checked, privately, and still think there is something there. Not
before — a difference you have not investigated is a question for yourself first.

Also when you are new somewhere. The window where "I am still learning how things work
here" is literally true is the cheapest time to ask anything, and it closes.

## How to tell if you should

Three questions, in order. If you stall on any of them, the answer is *not yet*.

**1. Is it causing harm, or is it just not how you would do it?**

There is a real difference between *this produces a failure mode I can name* and *this
is not my preference*. The second one is fine to hold quietly. Most divergences are the
second one and raising them spends credibility you will want later.

**2. Can you state the evidence without stating the conclusion?**

If you can say *"I hit X, and Y took three hours"* you have something. If the strongest
version is *"the modern approach is Z"*, you are appealing to an authority nobody in
the room can check, and it will be treated that way.

**3. Do you actually not know the answer?**

If you are asking a question you have already answered, everyone can tell, and the
conversation becomes about that instead. A real question gets a real answer; a
rhetorical one gets a defence.

### The reframe that is almost always available

**A decision has a context, and contexts expire.** You are not saying the decision was
wrong. You are asking whether the conditions that produced it still hold.

> *"This was decided when we were on one service and a different framework version. Do
> the reasons still apply, or is it worth a look?"*

That is not a diplomatic trick. It is usually just true, it costs the original decider
nothing, and it turns a challenge into a maintenance question — which is a category
your team already knows how to process.

## How it goes wrong

**Leading with where you got it.** *"Claude says…"*, *"I read that…"*, *"in the course
I took…"* — all three hand your credibility to something not in the room, and invite
an argument with an absent third party instead of a conversation with you. Bring the
evidence; leave the source out unless asked.

**"Best practice."** It appeals to an authority nobody can inspect, and everybody has
met somebody who used it to win an argument they should have lost. It reads as *I do
not have a reason*.

**Raising it in public first.** A channel with forty people in it turns a question into
a position somebody now has to defend in front of their colleagues. One person, first,
almost always — ideally whoever wrote the thing.

**Bundling.** Attaching it to two unrelated complaints converts a specific question
into a general grievance, and it will be answered as one.

**Hedging everything.** The overcorrection, and it fails differently: a paragraph of
qualifiers reads as either weakness or passive aggression, and it obscures what you
actually observed.

The shape that works is neither: **be confident about what you observed and tentative
about what should change.** That is not a tone trick — it is an accurate description of
what you actually know. You *did* hit the thing. You *do not* know their full context.

## Try it

Take a divergence you are actually sitting on and write two sentences: what you
observed, and what you are asking. No conclusion, no recommendation.

Then, if you want a second read on how it lands:

<details>
<summary>A prompt worth having</summary>

> *"Here is a message I am about to send to a senior colleague about a team guideline I
> think might be out of date. Tell me how it is likely to read to someone who wrote
> that guideline and is proud of it. Where does it sound like I have already decided?
> Where am I hedging so much that the point disappears?"*

Note what that asks for: **how it lands**, not a rewrite. A rewritten message is not
yours and will not sound like you, which people notice. What is useful is the read.

</details>

## Ask about

**Verify the model.** *"I think the strongest way to question an existing team decision
is to ask whether its original context still holds, rather than whether the decision
was correct. Is that generally true, or are there situations where it comes across as
evasive and being direct would work better?"*

**Break it.** *"Give me two concrete examples where framing a challenge as 'has the
context changed' would have backfired — where it read as passive-aggressive or as
avoiding the real disagreement. What was different about those situations?"*

**Map around it.** *"Beyond the framing of a single message, what else determines
whether a technical objection gets taken seriously in an engineering org — timing,
who you raise it with, whether you have a prototype, tenure? Which of those can
someone relatively new actually control?"*

## If you remember one thing

Be confident about what you observed and tentative about what should change. You did
hit the thing; you do not know their whole context. Say exactly that much.
