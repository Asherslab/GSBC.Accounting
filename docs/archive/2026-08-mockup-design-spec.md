---
title: Debit card form mockup — implementation spec
kind: plan
status: folded
opened: 2026-08-31
closed: 2026-08-31
verified: 2026-08-31
code:
  - mockups/debit-card-purchase-form.html
---

> **Archived 2026-08-31.** The mockup was ported; app.css and the Razor components are the live version. Kept for the token inventory and the float-arithmetic analysis, which is why the server recomputes every total.

# Debit card form mockup — implementation spec

Everything in `mockups/debit-card-purchase-form.html` that a Blazor developer has to reproduce, read out
of the file so nobody has to read it again. Scope, data model and slice order are in
[the scope doc](2026-08-expense-forms-scope.md); this doc is only the mockup.

The mockup is a single 1019-line file: `<style>` at lines 5–427, static markup 429–728, one IIFE at
730–1018. There is **no `<form>` element anywhere**, and no build step — everything is hand-written CSS
and vanilla DOM.

Where the mockup does not decide something, this doc says **silent**. Do not read a decision into
silence.

---

## 1. Design tokens

The mockup already has a token layer: 18 colours plus `--radius` and `--shadow` on `:root` (lines 6–27),
redefined for dark under both `@media (prefers-color-scheme: dark)` (28–51) and `:root[data-theme="dark"]`
(52–72). **Everything else — every size, every space, every weight — is hard-coded at the point of use.**

This section proposes **92 custom properties**. Twenty of them already exist and keep their names.

**Two defects in the existing token layer, fix both when porting:**

- `--line-strong` is declared twice inside the `prefers-color-scheme: dark` block — `#464a53` at line 37
  and `#4a4e57` at line 47. The first is dead. `:root[data-theme="dark"]` (line 60) uses `#4a4e57`, so
  **`#4a4e57` is the intended dark value** and `#464a53` should be deleted.
- The dark palette is written out twice, verbatim, in two blocks. Porting that duplication into the real
  app guarantees the two copies drift. Define the dark values once and have the media query and the
  attribute selector share them.

### 1.1 Colour

Existing names are kept. Light value is the `:root` block; dark is `:root[data-theme="dark"]` (line 52+),
which is authoritative over the media-query block.

| Token | Light | Dark | Used for |
|---|---|---|---|
| `--ground` | `#f7f6f3` | `#16171a` | page background; also the *text* colour on brand fills in dark |
| `--surface` | `#ffffff` | `#1e2024` | cards, topbar, action bar, inputs, chips |
| `--surface-sunk` | `#f1efea` | `#25272c` | `th`, `.checkline`, `.drop`, `.thumb`, `.sig`, neutral badge, rail hover |
| `--ink` | `#1b1c20` | `#eceae5` | body text, input text, `.checkline b` |
| `--ink-2` | `#4a4d55` | `#b3b5bc` | labels, help-adjacent prose, `.q p`, `.lede`, `.totals .k` |
| `--ink-3` | `#7c7f88` | `#83868f` | hints, `.help`, `th`, placeholders, `.sz`, rail headings, `$` prefix |
| `--line` | `#dfdcd4` | `#33363d` | card borders, table row rules, header rule, `.file`, `.totals .rule` |
| `--line-strong` | `#c6c2b8` | `#4a4e57` | input borders, chip/`.yn` borders, `.drop` dash, `.sig` border, rail dot |
| `--brand` | `#33415c` | `#a9bce2` | `.mark` fill, section numbers, primary button, `.yn` checked, links |
| `--brand-2` | `#4a5c80` | `#8fa4cd` | hover borders, focused input border, primary button hover, notice rule |
| `--brand-wash` | `#eaedf4` | `#232833` | active rail item, checked chip, `.notice`, `.drop` hover |
| `--ok` | `#2f6e4e` | `#7fc39c` | done dot, `.badge.ok`, checked `.checkline` border (via `color-mix`) |
| `--ok-wash` | `#e6f0e9` | `#1e2a24` | checked `.checkline` background, `.badge.ok` background |
| `--warn` | `#8a5a17` | `#d7a75a` | `.reveal` left rule and label, `.yn` **Yes** fill, `.badge.warn` |
| `--warn-wash` | `#f6eeda` | `#2c2519` | `.reveal` background, `.notice.warn`, `.badge.warn` background |
| `--bad` | `#9d3427` | `#e0897c` | `.req` asterisk, error dot, `.banner` rule and heading, remove-row hover |
| `--bad-wash` | `#f7e7e3` | `#2e1f1d` | `.banner`, `.notice.bad`, `.rowbtn:hover` background |
| `--focus` | `#8aa0cc` | `#7d93c0` | the 2px `:focus-visible` outline |
| **`--on-brand`** *(new)* | `#ffffff` | `var(--ground)` | **replaces five hard-coded `#fff`** — lines 102, 231, 234, 381, 384 |
| **`--print-paper`** *(new)* | `#fff` | — | `@media print` body background (line 404) |
| **`--print-ink`** *(new)* | `#000` | — | print body text, `.printhead` rule (404, 413, 423) |
| **`--print-rule`** *(new)* | `#999` | — | print borders — **five occurrences**, lines 409, 413, 414, 415, 416 |
| **`--print-header`** *(new)* | `#eee` | — | print card header fill (line 410) |

**`--on-brand` is the one that matters.** The mockup solves "white text on a brand fill" three separate
times — `.mark`, `.yn label:has(input:checked)`, `button.b.primary` — and each time needs a hard-coded
`#fff`, a `:root[data-theme="dark"]` override to `var(--ground)`, a `:root:not([data-theme="light"])`
override, *and* a `@media (prefers-color-scheme: light)` re-override to win back `#fff`. That is twelve
declarations doing one job. One token collapses all of it.

### 1.2 Type

Three families, ten occurrences of the mono stack alone (lines 111, 141, 172, 201, 209, 297, 320, 325,
346, 373) and five of the serif (99, 104, 154, 358, 425).

| Token | Value |
|---|---|
| `--font-sans` | `"Public Sans","Helvetica Neue",Arial,sans-serif` |
| `--font-serif` | `"Newsreader",Georgia,serif` |
| `--font-mono` | `"IBM Plex Mono",monospace` |

All three come from one Google Fonts link (line 4): Newsreader 400/500/600 + 400 italic, Public Sans
400/500/600/700, IBM Plex Mono 400/500/600. The real app must self-host or keep that link; the mockup is
silent on which.

**Sizes.** The mockup uses **sixteen** distinct pixel sizes plus three print point sizes. Several are
near-duplicates that exist only because each rule was written independently. The right-hand column is the
proposed scale — **nine steps** — and which observed values collapse into it.

| Observed | Count | Where | Proposed token | Scale value |
|---|---|---|---|---|
| `10px` | 1 | `.thumb` extension label (320) | `--fs-3xs` | 10px |
| `10.5px` | 1 | action bar sum key (372) | `--fs-2xs` | 11px |
| `11px` | 2 | rail heading (130), `th` (267) | `--fs-2xs` | 11px |
| `11.5px` | 2 | rail number (141), `.badge` (330) | `--fs-2xs` | 11px |
| `12px` | 6 | topbar sub (107), draft stamp (110), section number (172), `label`/`.lbl` (190), `.help` (193), `.file .sz` (325) | `--fs-xs` | 12px |
| `12.5px` | 4 | `.yn label` (226), `.drop .small` (311), `.file select` (326), `.footnote` (396) | `--fs-xs` | 12px |
| `13px` | 6 | card hint (178), `$` prefix (209), `.chip` (216), `.btn-add` (287), `.notice` (340), `.banner ul` (392) | `--fs-sm` | 13px |
| `13.5px` | **8** | rail link (137), `.checkline` (240), `.q p` (254), `table` (265), `.totals` (294), `.file .name` (324), `button.b` (376), `.banner h4` (391) | `--fs-md` | 13.5px |
| `14px` | 2 | all inputs (197), `.qlabel` (255) | `--fs-base` | 14px |
| `14.5px` | 1 | `.drop .big` (310) | `--fs-base` | 14px |
| `15px` | 2 | `body` (80), `.totals .net` (299) | `--fs-lg` | 15px |
| `15.5px` | 1 | card `h3` (176) | `--fs-lg` | 15px |
| `16px` | 3 | `.rowbtn` (281), `.totals .net .v` (300), action bar value (373) | `--fs-xl` | 16px |
| `17px` | 2 | `.mark` (99), `.lede` (154) | `--fs-2xl` | 17px |
| `19px` | 1 | topbar `h1` (105) | `--fs-3xl` | 19px |
| `22px` | 1 | `.sig .typed` (358) | `--fs-4xl` | 22px |
| `9pt` | 1 | print `.printhead p` (426) | `--fs-print-sm` | 9pt |
| `10pt` | 2 | print body (404), print inputs (413) | `--fs-print-base` | 10pt |
| `16pt` | 1 | print `.printhead h2` (425) | `--fs-print-lg` | 16pt |

`13.5px` is the workhorse — eight independent hard-codings of the same number. If only one token comes
out of this table, it is `--fs-md: 13.5px`.

**Weights** — `600` appears **twenty times**; `400` is never written (it is the body default), `500` once
(section number, line 172), `700` three times (`.checkline b` 245, `.totals .net` 299, `.notice .ico` 346).

| Token | Value |
|---|---|
| `--fw-regular` | 400 |
| `--fw-medium` | 500 |
| `--fw-semibold` | 600 |
| `--fw-bold` | 700 |

**Line heights** — seven values: `--lh-none: 1` (`.rowbtn`), `--lh-tight: 1.15` (topbar h1),
`--lh-snug: 1.35` (rail link), `--lh-help: 1.45` (`.help`), `--lh-normal: 1.5` (body and four others),
`--lh-lede: 1.55`, `--lh-loose: 1.6` (`.banner ul`, `.footnote`).

**Letter spacing** — nine values, all tiny, all doing one of three jobs:

| Token | Value | Job |
|---|---|---|
| `--ls-heading` | `-.01em` | topbar `h1` (105) |
| `--ls-heading-sm` | `-.005em` | card `h3` (176) |
| `--ls-label` | `.02em` | `.mark`, `label`, `.yn label` |
| `--ls-badge` | `.03em` | `.badge` |
| `--ls-mono` | `.04em` | topbar sub, `.thumb` |
| `--ls-num` | `.06em` | section number |
| `--ls-th` | `.07em` | `th` |
| `--ls-barkey` | `.08em` | action bar sum key |
| `--ls-rail` | `.11em` | rail heading |

Nine tokens for nine values is honest but not useful. **Recommendation: collapse to three** —
`--ls-tight: -.01em`, `--ls-wide: .02em`, `--ls-caps: .07em` — and accept a sub-pixel change in the six
places that get rounded. The mockup is silent on whether the exact values matter; they were almost
certainly typed by eye.

### 1.3 Spacing

Twenty-three distinct pixel values with no scale behind them. Proposed 4px-based scale, and what each
observed value maps to:

| Token | Value | Observed values it absorbs | Notable uses |
|---|---|---|---|
| `--sp-px` | 1px | 1, 2, 3 | rail `ol` gap, `.checkline input` offset, `.totals .rule` margin |
| `--sp-1` | 4px | 4, 5 | `.yn label` vertical padding, `.badge` gap, `.rowbtn` padding |
| `--sp-2` | 6px | 6, 7 | `.f` gap, `.chips` gap, `td` padding, `.chip` padding |
| `--sp-3` | 8px | 8, 9 | input padding, `.stack` gap, `th` padding, `.totals` row gap |
| `--sp-4` | 11px | 10, 11, 12 | `.checkline` padding, `.file` padding, `.notice` gap |
| `--sp-5` | 14px | 13, 14 | topbar padding, `.reveal` padding, `.q` padding, `.notice` padding |
| `--sp-6` | 16px | 16, 17 | `.grid` row gap, `.sig` padding, `.totals` top margin |
| `--sp-7` | 18px | 18 | **card bottom margin**, `.grid` column gap, `.sig` gap, topbar gap |
| `--sp-8` | 20px | 20, 22 | `.card .body` padding, action bar sum gap |
| `--sp-9` | 26px | 26, 28 | `main` padding, `.drop` padding, rail padding, `.lede` margin |
| `--sp-10` | 40px | 40 | rail bottom padding |
| `--sp-bar-clear` | 160px | 160 | `main` bottom padding — clears the fixed action bar |

`--sp-7: 18px` is the rhythm of the page: it is the gap between cards (line 162) and the column gap of
the 12-column grid (181). `--sp-bar-clear` is load-bearing — the action bar is `position:fixed` and 160px
of bottom padding on `main` is the only thing keeping it off the footnote.

Print spacing is a separate, point-based set: `14mm 13mm` page margin, `8pt` card margin, `6pt 8pt`
header padding, `8pt` body padding, `8pt 10pt` grid gap, `1pt 2pt` input padding, `6pt`/`10pt` printhead.
Tokenise as `--sp-print-*` or leave literal; the mockup treats them as an unrelated system and so should
the real app.

### 1.4 Radius, shadow, motion, layering, breakpoints

| Token | Value | Where |
|---|---|---|
| `--radius` | `4px` | **already exists** — 13 uses plus the `.reveal` corner pair (258) |
| `--radius-sm` | `2px` | `.mark` (96), `:focus-visible` (85) |
| `--radius-thumb` | `3px` | `.thumb` (318) — arguably should be `--radius-sm` |
| `--radius-pill` | `99px` | `.chip` (215), `.yn` (224), `.badge` (331) |
| `--shadow` | `0 1px 2px rgba(27,28,32,.06), 0 8px 24px -16px rgba(27,28,32,.28)` | **already exists** — card elevation; dark is `rgba(0,0,0,.4)` / `rgba(0,0,0,.8)` |
| `--shadow-bar` | `0 -8px 24px -20px rgba(0,0,0,.5)` | action bar (368) — **hard-coded, no dark variant**, unlike `--shadow` |
| `--motion-fast` | `.12s` | `.drop` transition (307) — the only transition in the file |
| `--z-topbar` | `40` | `.topbar` (89) |
| `--z-actionbar` | `50` | `.actionbar` (364) |
| `--bp-md` | `900px` | grid columns collapse to full width (185) |
| `--bp-sm` | `700px` | `.sig` collapses to one column (356) |

There is no `--bp-lg`; `.layout`'s 238px rail never collapses at any width. **That is a gap, not a
decision** — below about 700px the page is a 238px rail beside a squeezed form. See §7.

The dark theme is applied two ways: the OS preference, and a `data-theme` attribute on
`document.documentElement` toggled by `#btnTheme` (lines 1000–1004). The attribute wins. There is no
persistence — a reload reverts to the OS preference. The mockup is silent on whether the real app should
persist it.

### 1.5 Layout constants and measures

Not colours, but hard-coded numbers the layout depends on. Give these names too or they will be retyped.

| Token | Value | Line | What breaks if it changes |
|---|---|---|---|
| `--rail-w` | `238px` | 116 | `.layout` grid column |
| `--page-max` | `1320px` | 119 | `.layout` max width |
| `--topbar-h` | `63px` | 123, 126 | rail `top` and `max-height` — **must equal the topbar's real height** (14px + 34px + 14px + 1px border) |
| `--scroll-offset` | `78px` | 164 | `scroll-margin-top` on cards, so rail anchors don't land under the topbar |
| `--table-min` | `840px` | 265 | line-item table min width before `.tablewrap` scrolls |
| `--table-min-sm` | `720px` | 810 | attendee table min width (inline style, in JS) |
| `--totals-max` | `420px` | 292 | totals block width |
| `--totals-input` | `130px` | 301 | the personal-portion input |
| `--sig-col` | `190px` | 352 | signature date column |
| `--thumb` | `44px` | 314, 318 | attachment thumbnail, and the `.file` grid's first column |
| `--mark` | `34px` | 96 | topbar logo square |
| `--measure-lede` | `64ch` | 153 | `.lede` |
| `--measure-q` | `66ch` | 254 | compliance question text |
| `--measure-note` | `78ch` | 396 | `.footnote` |

`--topbar-h` and `--scroll-offset` are 63 and 78 — they are related (78 = 63 + 15 of breathing room) but
written as unrelated literals. Derive the second from the first.

---

## 2. Page skeleton

### 2.1 DOM shape

```
<title>, font <link>s, <style>                        1–427
div.topbar.no-print                                   429–440   position:sticky, top:0, z-index:40
  div.mark                "GS"
  div > h1 + div.sub      title + "Good Shepherd Baptist Church · Finance"
  div.spacer              flex:1
  div.draft#draftstamp    "Draft · autosaved 12:04"
  button.b#btnTheme  button.b#btnPrint  button.b.primary#btnSubmit
div.printonly.printhead                               442–445   display:none except @media print
  h2 + p
div.layout                                            447–715   grid 238px / minmax(0,1fr), max 1320, centred
  nav.rail.no-print[aria-label="Form sections"]       448–461   position:sticky, top:63px, own scroll
    h2 "Sections"
    ol#railList > li × 9 > a[href="#sN"]
      span.n (1..8 or "—")  +  span(text + span.dot[data-dot="sN"])
  main                                                463–714   padding 26px 28px 160px
    p.lede.no-print
    div.banner.no-print#errBanner[hidden] > h4 + ul#errList
    section.card#s1 … #s8   (with #s3b between #s3 and #s4)
    p.footnote
div.actionbar.no-print                                717–728   position:fixed, bottom:0, z-index:50
  div.sum > div × 3 (span.k + span.v)   #barCharged #barNet #barFiles
  div.spacer
  span.badge#barStatus
  button.b#btnSave  button.b#btnPrint2  button.b.primary#btnSubmit2
script (IIFE)                                         730–1018
```

### 2.2 The card

Every section is the same three-part shape. There are no variants except the `.locked` modifier and the
`.pagebreak` utility on `#s6`.

```
section.card[.locked][.pagebreak]#sN
  header                          padding 15px 20px, border-bottom 1px --line, flex, align-items:baseline
    span.num                      "01".."08" — mono, 12px, --brand, tracking .06em
    h3                            15.5px / 600 / tracking -.005em
    span.hint | span.badge        optional, right-hand side
  div.body                        padding 20px
    (content)
```

Visual separation between cards is **only** the 18px gap, the 1px `--line` border, the 4px radius and
`--shadow`. There is no divider, no alternating background, no numbering gutter. Cards carry
`scroll-margin-top:78px` so rail anchors clear the sticky topbar.

**The `.hint` / `.badge` distinction is real and easy to get wrong.** `.hint` has `margin-left:auto`
(line 178) so it floats to the right edge of the header. `.badge` does not, so in sections 7 and 8 the
badge sits **immediately after the `h3`**, separated by the header's 12px gap. That is what the mockup
renders; reproduce it, or decide deliberately to change it.

### 2.3 Numbering

Two independent numbering systems, and they disagree on purpose:

- Card headers use **zero-padded two-digit** `01`–`08` in `span.num`.
- The rail uses **bare single digits** `1`–`8` in `span.n`.
- The attachments card is `—` (em dash) in both. **It is not a section of the paper form**; it sits
  between sections 3 and 4 because that is where the evidence belongs, and it is deliberately not
  numbered. Keep it that way — renumbering would break the correspondence with the `.docx`.

The rail's nine entries are hard-coded markup, not generated. Section 4's *content* is generated but its
card is static.

### 2.4 The 12-column grid

`.grid` (181) is `repeat(12,1fr)`, `gap:16px 18px`. Fields are `.f` (a 6px-gap flex column holding label,
control, optional `.help`) with a span class: `.c3 .c4 .c5 .c6 .c7 .c8 .c12`. Below 900px every span
except `.c12` becomes `span 12` (185–187). `.c7` and `.c8` are defined but **never used** in the markup —
dead CSS, safe to drop.

### 2.5 Print

`@media print` (402–421) turns the app back into the paper form: A4, 14mm/13mm margins, black on white,
10pt. It hides `.topbar, .rail, .actionbar, .btn-add, .rowbtn, .drop, .banner, .no-print`, unwraps
`.layout` to a block, strips shadows, reduces inputs to a bottom rule, and reveals `.printonly`.
`#s6` carries `.pagebreak` (`break-before:page`), so the declaration starts a fresh page.

Per the scope doc the real PDF is QuestPDF server-side, not `window.print()`. **This print stylesheet is
still the best statement of what the printed form should look like** — read it as a spec for the QuestPDF
layout, not as code to keep.

---

## 3. Interactive behaviours

Every behaviour the script implements. "Blazor" is the construct that replaces it, not a full design.

| # | Trigger | What it does | Blazor replacement |
|---|---|---|---|
| 1 | Page load, lines 766, 843, 897–898, 1017 | Seeds 3 line items, 2 attendee rows, 1 fake attachment, then calls `recalc()` | `OnInitialized` seeding the model — **dev-only**, per the scope doc's mock-data button |
| 2 | `#addItem` click (767) | Appends an empty line-item row (`rec:"attached"`, `pct:100`), recalculates | `@onclick` → `Lines.Add(new ExpenseLine{ ChurchUsePercent = 100 })` |
| 3 | `.rowbtn` click in a line row (759–761) | Removes the row **only if `tbody.rows.length > 1`**, then recalculates | `@onclick` → `if (Lines.Count > 1) Lines.Remove(line)`; render the button `disabled` when `Count == 1` rather than silently ignoring the click |
| 4 | `input`/`change` on any line row (762–763) | Recalculates every total | `@bind:event="oninput"` on each cell; totals become computed properties, no handler |
| 5 | `#personal` input (928) | Recalculates | two-way bind + computed `Net` |
| 6 | `#charged` input (929) | Recalculates — drives the mismatch notice and the action bar | two-way bind + computed `HasMismatch` |
| 7 | Yes/No radio change on any of the six compliance questions (826–829) | `rev.hidden = !(yes checked)`; refreshes rail dots | `@if (model.TravelYes == true) { <Reveal/> }` — conditional render, one per question |
| 8 | `#addHosp` click (845) | Appends an attendee row to the hospitality reveal table | `@onclick` → `Attendees.Add(new())` |
| 9 | `.drop` click (889) or Enter/Space (890) | Proxies to the hidden `#fileInput` | `InputFile` styled as the drop zone |
| 10 | `dragenter`/`dragover` on `.drop` (892) | Adds `.over` class | CSS `:hover` plus an `InputFile` overlay; MudBlazor or a small JS interop if a real drag class is wanted |
| 11 | `dragleave`/`drop` on `.drop` (893) | Removes `.over` | as above |
| 12 | `drop` / file input `change` (891, 894) | `addFiles()` — pushes `{name,size,url}`, creates an object URL for `image/*`, re-renders, recalculates | `InputFileChangeEventArgs` → upload per the scope doc's two-phase flow |
| 13 | `.rowbtn` click on a `.file` (871) | `files.splice(i,1)`, re-render, recalculate | `@onclick` → remove + `DELETE` the attachment |
| 14 | File list re-render (854–877) | Sets name, size, extension badge or image thumb, evidence-type `<select>`, and **auto-selects "Itemised receipt / tax invoice" when the filename matches `/receipt|invoice|tax/i`** (870) | computed default on add; keep the regex, it is a real nicety |
| 15 | Any `input`/`change` inside `main` (973–974) | `updateDots()` — rail dots and the action bar status badge | computed properties re-evaluated on render; no handler needed |
| 16 | `recalc()` (901–927) | See §4 | computed properties |
| 17 | `recalc()` (922–924) | Unlocks section 5 when **any** line's Receipt select is `missing`; swaps `#s5inner` / `#s5idle`; rewrites `#s5hint` | `@if (Lines.Any(l => l.ReceiptStatus == Missing))` — conditional render + computed hint text |
| 18 | `recalc()` (918–920) | Shows `#mismatchNotice` and fills `#mmLines` / `#mmCharged` | computed `HasMismatch` + conditional render |
| 19 | `#btnSubmit` / `#btnSubmit2` click (990–991) | `submit()` — if `problems()` is non-empty, fills `#errList` with anchor links and scrolls the banner into view; else stamps "Submitted — awaiting approval" | `EventCallback` → server call; `problems()` becomes both a client-side check **and** the server's validator (they must agree) |
| 20 | `#btnSave` click (992–995) | Rewrites the draft stamp with the current local time. **Saves nothing.** | per the scope doc, `localStorage` via JS interop — the mockup only pretends |
| 21 | `#btnPrint` / `#btnPrint2` click (996–997) | `window.print()` | replace with `GET /api/submissions/{id}/pdf` |
| 22 | `#btnTheme` click (1000–1004) | Toggles `data-theme` on `<html>`, falling back to the OS preference. Not persisted. | JS interop; decide persistence separately |
| 23 | `IntersectionObserver` on every `section.card` (1007–1015) | Sets `.active` on the matching rail link, with `rootMargin: "-70px 0px -70% 0px"` | JS interop — Blazor has no equivalent. Not worth a component; keep the observer |
| 24 | `updateDots()` (951–972) | Per-section `.done` dots and the status badge, "*N* items outstanding" / "Ready to submit" | computed properties, one per section |

### 3.1 The three behaviours to get right

**Conditional reveals (§3 row 7).** Six questions, generated at 792–830 from the `checks` array at
770–789. Each renders `div > div.q + div.reveal[hidden]`. `.q` is a flex row: a `<p>` holding
`span.qlabel` (the question, 14px/600/`--ink`) and `span.thenline` (the "if yes, do this" sentence,
inheriting `.q p` at 13.5px/`--ink-2`) on the left, and a `.yn` pill toggle on the right. **`.thenline`
has no CSS rule of its own — the mockup is silent on styling it differently, and it always shows,
whether the answer is Yes or No.**

`.q:first-child` drops its top border and top padding (253), so the six questions read as one ruled list
inside the card body.

**The default is `No`, pre-checked** (line 799). The scope doc requires `bool?` where null means *not
answered*. **The mockup cannot express that** — it has no unanswered state. The real page must either
render both radios unchecked initially, or make "not answered" a visible third state. This is the one
place where the mockup and the data model actively disagree.

The `.yn` control is a two-label pill (224–235): checked = `--brand` fill with `--on-brand` text, except
`input[value="yes"]:checked`, which is `--warn` — **a Yes answer turns the toggle amber**, matching the
`--warn`-bordered reveal it opens. That colour link is deliberate; keep it.

Five of the six reveals are a `.grid` of `.c6` fields built from `c.fields` (818–821); the second
(`hosp`) is a table (806–816).

**The line-item table (§3 rows 2–4).** `#itemsBody`, rows built by `row(d)` at 745–765. Eight columns:

| Column | Header | Control | Class | Notes |
|---|---|---|---|---|
| 1 | Item | `input[type=text]` | `.i-item` | placeholder "What was bought"; `min-width:180px` |
| 2 | Qty / details | `input[type=text]` | `.i-qty` | `min-width:130px` |
| 3 | Church purpose / user | `input[type=text]` | `.i-purpose` | `min-width:170px` |
| 4 | Receipt | `select` | `.i-rec` | `attached` \| `missing` — **drives section 5** |
| 5 | Gross incl. GST | `input[type=number][step=.01]` in `.money` | `.i-gross` | `width:110px`, right-aligned |
| 6 | GST shown | `input[type=number][step=.01]` in `.money` | `.i-gst` | `width:100px` |
| 7 | Church use % | `input[type=number][step=1][min=0][max=100]` | `.i-pct.mono` | `width:105px`, inline `text-align:right` |
| 8 | *(none)* | `button.rowbtn` `×` | — | `width:38px`, `aria-label="Remove line"` |

Table cell inputs are borderless and transparent until hovered or focused (273–275) — the table reads as
a grid, not as 24 boxes. `.tablewrap` gives `overflow-x:auto` around a `min-width:840px` table, so on
narrow screens the table scrolls inside the card rather than breaking the layout.

**Column 7, Church use %, is collected and never used in any calculation.** See §4.

**Running totals (§3 row 16).** `.totals` (291–301) is a two-column grid, `margin-left:auto`,
`max-width:420px`, right-aligned mono values with `font-variant-numeric:tabular-nums`. Four rows:

```
Total card transaction              #tTotal   (computed)
GST shown on evidence               #tGst     (computed)
Less personal portion to be repaid  #personal (an editable number input, max-width 130px)
──────────────────────────────────  .rule (grid-column 1/-1, 1px --line)
Net authorised church expense       #tNet     (computed, 15/16px, weight 700)
```

The third row is an **input inside the totals block**, not a read-out. That is the only editable total.

---

## 4. The arithmetic

Every calculation the script performs, with the mockup's field names. `N(x)` is the mockup's
`Number(x) || 0` idiom — it maps empty, whitespace and non-numeric input to `0`.

### 4.1 The formulas

| # | Output | Formula | Lines |
|---|---|---|---|
| A1 | `total` | `Σ over rows of N(row.i-gross)` | 903–905 |
| A2 | `gst` | `Σ over rows of N(row.i-gst)` | 903–905 |
| A3 | `missing` | `any row where row.i-rec === "missing"` | 906 |
| A4 | `personal` | `N(#personal)` | 908 |
| A5 | `charged` | `N(#charged)` | 909 |
| A6 | `net` | `Math.max(0, total − personal)` | 910 |
| A7 | `mismatch` | `Math.abs(total − charged) > 0.005 AND charged > 0` | 918 |
| A8 | display | `money(n) = "$" + (N(n)).toFixed(2)` with `\B(?=(\d{3})+(?!\d))` comma insertion | 735 |
| A9 | `#barCharged` | `money(charged)` | 915 |
| A10 | `#barNet` | `money(net)` | 916 |
| A11 | validation total | `rows.reduce((s,tr) => s + N(tr.i-gross), 0)` — **a second, independent computation of A1** | 941 |
| A12 | validation mismatch | `charged > 0 AND Math.abs(total − charged) > 0.005` — **a second copy of A7** | 943 |
| A13 | attachment size | `b < 1048576 ? max(1, round(b/1024)) + " KB" : (b/1048576).toFixed(1) + " MB"` | 852 |
| A14 | outstanding count | `problems().length` | 968 |

**Nothing else is computed.** In particular:

- **`i-pct` (Church use %) is read from the seed, written into the DOM, and never referenced again.**
  There is no `total × pct/100` anywhere. The mockup collects an apportionment percentage and ignores
  it. The real app must decide what it means — most likely `LineChurchAmount = Gross × Pct / 100`, with
  the church-use total feeding the net — but **the mockup does not make that decision** and this doc
  will not make it either. Flag it to the requester before slice 5.
- **`gst` is summed but never checked against `total`.** No `total / 11` sanity check, no cap. GST larger
  than gross is accepted.
- The attendee table's **Amount** and **Private share** columns are never totalled and never feed
  `personal`.
- The `family` reveal's **"Amount excluded as private"** field is never linked to `#personal` either,
  though it is plainly the same number.
- `#mrAmount` in section 5 is never compared to any line.
- `net` does not adjust `gst`. Repaying a personal portion reduces the net expense but leaves the GST
  figure untouched.

### 4.2 Float hazards, and what the server must produce

The scope doc's rule: money is `decimal(12,2)` and the server recomputes every total. These are the
places where the mockup's `double` and the server's `decimal` can disagree, and what the server must do.

| # | Hazard | Concrete case | What decimal must produce |
|---|---|---|---|
| **F1** | **`total +=` accumulates in binary floating point.** The seeded rows sum to a double whose exact value is `184.59999999999999432` — the nearest double to `184.60`. It prints correctly, but it is not `184.60`. | seed `118.00 + 51.60 + 15.00` (739–741) | `Lines.Sum(l => l.Gross)` in `decimal` = exactly `184.60m`. **Never compare a posted client total to the server total for equality across a `double` round-trip.** Recompute and use the server's figure. |
| **F2** | **`toFixed(2)` rounds by the double's binary value, not half-away-from-zero.** `(951.145).toFixed(2)` is `"951.14"`; `(8.575).toFixed(2)` is `"8.57"`; `(0.615).toFixed(2)` is `"0.61"`. | any amount ending `.xx5` | `decimal.Round(951.145m, 2, MidpointRounding.AwayFromZero)` = `951.15m`. **The screen and the receipt will differ by a cent.** Either round on the client the same way the server does, or never show more precision than was entered. |
| **F3** | **`step="0.01"` is not enforced.** There is no `<form>`, nothing calls `checkValidity()`, and `step` on a bare input is inert. A user can type or paste `184.605` or `1.23456` into any money field. | `#charged`, `#personal`, `.i-gross`, `.i-gst`, every reveal money field | Reject more than 2 decimal places at the contract boundary, or round half-away-from-zero on ingest and echo the rounded figure back. **Do not silently truncate** — the claimant's total will not match their receipt. |
| **F4** | **`Number(x) \|\| 0` swallows garbage.** `Number("abc")` is `NaN` → `0`. A mistyped amount silently becomes zero and the form still reports "Ready to submit". `0` and "not entered" are the same value. | any money input | Money fields are `decimal?`. `null` (not entered) must be distinguishable from `0.00`, and a non-numeric post is a validation error, not a zero. |
| **F5** | **`Math.max(0, total − personal)` clamps.** A personal portion larger than the line total silently produces `net = 0` with no warning; the claimant sees a plausible-looking form. | `personal = 200`, `total = 184.60` → `net = $0.00` | The server must **not** clamp. `personal > Σ Gross` is a validation error naming both figures, per the scope doc's "refused with both figures named". |
| **F6** | **The `0.005` epsilon is a float workaround that must not survive.** Half a cent cannot exist in `decimal(12,2)`. Carried into C# it would let a real discrepancy of `0.004` pass — impossible in decimal, so the epsilon is dead code that reads as a deliberate tolerance. | A7, A12 | `if (lineTotal != amountCharged)` — exact decimal equality, no tolerance. |
| **F7** | **The `charged > 0` guard disables the check.** Leave `#charged` blank or zero and the mismatch notice never appears — and `updateDots` marks section 3 done (955) because `#mismatchNotice.hidden` is true. A form with $184.60 of lines and no charge reads as complete. | `#charged` empty | `AmountCharged` is required and must be `> 0`. Reconciliation runs unconditionally. |
| **F8** | **A1 and A11 are the same sum written twice**, in `recalc` and in `problems`. They agree today and will not after the first edit. Likewise A7 and A12. | 903–905 vs 941; 918 vs 943 | One computed property, used by the display, the validator and the wire contract. |
| **F9** | **No bounds on any money field.** Negative gross, negative GST, GST exceeding gross, and amounts beyond `decimal(12,2)`'s range are all accepted. `min`/`max` exist only on `.i-pct` (755) and are unenforced. | `.i-gross = -50` | Range-check every amount: `>= 0`, `<= 9,999,999,999.99`, `GST <= Gross`, `0 <= Pct <= 100`. |
| **F10** | **`money()` puts the sign inside the currency symbol** — `money(-1234.5)` is `"$-1,234.50"`. | any negative total | Format server-side with an invariant/`en-AU` currency format; do not port `money()`. |

**The summary the developer needs:** the mockup's client-side arithmetic is *display only*. Recompute A1,
A2, A6 and A7 in `decimal` on the server, reject rather than clamp (F5) and reject rather than tolerate
(F6, F7), and treat every posted total as a claim to be checked, never as an input.

---

## 5. Sections 7 and 8 — how the mockup renders them

Both are `<section class="card locked">`. The `.locked` modifier is two rules and nothing else:

```css
.locked{opacity:.72}                    /* line 349 */
.locked .body{pointer-events:none}      /* line 350 */
```

So: the whole card, header included, is dimmed to 72% opacity, and the body ignores the mouse.

**This is a visual lock, not a real one.** `pointer-events:none` does not remove anything from the tab
order. In section 7 the four decision radios (661–664) and in section 8 the three GST radios (686–688),
two evidence checkboxes (692–693) and six checklist checkboxes (697–702) carry **no `disabled`
attribute** — a keyboard user tabs straight into them and can change them with the arrow keys or space.
Only the free-text, date and number inputs are `disabled`. The real page must disable every control in
both sections, or render them as static text. See §7.

### Section 7 — Independent approval (654–671)

- Header: `<span class="num">07</span>`, `<h3>Independent approval</h3>`,
  `<span class="badge neutral">Approver — after submission</span>`. The neutral badge is
  `--surface-sunk` on `--ink-3`, pill radius, 11.5px/600.
- Lead paragraph, `.help` with `margin-top:0` (657), verbatim:
  > The approver must not be the cardholder. Routed automatically to the ministry's delegate; if the
  > cardholder is a Responsible Person or related party, it routes to an independent approver instead.
- `.grid` with `margin-top:14px` (inline), containing:

| Field | Control | Span | State |
|---|---|---|---|
| Decision | 4 `.chip` radios, `name="decision"`: "Approved in full", "Church expense approved $…", "Repayment required $…", "Declined" | `.c12` | **not disabled** |
| Approver name / role | `input[type=text]` | `.c6` | `disabled` |
| Signature | `input[type=text].typed` | `.c3` | `disabled` |
| Date | `input[type=date]` | `.c3` | `disabled` |

The "Decision" group label is a `<span class="lbl">`, not a `<legend>`. The three field labels are bare
`<label>` elements with **no `for`** and the inputs have **no `id`** — they are siblings, so there is no
association at all, implicit or explicit.

`.typed` (357–360) is the signature style: Newsreader italic, 22px, no border except a 1px
`--line-strong` bottom rule, transparent background.

### Section 8 — Finance use only (674–707)

- Header: `08`, `Finance use only`, `<span class="badge neutral">Finance team</span>`.
- No lead paragraph. Straight into a `.grid`:

| Field | Control | Span | State |
|---|---|---|---|
| Transaction reference | `input[type=text]` | `.c4` | `disabled` |
| Statement date | `input[type=date]` | `.c4` | `disabled` |
| BAS period | `input[type=text]` | `.c4` | `disabled` |
| Account / GL code | `input[type=text]` | `.c4` | `disabled` |
| Cost centre / ministry | `input[type=text]` | `.c4` | `disabled` |
| GST credit claimed | `input[type=number]` inside `.money` | `.c4` | `disabled` |
| GST treatment | 3 `.chip` radios `name="gst"`: "GST credit", "No GST credit", "Mixed" | `.c6` | **not disabled** |
| Evidence check | 2 `.chip` checkboxes: "Valid tax invoice(s)", "Other acceptable evidence" | `.c6` | **not disabled** |
| Finance checklist | 6 `.checkline` checkboxes | `.c12` | **not disabled** |

The six checklist items, verbatim (697–702):

1. Supplier, date, description and amount are evidenced; GST information is sufficient for the credit claimed.
2. Coded to the correct activity/fund; restricted funds and grant conditions checked.
3. No personal or family costs remain as a Church expense; joint costs apportioned, private amounts
   repaid and reconciled.
4. Meal attendees, gift recipients and travel purpose documented; refunds, credits and duplicate charges checked.
5. Related-party, conflict and overseas-activity registers updated where required.
6. Form, approvals and supporting records filed together and retained under the record-retention
   policy (minimum seven years for ACNC records).

### Both sections in the rail and the print view

`updateDots()` hard-codes `s7: false, s8: false` (960), so their rail dots are **never** marked done.
Neither card is `.no-print`, so both appear in the print output — dimmed to 72% by `.locked`, which is
almost certainly wrong on paper. Per the scope doc the PDF renders them as **empty ruled blocks for
wet-signing**; the mockup does not do that, and the print CSS does not undo `.locked`'s opacity.

---

## 6. Attachments

**The mockup does have an attachments UI.** It is section `#s3b` (578–592) plus the JS at 847–898, and it
is complete enough to build from. The later slice is *not* designing from scratch.

What it renders:

- A card headed `—` / **Attachments**, with `<span class="hint" id="attCount">` showing
  `"No files yet"` or `"N file(s) attached"` (874).
- A **drop zone** (`.drop`, 581–584): 1.5px dashed `--line-strong`, 4px radius, 26px/20px padding,
  centred, `--surface-sunk` background, `cursor:pointer`, `.12s` transition on border and background,
  turning `--brand-2` / `--brand-wash` on hover or `.over`. Two lines of text:
  - `.big` (14.5px/600/`--ink`): **"Drop receipts here, or click to choose files"**
  - `.small` (12.5px/`--ink-3`): **"PDF, JPG, PNG, HEIC or email (.eml) · up to 20 MB each · photos of
    paper receipts are fine"** — note the non-breaking space in "20 MB".
- A hidden `<input type="file" id="fileInput" multiple hidden>` with
  `accept=".pdf,.jpg,.jpeg,.png,.heic,.eml,.webp"`. **The `accept` list includes `.webp`, which the
  visible copy does not mention.** Reconcile that against the scope doc's magic-byte allow-list (JPEG,
  PNG, PDF, HEIC) — `.eml` and `.webp` have no magic-byte rule there.
- A `<ul class="files">` of `.file` rows. Each is a 4-column grid `44px 1fr auto auto`, 9px/11px padding,
  1px `--line`, `--surface` background:
  1. `.thumb` — 44px square, `--surface-sunk`, 3px radius, 1px `--line`. Holds an `<img>`
     (`object-fit:cover`) when the file is `image/*` and an object URL was made; otherwise the
     **uppercased file extension, first 4 characters**, in 10px mono.
  2. `.meta` — `.name` (13.5px/600, single line, ellipsised) over `.sz` (12px mono `--ink-3`).
  3. A `<select aria-label="Evidence type">` with six options (850): *Itemised receipt / tax invoice,
     Card terminal receipt, Quote or order confirmation, Approval email, Photo of goods / event, Other
     supporting evidence*. Defaults to the first when the filename matches `/receipt|invoice|tax/i`.
  4. A `.rowbtn` `×` with `aria-label="Remove attachment"`.
- A **`.notice.bad`** below the list (587–590), shown whenever there are zero files (875):
  > **At least one itemised receipt or tax invoice is required.** If evidence genuinely can't be
  > obtained, mark the line "Missing" in section 3 and complete the missing receipt declaration.

What it does **not** have — all silent, all needing a decision in the attachment slice:

- No upload progress, no per-file status, no error state, no retry. Files appear instantly because
  nothing is uploaded.
- No client-side size or type rejection. The 20 MB limit is text in the drop zone; nothing enforces it.
- No duplicate detection, no reordering, no preview beyond the 44px thumbnail.
- No link between an attachment and a **line item** — the scope doc's `ExpenseAttachment` may point at a
  line, but the mockup's UI cannot express that.
- `fmtSize` (852) is decimal-ish and inconsistent: KB is `b/1024` rounded (binary), MB is `b/1048576` to
  one decimal (binary), and it floors at "1 KB". The seeded file, 214,000 bytes, renders as "209 KB".
- Nothing removes the object URLs created at line 882 — a real page must `URL.revokeObjectURL`.

The list also feeds two other places: `#barFiles` in the action bar (876) and the `s3b` rail dot (956).

---

## 7. Accessibility and form-semantics gaps

The mockup is a visual study and does not pretend otherwise. Every item here is a **fix for the real
page**, not a criticism of the mockup.

| # | Gap | Where | What the real page does |
|---|---|---|---|
| A | **No `<form>` element at all.** Nothing submits, no native validation, no `Enter`-to-submit, and `step`/`min`/`max`/`maxlength` are decorative. | whole file | `<EditForm>` with a model and a `DataAnnotationsValidator`. This one change is what makes F3 and F9 in §4.2 enforceable. |
| B | **No `required` attribute and no `aria-required`.** Required fields are marked by a `data-req` attribute (JS only) and a red `*` in a `<span class="req">` — an unlabelled asterisk that a screen reader announces as "star". | 480–531 | `required` + `aria-required="true"`, and an explanatory "Fields marked * are required" once at the top. |
| C | **No `<fieldset>`/`<legend>` on any radio or checkbox group.** "Role / relationship" (488), the six Yes/No pairs (798–801), the declarations (631), section 7's Decision, section 8's GST treatment and Evidence check all use a `<span class="lbl">` or a `<p>` as the group label — which is not a label. | throughout | `<fieldset><legend>` for each, or `role="radiogroup"` + `aria-labelledby`. The six compliance questions matter most: the question text is the legend. |
| D | **Labels not associated in three places.** Section 7 and 8 use `<label>` as a *sibling* of an id-less input (666–668, 678–683). The JS-built reveal fields do the same (819). The attendee table's inputs have no labels at all (836–840). Sections 1–6's static fields **do** use `for`/`id` correctly. | 666–683, 819, 836–840 | `for`/`id` everywhere; for table cells, `aria-label` per input plus `scope="col"` on the `th`. |
| E | **Line-item table inputs have no accessible names** — only placeholders (749–755). A placeholder is not a label and disappears on input. | 749–755 | `aria-label` per cell input, `scope="col"` on every `th`, and a `<caption>` on the table. |
| F | **`.locked` does not disable anything.** `pointer-events:none` leaves 15 controls in sections 7 and 8 keyboard-reachable and changeable. | 349–350, 661–702 | `disabled` on every control in both sections, or render them as read-only text. |
| G | **No live regions.** `#errBanner` (470), `#mismatchNotice` (570), `#barStatus` (724) and `#attCount` (579) all change silently. | 470, 570, 579, 724 | `role="alert"` on the error banner and the mismatch notice; `aria-live="polite"` on the status badge and the attachment count. |
| H | **The rail dots carry state with no text.** `.done` / `.err` are colour-only, 6px, no `aria-label`, no text alternative. Colour is the sole channel. | 143–148, 962–967 | Add visually-hidden text ("complete" / "needs attention") and something non-colour — a tick glyph. |
| I | **The `$` prefix is a `::before` pseudo-element** (207–210), so it is not in the accessible name and not in the value. | 205–210 | Keep the visual, add the currency to each field's label or `aria-label`. |
| J | **The drop zone is a `div[role=button][tabindex=0]`** wrapping a `hidden` file input, with Enter/Space handled manually (890). | 581–590 | Blazor's `InputFile`, styled — a real `<input type="file">` with a real `<label>`, which gets keyboard and screen-reader behaviour for free. |
| K | **No `autocomplete` on any field.** Cardholder name, ministry, supplier, and the reimbursement form's phone/email all lack it. | 480–510 | `autocomplete="name"`, `"tel"`, `"email"` etc. on the personal fields. |
| L | **Tab order has no skip link,** and the sticky topbar plus fixed action bar mean a keyboard user tabs through 3 topbar buttons, then 9 rail links, before reaching the first field. | 429–461 | A "Skip to form" link as the first focusable element. |
| M | **Focus is never managed after a state change.** Adding a line, opening a reveal, or scrolling the error banner into view (982) moves nothing. | 767, 827, 982 | Focus the new row's first input, the revealed group, and the banner heading (`tabindex="-1"`). |
| N | **Removing the last line item silently does nothing** (760). No message, no disabled state. | 759–761 | Render the button `disabled` with a title explaining why. |
| O | **The attendee table has an Add but no Remove** (845) — asymmetric with section 3, and rows cannot be undone. | 833–845 | Add a remove button matching section 3's. |
| P | **No responsive breakpoint below 700px for the shell.** `.layout`'s `238px` rail never collapses; only `.grid` (900px) and `.sig` (700px) respond. On a phone the form column is what is left of the viewport after 238px. | 114–120 | A breakpoint that stacks the rail above `main` or turns it into a top nav. **The mockup is silent on what that should look like.** |
| Q | **Contrast is unverified.** `--ink-3` (`#7c7f88`) on `--surface-sunk` (`#f1efea`) is used for `th` and `.help` at 11–12px. That pairing is around 3.4:1 — below the 4.5:1 needed for small text. | 193, 267 | Measure every ink/ground pair in both themes and darken `--ink-3` where it fails. |
| R | **The theme toggle is not persisted and has no accessible state** — a plain button labelled "Theme". | 437, 1000–1004 | `aria-pressed`, or a three-state control (System / Light / Dark), and persistence. |

Two things the mockup gets **right** and should be preserved: the `:focus-visible` rule at line 85 (a 2px
`--focus` outline with 2px offset, applied globally, never removed) and `.card` `scroll-margin-top:78px`,
which stops rail anchors landing behind the sticky topbar.

---

## 8. Differences to expect on the reimbursement page

The mockup is the **debit card** form only. Per the scope doc these two pages share one contract and one
aggregate but stay separate Razor pages. What has to change, and where it is hard-coded in the mockup:

| What | Debit card (mockup) | Reimbursement |
|---|---|---|
| Page title, topbar `h1` | "Debit Card Purchase" (432) | "Expense Reimbursement" |
| Print head | "Church Debit Card Purchase Form" / "one form per transaction" (443–444) | claim-based wording, one form per claim period |
| Section 1 heading | "Cardholder and transaction details" (477) | "Claimant details" |
| Section 1 fields | Cardholder name, card last 4, transaction date + time, amount charged, bank reference (480–510) | Claimant name, phone, email, expense period **from → to**, payment method, bank details on file. **The card-security notice (513–516) has no equivalent and must be removed, not reworded.** |
| Section 3 column 1 | **Item** (748) — one card transaction, itemised | **Date** — one line per receipt |
| Section 3 hint | "Itemise the complete card transaction" (540) | "One line per receipt" |
| Totals block | Total card transaction → less personal → **Net authorised church expense** (562–567) | Subtotal of receipts → less non-reimbursable → **Total reimbursement claimed** |
| Reconciliation | lines vs `#charged` (A7) | there is no single charge to reconcile against — **the mismatch notice has no counterpart.** Decide what replaces it, if anything. |
| Section 4, Q1 | "Parking, toll, fuel, taxi or other travel?" with two text fields (771–773) | motor vehicle **kilometres**, revealing a **trip record table**: from / to / km / rate — a different table from the meal-attendee one, with an obvious per-row `km × rate` calculation the mockup has no precedent for |
| Section 4, Q2 | meal/hospitality → attendee table (774–776) | identical |
| Section 4, Q3–Q6 | family, overseas, related party, conflict (777–788) | identical, word for word |
| Section 5 | missing receipt declaration (603–625) | identical, **plus** a "not reimbursed from another source" clause |
| Section 6 | five declarations (632–641) | five declarations, four shared; the fifth is the no-double-claim one. The card-specific third item ("the Church debit card must not be used for personal purchases", 637) does not apply |
| Section 6 signature label | "Cardholder signature" (645) | "Claimant signature" |
| Section 7 decision chips | includes "Repayment required $…" (663) | includes "Returned for information" instead |
| Section 8 fields | Transaction reference, Statement date, personal repayment (678–679) | Claim reference, Payment date, Payment reference |
| `.docx` source | *Good Shepherd Baptist Church Debit Card Purchase Form.docx* | *Good Shepherd Baptist Church Expense Reimbursement Form.docx* — **read it for the exact wording; this doc has none of it** |

The **trip record** is the one genuinely new component. The mockup's `checks` array supports exactly two
reveal shapes — a grid of labelled fields, or the one hard-coded hospitality table (806–816). A trip
record is a third shape with its own arithmetic (`km × rate`, summed). Build the reveal as a component
that takes arbitrary content rather than extending the `checks`-array pattern, or the reimbursement page
will fork it.

Everything in §1 (tokens), §2 (skeleton) and §7 (accessibility) applies unchanged to both pages.
