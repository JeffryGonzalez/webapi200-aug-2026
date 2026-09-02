# The purchasing catalog

Reference: <https://theoria.hypertheory-labs.com/clerk-records/purchasing>

## It is not ours and it is not in this solution

**The role:** something has to say whether a vendor may be paid.

**How we cast it:** the Clerk's office runs it. We call it over the internet. It is not
started by our AppHost, we cannot stop it, and we do not have its source.

Worth knowing because everything else in this practice repository is a service you can
restart. This one is not, and that changes what "it is broken" means.

## There is a published specification, and it is in this repository

`catalog-openapi.yaml`, at the root. A copy, taken from the Clerk's office.

Worth knowing because it is the only statement anyone has made about what the catalog
returns. It is also **a document, not a program.** Nothing enforces it — not on their
side, and not on ours unless we make it.

## Ports and addresses

`dispatch` is pinned to 5181 in `launchSettings.json`. The catalog's address is a real
URL and is written in the specification's `servers` block, which is where a generated
client will find it.
