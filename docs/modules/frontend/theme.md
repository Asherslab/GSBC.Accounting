---
title: The theme, and why there is no component library
kind: reference
status: current
module: frontend
verified: 2026-09-01
code:
  - GSBC.Accounting.WASM/wwwroot/css/app.css
  - GSBC.Accounting.WASM/wwwroot/index.html
  - GSBC.Accounting.WASM/Layout/MainLayout.razor
  - GSBC.Accounting.WASM/App.razor
  - GSBC.Accounting.WASM/Program.cs
  - GSBC.Accounting.WASM/Pages/Home.razor
  - GSBC.Accounting.WASM/Features/Expenses/Components/SectionRail.razor
  - GSBC.Accounting.WASM/Features/Expenses/Pages/DraftsPage.razor
  - GSBC.Accounting.WASM/Features/Expenses/Components/LineTable.razor
  - GSBC.Accounting.WASM/Features/Expenses/Components/ComplianceSection.razor
  - mockups/debit-card-purchase-form.html
---

# The theme, and why there is no component library

Read this before styling a page or reaching for a UI package. It says where the CSS came from, the one
rule that keeps it honest, what the mockup does not cover, and the traps that have already bitten once.

## `app.css` is a lift, not an interpretation

`GSBC.Accounting.WASM/wwwroot/css/app.css` is the `<style>` block of
`mockups/debit-card-purchase-form.html`, moved across close to verbatim. The mockup was built from the
`.docx` and approved as the shape to aim at, so **when the two disagree at desk width, the mockup is
right and the CSS is the bug.** Change a token only by changing the mockup first.

Blocks that are not from the mockup are marked as such in the file. There are four: the Blazor shell
styles (the boot spinner in `#app` and the `#blazor-error-ui` strip), because a static page has no boot
phase; the `.layout.norail` fix under "The shell" below; the `.topbar h1` focus rule under "Traps"; and
the whole mobile layer, which is the next section.

## The mobile layer is this app's, not the mockup's

The mockup was drawn at desk width and carries one breakpoint — the `.grid` columns collapsing at
900px. Everything under `/* phones and small tablets */` in `app.css` was written here, against a real
390px viewport, and it is **not** subordinate to the mockup: below 900px the mockup has no opinion, so
there is nothing to disagree with. Above 900px the rule above still holds unchanged.

It is one breakpoint, `max-width:900px`, deliberately the same one `.grid` already used. A layout that
changes shape at two widths has a state in between that nobody ever looks at.

Four things in the desk design are broken rather than merely cramped on a phone, and each is fixed
rather than worked around:

| What | Why it is not cosmetic |
|---|---|
| The 238px rail | 61% of an iPhone's width. It becomes a horizontal scrolling stepper pinned to the top, and **the topbar gives up `position:sticky`** so the two do not both eat the viewport. |
| The line table | ~990px of column `min-width`s. Sideways scrolling inside a form is a trap on touch — a swipe meant for the page moves the table — so each row becomes a labelled card. |
| Borderless table fields | `td input` shows its border on `:hover`. A touch screen never hovers, so the whole table would be invisible fields. They get the border back. |
| 14px inputs | iOS Safari zooms the page when a field under 16px takes focus, and no gesture puts it back. **16px on mobile is a functional requirement, not a preference.** |

Two things this layer needs from outside the stylesheet, both of which break silently if removed:

- **`data-label` on every `<td>`, and `class="rowdel"` on the remove cell.** The stacked row card reads
  its field labels from `data-label`, because the `<thead>` they would otherwise come from is hidden. A
  cell without one renders as a field with no label on every phone, and nothing errors. `rowdel` is
  what lifts the remove button out of the label/field flow and onto the row card's corner.
  `.tablewrap.lines` captions those cards "Line 1" instead of "Row 1".
- **`viewport-fit=cover` in `index.html`.** It is the only thing that makes `env(safe-area-inset-*)`
  report anything but `0`. The action bar reads the bottom inset so its primary button clears the
  iPhone home indicator; without the attribute the inset is `0`, the button sits under the swipe bar,
  and the first tap dismisses the app instead of submitting the form.

**The action bar is not pinned below 900px.** `position:fixed` and the on-screen keyboard fight each
other: iOS shrinks the visual viewport but not the layout viewport, so a bottom-pinned bar either sits
behind the keyboard or floats halfway up the screen over the field being typed into — and on a form
this long the keyboard is up for most of the visit. It becomes the last block on the page instead,
which is where somebody who has finished a form is looking anyway. Nothing is lost by it: section 3's
`.totals` panel already carries the running figures where the lines are.

At desk width it stays fixed. There is no keyboard to fight there, and the form column is short enough
that the bar is never far from what is being filled in.

`.cardlink` is mobile-only and does nothing at desk width. A bare text link is a ~20px tap target, and
these are the only way into a form and the only way to resume a draft.

**Two other controls need the same treatment and are easy to miss**, because both live in the page body
rather than in the action bar that already sizes its own buttons:

- `main button.b` — the drafts page's Discard and its confirm pair. The base `.b` is 36px tall.
- `.crumbs a` — padded to 44px with an equal negative margin, so the touch area grows without the strip
  getting taller. Measured at 390px: 9px of padding left it at 39px, 12px gets it to 45px.

Measured at 320px as well as 390px, both pages free of horizontal overflow. The form page overflows its
viewport by 4px at 320px — that is the rail stepper's `width:max-content` inside its own scroll
container and predates the breadcrumb; it is unchanged with the crumb removed.

### What the keyboard does

Two attributes that are only about a phone, and both are markup rather than CSS:

- **`inputmode="decimal"` on every enabled `type="number"`.** iOS puts up the numbers-and-punctuation
  keyboard for `type=number` and a clean numeric pad for this. The two disabled fields in
  `ApprovalSections` are left alone — they never take a keyboard.
- **`autocomplete` only on the fields that are genuinely about the person** — `name` on both forms'
  claimant, `tel` on the reimbursement form's phone/email. Ministry, supplier and event are not
  autofillable and must not claim to be. **Nothing carries a card token.** The only card field on the
  debit card form is the last four digits, and `cc-number` would invite the browser to fill a full card
  number into a form whose own notice says it must never hold one.

Phone/email stays `type="text"`: it takes either, so `type=email` would reject a phone number and
`type=tel` would raise the wrong keyboard for an address.

## No component library — GSBC.ImpactKids uses MudBlazor and this app does not

This is the one place the two apps' frontends diverge on purpose.

MudBlazor brings its own token layer, its own density scale and its own opinion about what an input
looks like. The mockup is a hand-rolled system whose look is the point: a form that replaces a paper
document has to read as that document, and every surface would have to be overridden back. The
override, not the design, would become the thing being maintained.

What follows: pages are plain markup against the classes in `app.css` — `.card`, `.grid`/`.c6`, `.f`,
`.yn`, `.checkline`, `.tablewrap`, `.totals`, `.drop`, `.notice`, `.badge`, `.actionbar`. Add a class
to `app.css` rather than a package.

## The three theme states

The viewer's theme has **three** states, not two:

| State | What is on the root element | What decides the palette |
|---|---|---|
| Explicit dark | `data-theme="dark"` | `:root[data-theme="dark"]` |
| Explicit light | `data-theme="light"` | bare `:root` |
| System (the default) | nothing | `@media (prefers-color-scheme: dark)`, guarded `:root:not([data-theme="light"])` |

**A colour whose only definition sits inside a media query disappears in one of the three.** The full
light palette is on bare `:root`; the dark blocks redefine only what changes.

`color-scheme` is declared in all three alongside the custom properties, and it is not decoration: it
is what the browser paints its **own** surfaces from — the date picker panel, the select dropdown,
scrollbars, the caret. Left unset they stay light, so a dark form opened a white date picker.

## The toggle, and why it is the only JavaScript here

The topbar carries a button that cycles the three states in order: **system → light → dark → system**.
It is in `MainLayout`, so it is on every page — the mockup's other two topbar buttons are not here,
because Print belongs to a page (and the artefact that matters is the server-rendered PDF, not the
browser's print view) and Submit belongs to the form's action bar next to the totals it acts on.

The choice persists in `localStorage` under `gsbc.theme`, and **"system" is stored as the absence of
the key**, not as the string `"system"`. The attribute is then removed rather than set, leaving
`prefers-color-scheme` as the only signal — which is exactly the third row of the table above.

**The logic is an inline `<script>` in `index.html`, and that is not laziness.** It has to stamp
`data-theme` on `<html>` *before the first paint*, and a Blazor component cannot: WASM boots long after
the first frame, so anyone whose stored choice differs from their device setting would get a flash of
the wrong theme on every cold load — behind the boot spinner, where it is at its most obvious. So the
script must be in `<head>`, synchronous, with no `defer` or `async`. Nothing else in this app justifies
a script; this does.

`MainLayout` owns only the glyph. It reads `gsbcTheme.get()` in `OnAfterRenderAsync`, because the
stored value affects nothing but which of the three icons is drawn — the page is already in the right
colours by then — and `gsbcTheme.cycle()` returns the new mode so the click needs no second read.

The glyph names the state the theme **is in**, not the state the button would move to. Either reading
is defensible from an icon alone, so the `title` and `aria-label` spell out both halves: "Theme: light.
Switch to dark."

Two things that follow, and both bite silently:

- **`<meta name="theme-color">` is a single tag with no `media`, written by the script.** The media
  form — one tag per scheme — cannot see an explicit choice, so it goes on tinting the iOS Safari
  toolbar to the *device* scheme after somebody has switched the page to the other one.
- **Every `localStorage` access is wrapped in `try`/`catch`.** It throws outright in some contexts
  (private windows, third-party-cookie blocking), and a theme preference is never worth breaking a page
  over. The theme is now the app's **only** use of `localStorage` — drafts moved to the server, see
  [../expenses/drafts.md](../expenses/drafts.md) — and it is the right thing to keep there: it is a
  per-browser display preference nobody needs to recover.

## Traps

**The dark palette is written out twice and the two copies must stay byte-identical.** Plain CSS gives
no way round it — an at-rule block and a plain selector cannot share a declaration list — so
`@media (prefers-color-scheme: dark) :root:not([data-theme="light"])` and `:root[data-theme="dark"]`
each carry the whole thing. A value added to one and not the other produces a page that is right until
somebody touches the theme toggle, and then is wrong in a way nobody reproduces.

The mockup had already drifted here: `--line-strong` was declared twice inside the media block,
`#464a53` then `#4a4e57`, and only the second matched the explicit-dark block. The dead one is deleted
in `app.css`; the mockup still has it.

**`<FocusOnNavigate Selector="h1">` in `App.razor` draws a focus ring on the topbar title.** Every
navigation, including the first paint, moves focus to the page heading, and the `:focus-visible` rule
then outlines it — so the app opens with what reads as a stray selection box around its own title, and
on a phone that is the first thing on screen. `.topbar h1:focus` clears the outline. Nothing is lost:
`FocusOnNavigate` gives the element `tabindex="-1"`, so it is reachable only programmatically and never
by Tab, and the focus move still happens and is still announced.

**The `@media print` block is not the PDF.** It is the browser's print view, kept because the mockup
had it. The artefact that matters is rendered server-side with QuestPDF from the submission aggregate,
deliberately, so the screen layout and the printed layout are free to diverge — including appending the
receipts as pages, which no print stylesheet can do.

## The shell

`MainLayout.razor` renders only the topbar: the `GS` mark, the page title, the church/finance subtitle
and the theme button. It does **not** render the `.layout` grid — the section rail in the mockup's left column is
per-page, because each form owns its own section list and the landing page has none. A page therefore
opens its own `<div class="layout">`.

**A page with no rail must say `class="layout norail"`.** `.layout` is a two-column grid and the first
column is the 238px rail; a page that renders `<main>` with no `nav.rail` before it puts main in the
rail's slot, so every card is squeezed to 238px with the rest of the window empty. Nothing errors and
the styling still looks correct — it reads as "the design is just narrow", which is why it survived
the first run of the app on 2026-08-31 without being noticed. `.layout.norail` collapses the grid to
one column.

### The breadcrumb is derived, not declared

`MainLayout` also renders a two-level breadcrumb above `.layout` on every page except the landing page,
and **which page is which is worked out from the URL** — no page opts in.

That is the point. A page that had to call something to get its way back would eventually be a page
that forgot, and the failure is invisible in review: the crumb is simply absent, on the one screen where
somebody is looking for the way out. The second level reuses whatever the page passed to `SetTitle`, so
the two cannot drift.

Two things it needs to keep working:

- **`Nav.LocationChanged` is subscribed, and the layout unsubscribes on dispose.** A navigation
  re-renders `@Body` and not the layout, so without the subscription the crumb is right on first load
  and stale for the rest of the session — including on the trip back to the landing page it exists to
  offer.
- **It is not sticky, at any width.** The topbar already is, and on a form the rail is too below 900px.
  A third pinned strip leaves a phone reading the form through a letterbox. The way home is wanted once,
  at the moment somebody decides to leave.

The separator is a `::before` pseudo-element so a screen reader never announces it, and the current page
carries `aria-current="page"` rather than being a link to itself.

## Culture is pinned to en-AU

`Program.cs` sets `CultureInfo.DefaultThreadCurrentCulture` before the host is built. A WASM app
otherwise inherits the **viewer's** locale, so `ToString("C2")` on a laptop set to en-GB renders the
church's money as pounds — observed on 2026-08-31 as `£0.00` in the debit card form's action bar. This
is an Australian church filing to the ACNC and the ATO; the currency is AUD and dates are day-first
regardless of who opens the page. It must be set before the host is built, because component code reads
`CurrentCulture` as it renders.

## A section-rail anchor must carry the page path

`<base href="/">` means a fragment-only `href="#s1"` resolves to `https://host/#s1` — the path is
dropped and clicking a rail entry navigates to the landing page. Nothing errors, so it reads as "the
anchors just do not work". `SectionRail.Href` builds each one from `NavigationManager.Uri`'s path.

`index.html` deliberately does **not** link `GSBC.Accounting.WASM.styles.css`. That bundle is emitted
only when some component has a scoped `.razor.css`, and none does — the whole design is in `app.css` —
so the template's unconditional link is a 404 on every page load.

A page sets the topbar title by taking `MainLayout` as a `[CascadingParameter]` and calling
`SetTitle` from `OnInitialized`.
