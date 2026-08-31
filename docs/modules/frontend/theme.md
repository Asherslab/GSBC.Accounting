---
title: The theme, and why there is no component library
kind: reference
status: current
module: frontend
verified: 2026-08-31
code:
  - GSBC.Accounting.WASM/wwwroot/css/app.css
  - GSBC.Accounting.WASM/wwwroot/index.html
  - GSBC.Accounting.WASM/Layout/MainLayout.razor
  - mockups/debit-card-purchase-form.html
---

# The theme, and why there is no component library

Read this before styling a page or reaching for a UI package. It says where the CSS came from, the one
rule that keeps it honest, and the two traps in it that have already bitten once.

## `app.css` is a lift, not an interpretation

`GSBC.Accounting.WASM/wwwroot/css/app.css` is the `<style>` block of
`mockups/debit-card-purchase-form.html`, moved across close to verbatim. The mockup was built from the
`.docx` and approved as the shape to aim at, so **when the two disagree, the mockup is right and the
CSS is the bug.** Change a token here only by changing the mockup first.

Two blocks are not from the mockup and are marked as such in the file: the Blazor shell styles (the
boot spinner in `#app` and the `#blazor-error-ui` strip), because a static page has no boot phase, and
the fix described under "Traps" below.

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

## Traps

**The dark palette is written out twice and the two copies must stay byte-identical.** Plain CSS gives
no way round it — an at-rule block and a plain selector cannot share a declaration list — so
`@media (prefers-color-scheme: dark) :root:not([data-theme="light"])` and `:root[data-theme="dark"]`
each carry the whole thing. A value added to one and not the other produces a page that is right until
somebody touches the theme toggle, and then is wrong in a way nobody reproduces.

The mockup had already drifted here: `--line-strong` was declared twice inside the media block,
`#464a53` then `#4a4e57`, and only the second matched the explicit-dark block. The dead one is deleted
in `app.css`; the mockup still has it.

**The `@media print` block is not the PDF.** It is the browser's print view, kept because the mockup
had it. The artefact that matters is rendered server-side with QuestPDF from the submission aggregate,
deliberately, so the screen layout and the printed layout are free to diverge — including appending the
receipts as pages, which no print stylesheet can do.

## The shell

`MainLayout.razor` renders only the topbar: the `GS` mark, the page title and the church/finance
subtitle. It does **not** render the `.layout` grid — the section rail in the mockup's left column is
per-page, because each form owns its own section list and the landing page has none. A page therefore
opens its own `<div class="layout">`.

A page sets the topbar title by taking `MainLayout` as a `[CascadingParameter]` and calling
`SetTitle` from `OnInitialized`.
