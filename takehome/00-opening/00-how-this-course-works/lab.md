# How This Course Works

No code in this one. You will read four pages on the village's staff portal and write
a few things down.

Everything you need is behind **Employee Login**, under **Contractor Resources**. Your
instructor has the URL; it is also in your class folder. Log in before you start.

Budget twenty to thirty minutes. It is mostly reading and it is worth not rushing —
the rest of the week refers back to it constantly.

## Step 1 - Read the letter

**Contractor Resources → Current Engagement → Letter of Engagement.**

It is from Marguerite Ferraro, the Village Administrator. Read the whole thing,
including the last paragraph, which is the most important one and does not look like
it.

Write down two things:

- What has the village actually contracted for? Three items; she numbers them.
- What is the date, and where does it come from?

## Step 2 - Read the thread behind it

**Contractor Resources → Forwarded — Work Order 2026-0817.**

This is the email chain that produced the letter, reproduced as it arrived. Start at
the bottom — the oldest message — and read upward.

Two questions worth answering before you move on:

- Ted Vosmik says something at the bottom that nobody above him repeats. What is it,
  and how many messages does it survive?
- Who is Kerns Excavating, when did something change about them, and when was work
  sent to them?

<details>
<summary>If you want to check what you found</summary>

Ted says the website report and the phone report are the same hole, and that 0819 is a
different hole. That distinction does not appear in any message above his — by the
time the thread reaches the contractors, three work orders are simply "open."

Kerns was suspended in May. Work was dispatched to them in August. Nobody in the
thread resolves whether that is a problem, and Dale explicitly declines to: he says it
is a question for whoever committed the funds.

</details>

## Step 3 - Find the system in the inventory

**Systems Inventory**, on the portal.

Find the row for **Work Order Intake**. Note its owner, note what its Status says, and
then look at the **Talks to** column.

<details>
<summary>What that column says, and why it matters</summary>

It says **None**.

Ten other systems are listed and most of them say the same thing. That is the actual
condition of the village: eleven systems, almost none of which speak to one another,
and nobody chose it.

That cell is also the shortest description of your engagement available. If it still
says None on Friday, the week did not land.

</details>

While you are there, look at the **Last reviewed** dates on the other rows. They are
worth a moment.

## Step 4 - Find the service that already exists

**API Catalog**, on the portal.

The Clerk's office runs a Purchasing & Approved Vendor Catalog. Read the page, then
open the full API reference and look at one endpoint in particular:
`GET /vendors/{vendorId}/standing`.

Answer for yourself:

- What are the three standing values, and what does each one mean for a purchase?
- The page says the endpoint does *not* authorize a purchase. Who does?

You can also download the OpenAPI specification from that page. You will want it.

## Step 5 - Write down what you think has to be built

Before anyone tells you. Five minutes, on paper, and keep it — you will want to
compare it to Friday.

- What has to exist that does not exist now?
- Which of the three things Marguerite asked for looks hardest, and why?
- What is the riskiest assumption you are currently making?

There is no answer key for this step. Being wrong here costs nothing and is useful;
what matters is that the guess is written down before the week starts arguing with it.

## Step 6 - How this course works

Now the part where we drop the pretence for a minute.

**Theoria is a framing device.** There is no village. Nobody here needs a work-order
system, and we are not going to be evaluated on whether Marguerite is satisfied. What
the village provides is a reason for one mechanism to be a better choice than another,
which is a thing that cannot exist in a tutorial where every exercise has exactly one
correct answer.

**You will be shown more than you need.** Deliberately. Over these three days you will
work through labs on mechanisms that the village's work does not require — several
ways to bind a parameter, several ways to validate a request, several ways to shape a
response. Most of them will not be used on the project.

That is not padding, and it is not disorganisation. **If everything you were taught
turned out to be needed, you would have no way to tell which parts mattered.** A course
where the lesson is exactly the size of the lab teaches you that whatever you were
shown is important, because you were never shown anything else. You would leave able
to do the exercise and unable to make the call.

So the shape of most days is: a stretch of mechanisms in the morning and afternoon,
more than the work needs, and then the next morning we take a short pass at the actual
application — where the interesting part is not typing it in, it is choosing which of
the five things you now know is the right one here, and being able to say why in a
sentence.

**The application work will also contain things nobody taught you.** That is on
purpose too. Work always does.

Two practical notes. The labs are a menu, not a queue — you will not do all of them,
nobody is tracking which ones you did, and there is no prize for finishing. And there
are extra short pieces in your class folder marked **Sidebars**, on things this course
assumes rather than teaches. Nothing depends on your having read one.

## What this generalizes to

You spent twenty minutes on four pages of somebody else's documentation and came out
knowing what the organisation wants, which system is at the centre of it, what it
currently talks to, and which external contract constrains it. Nobody explained any of
it to you.

That is the same first hour on every project any of us joins. The documentation is
partly stale, the requirement is buried several forwards deep and has degraded on the
way up, and the person who actually knows is out on a truck. The skill is not reading
comprehension — it is knowing which four pages to open, and knowing that the thing
somebody said once at the bottom of a thread usually outranks the summary at the top.

It is worth noticing what the expensive information was here. Not the letter, which is
a summary. It was one line from Ted saying two of the reports are the same hole, and
one line from Dale saying a vendor's status changed in May. Both were sitting in a
forwarded email nobody expected you to read closely.

Assume that is always true, and read accordingly.
