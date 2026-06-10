/* ─────────────────────────────────────────────────────────────────────────
   InsuranceHub — Guided Tour  (Driver.js v1)
   Runs automatically on first visit; restart via the ? button any time.
───────────────────────────────────────────────────────────────────────── */

(function () {
    "use strict";

    const TOURED_KEY  = "ihub_toured_v1";
    const ADMIN_KEY   = "ihub_admin_toured_v1";

    // ── Helpers ──────────────────────────────────────────────────────────────

    function el(selector) { return document.querySelector(selector); }

    // Only include a step if the target element actually exists
    function step(element, title, desc, opts) {
        if (!el(element)) return null;
        return { element, popover: { title, description: desc, ...opts } };
    }

    // ── Main site tour ────────────────────────────────────────────────────────

    function buildSiteTour() {
        const isLoggedIn = document.body.dataset.loggedIn === "true";
        const isAdmin    = document.body.dataset.isAdmin  === "true";

        const steps = [
            // 1 ── Welcome (no element — centred dialog)
            {
                popover: {
                    title: "👋 Welcome to InsuranceHub",
                    description:
                        "Kenya's simplest way to compare and get covered. " +
                        "Let me walk you through the key features in under a minute.",
                    side: "over",
                    align: "center",
                }
            },

            // 2 ── Search
            step("#searchBox",
                "🔍 Instant search",
                "Type any plan name, provider, or cover type — results filter in real time as you type.",
                { side: "bottom", align: "start" }
            ),

            // 3 ── Category aisles
            step(".stripe-aisles",
                "📂 Browse by category",
                "Pick a cover category — Life, Health, Motor, Home, Travel, Business, Agriculture, or Micro — to narrow down the plans shown.",
                { side: "bottom", align: "start" }
            ),

            // 4 ── Provider pills
            step(".stripe-providers",
                "🏦 Filter by provider",
                "Want plans from a specific insurer? Pick them here — Jubilee, AAR, Britam, and more.",
                { side: "bottom", align: "start" }
            ),

            // 5 ── First plan card
            step(".stripe-card",
                "📋 Plan cards",
                "Each card shows the insurer, cover type, star rating, and top three features — everything you need at a glance.",
                { side: "right", align: "start" }
            ),

            // 6 ── Price block
            step(".stripe-price-block",
                "💰 Monthly premium",
                "See exactly what you'll pay each month. The coverage limit is shown just below the price.",
                { side: "right", align: "start" }
            ),

            // 7 ── Card actions
            step(".stripe-card-actions",
                "⚡ Get a quote or explore",
                isLoggedIn
                    ? "Click <strong>Get Quote</strong> to send a no-obligation request — we'll connect you with the provider within 24 hours. Or tap <strong>View details</strong> for the full breakdown."
                    : "Click <strong>Get Quote</strong> — you'll be asked to sign in first (it's free). Or tap <strong>View details</strong> to read the full plan breakdown before committing.",
                { side: "top", align: "start" }
            ),

            // 8 ── Nav / auth area
            isLoggedIn
                ? step(".nav-desktop",
                    "👤 Your account",
                    "Click your avatar to view your profile, track your quote history, and manage all your active policies in one place." +
                    (isAdmin ? " As an admin you also have full access to the <strong>Admin panel</strong> via the badge in the nav." : ""),
                    { side: "bottom", align: "end" }
                  )
                : step(".nav-desktop",
                    "🔐 Create a free account",
                    "Sign up to request quotes, save your favourite plans, and track all your cover in a single dashboard — completely free, no spam.",
                    { side: "bottom", align: "end" }
                  ),

        ].filter(Boolean); // remove nulls (missing elements)

        return steps;
    }

    // ── Admin tour ────────────────────────────────────────────────────────────

    function buildAdminTour() {
        return [
            {
                popover: {
                    title: "🛠 Admin panel overview",
                    description:
                        "Everything you need to manage the platform lives here. " +
                        "Let me point out the key sections.",
                    side: "over",
                    align: "center",
                }
            },
            step(".admin-nav",
                "🗂 Navigation",
                "Switch between <strong>Quotes</strong>, <strong>Plans</strong>, <strong>Providers</strong>, and <strong>Users</strong> from this sidebar. The active section is highlighted.",
                { side: "right", align: "start" }
            ),
            step("a[href='/admin/quotes']",
                "📬 Quote Requests",
                "Every quote a user submits lands here. Update the status to <strong>Contacted</strong> once you've reached out, and <strong>Closed</strong> when the policy is placed.",
                { side: "right", align: "start" }
            ),
            step("a[href='/admin/plans']",
                "📋 Plans",
                "Create, edit, or delete insurance plans. Toggle the <em>Popular</em> flag to feature a plan at the top of the browse page.",
                { side: "right", align: "start" }
            ),
            step("a[href='/admin/providers']",
                "🏦 Providers",
                "Manage the insurers that appear on the platform — name, rating, contact email, and website.",
                { side: "right", align: "start" }
            ),
            step("a[href='/admin/users']",
                "👥 Users",
                "See everyone who has registered. Promote any user to <strong>Admin</strong> or remove the role with one click. You cannot change your own role.",
                { side: "right", align: "start" }
            ),
            step(".admin-back-link",
                "↩ Back to the site",
                "When you're done, use this link to return to the public-facing Insurance Hub.",
                { side: "right", align: "start" }
            ),
        ].filter(Boolean);
    }

    // ── Driver.js config ──────────────────────────────────────────────────────

    function makeTour(steps) {
        return window.driver.js.driver({
            showProgress:      true,
            showButtons:       ["next", "previous", "close"],
            nextBtnText:       "Next →",
            prevBtnText:       "← Back",
            doneBtnText:       "Done  ✓",
            smoothScroll:      true,
            overlayColor:      "rgba(10, 37, 64, 0.55)",
            popoverClass:      "ihub-popover",
            steps,
        });
    }

    // ── Start helpers ─────────────────────────────────────────────────────────

    function startSiteTour() {
        const steps = buildSiteTour();
        if (!steps.length) return;
        makeTour(steps).drive();
        localStorage.setItem(TOURED_KEY, "1");
    }

    function startAdminTour() {
        const steps = buildAdminTour();
        if (!steps.length) return;
        makeTour(steps).drive();
        localStorage.setItem(ADMIN_KEY, "1");
    }

    // ── Expose globally so buttons can call them ──────────────────────────────

    window.ihubTour      = startSiteTour;
    window.ihubAdminTour = startAdminTour;

    // ── Auto-start on first visit ─────────────────────────────────────────────

    document.addEventListener("DOMContentLoaded", () => {

        const isAdminPage = document.body.dataset.adminPage === "true";

        if (isAdminPage) {
            if (!localStorage.getItem(ADMIN_KEY)) {
                setTimeout(startAdminTour, 800);
            }
        } else {
            if (!localStorage.getItem(TOURED_KEY)) {
                // Only auto-start on home page where the plan grid is present
                if (document.getElementById("shelf")) {
                    setTimeout(startSiteTour, 1200);
                }
            }
        }

        // Wire up any [data-tour] trigger buttons
        document.querySelectorAll("[data-tour]").forEach(btn => {
            btn.addEventListener("click", e => {
                e.preventDefault();
                if (btn.dataset.tour === "admin") startAdminTour();
                else startSiteTour();
            });
        });
    });

})();
