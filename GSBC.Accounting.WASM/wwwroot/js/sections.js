/*
  Marks the section the claimant is looking at, and keeps that entry visible in the mobile stepper.

  The second script in this app, and the bar for adding one is the same as it was for the theme script
  in index.html: it has to be something Blazor genuinely cannot do. This is. Answering "which section
  is on screen" means watching the viewport as it scrolls, and doing that from C# would be one interop
  call per scroll event across the WASM boundary - a boundary crossing per frame to set one CSS class.
  An IntersectionObserver answers it in the browser, at the browser's own pace, and never calls back
  into the app at all.

  It also means the rail markup stays free of a `current` parameter that only a scroll could set, so
  nothing here has to be threaded through the form's render tree.

  DELIBERATELY OUTSIDE THE RENDER TREE. Blazor does not manage the class this sets: SectionRail renders
  its anchors with no class attribute at all, so a re-render neither carries this class over nor fights
  it. A re-render that did drop it is repaired by the next scroll, and by the MutationObserver below.
*/
(function () {
    "use strict";

    var current = null;
    var observer = null;

    function rail() {
        return document.querySelector("nav.rail");
    }

    /*
      The pill or row for one section. Matched on the href's ending rather than on a data attribute,
      because SectionRail writes the whole page path in front of the fragment - see its Href comment
      for why a bare "#s3" cannot be used.
    */
    function entryFor(id) {
        var nav = rail();

        return nav ? nav.querySelector('a[href$="#' + id + '"]') : null;
    }

    function mark(id) {
        if (id === current) return;

        var previous = current === null ? null : entryFor(current);
        if (previous) previous.classList.remove("active");

        current = id;

        var entry = entryFor(id);
        if (!entry) return;

        entry.classList.add("active");

        /*
          Horizontal only, and only when the rail actually scrolls - which is the mobile stepper, 1732px
          of pills inside a 390px window. scrollIntoView would also scroll the PAGE vertically to bring
          the sticky rail into view, which on a form somebody is reading is the page moving under their
          hands. Setting scrollLeft moves the strip and nothing else.
        */
        var nav = rail();
        if (!nav || nav.scrollWidth <= nav.clientWidth) return;

        var target = entry.offsetLeft - (nav.clientWidth - entry.offsetWidth) / 2;
        nav.scrollTo({left: Math.max(0, target), behavior: "smooth"});
    }

    function onIntersect(entries) {
        /*
          The topmost section currently on screen wins. Reading every observed section's own bounding
          box rather than only the ones this callback carries: the callback fires for what CHANGED, and
          the section that should be marked is often one that did not change at all - it was already on
          screen and the one below it has just left.
        */
        var best = null;

        document.querySelectorAll("main section.card[id]").forEach(function (section) {
            var box = section.getBoundingClientRect();

            // Anything whose top has passed the reading line and whose bottom has not yet. The 120px is
            // the sticky topbar plus enough of a heading to be looking at it.
            if (box.top <= 120 && box.bottom > 120) best = section.id;
        });

        // Before the first section reaches the line - at the very top of the page - nothing has passed
        // it, so fall back to the first section that is visible at all.
        if (!best && entries.length) {
            var visible = entries.filter(function (x) { return x.isIntersecting; });
            if (visible.length) best = visible[0].target.id;
        }

        if (best) mark(best);
    }

    function rescan() {
        if (observer) observer.disconnect();

        observer = new IntersectionObserver(onIntersect, {threshold: [0, 0.01, 0.5]});

        document.querySelectorAll("main section.card[id]").forEach(function (section) {
            observer.observe(section);
        });
    }

    /*
      Sections come and go while the form is being filled in - the whole of 1 to 8 appears when question
      zero is answered, and section 5 appears and disappears with the evidence attached to section 3 -
      so what is being observed has to be rebuilt when the DOM changes. Debounced, because Blazor
      rewrites parts of this tree on every keystroke and rebuilding an observer per character would cost
      more than the observer saves.
    */
    var pending = 0;

    function scheduleRescan() {
        clearTimeout(pending);
        pending = setTimeout(rescan, 150);
    }

    function start() {
        rescan();

        new MutationObserver(scheduleRescan).observe(document.body, {childList: true, subtree: true});
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", start);
    } else {
        start();
    }
})();
