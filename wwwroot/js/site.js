document.addEventListener("DOMContentLoaded", () => {

    // ── Mobile nav hamburger ───────────────────────────────────────────────
    const navToggle = document.getElementById("navToggle");
    const navDrawer = document.getElementById("navDrawer");
    if (navToggle && navDrawer) {
        navToggle.addEventListener("click", () => {
            const open = navDrawer.classList.toggle("open");
            navToggle.classList.toggle("open", open);
            navToggle.setAttribute("aria-expanded", open);
            navDrawer.setAttribute("aria-hidden", !open);
            document.body.style.overflow = open ? "hidden" : "";
        });
        // Close drawer on outside click
        document.addEventListener("click", e => {
            if (!navToggle.contains(e.target) && !navDrawer.contains(e.target)) {
                navDrawer.classList.remove("open");
                navToggle.classList.remove("open");
                navToggle.setAttribute("aria-expanded", false);
                navDrawer.setAttribute("aria-hidden", true);
                document.body.style.overflow = "";
            }
        });
    }

    const modal          = document.getElementById("quoteModal");
    const closeBtn       = document.getElementById("closeForm");
    const selectedPlan   = document.getElementById("selectedPlan");
    const modalBadge     = document.getElementById("modalBadge");
    const modalProvider  = document.getElementById("modalProvider");
    const modalPrice     = document.getElementById("modalPrice");
    const searchBox      = document.getElementById("searchBox");

    // ── Quote buttons ──────────────────────────────────────────────────────

    document.querySelectorAll(".quote-btn").forEach(btn => {
        btn.addEventListener("click", () => {

            document.getElementById("hiddenPlanId").value   = btn.dataset.planId   ?? "";
            document.getElementById("hiddenPlanName").value = btn.dataset.plan      ?? "";
            document.getElementById("hiddenTier").value     = btn.dataset.tier      ?? "";
            document.getElementById("hiddenProvider").value = btn.dataset.provider  ?? "";
            document.getElementById("hiddenPremium").value  = btn.dataset.premium   ?? "";
            document.getElementById("hiddenCoverage").value = btn.dataset.coverage  ?? "";
            document.getElementById("hiddenType").value     = btn.dataset.type      ?? "";
            document.getElementById("hiddenFeatures").value = btn.dataset.features  ?? "";

            if (selectedPlan)  selectedPlan.textContent  = btn.dataset.plan ?? "";
            if (modalBadge)    modalBadge.textContent    = btn.dataset.type ?? "";
            if (modalProvider) modalProvider.textContent = btn.dataset.provider ?? "";
            if (modalPrice)    modalPrice.textContent    = btn.dataset.premium
                ? `KES ${parseFloat(btn.dataset.premium).toLocaleString()}/mo`
                : "";

            openModal();
        });
    });

    // ── Modal open / close ─────────────────────────────────────────────────

    function openModal() {
        modal.style.display = "block";
        document.body.style.overflow = "hidden";
    }

    function closeModal() {
        modal.style.display = "none";
        document.body.style.overflow = "";
    }

    closeBtn?.addEventListener("click", closeModal);
    window.addEventListener("click", e => { if (e.target === modal) closeModal(); });
    document.addEventListener("keydown", e => { if (e.key === "Escape") closeModal(); });

    // ── Live search ────────────────────────────────────────────────────────

    searchBox?.addEventListener("input", () => {
        const q = searchBox.value.toLowerCase().trim();
        document.querySelectorAll(".insurance-plan").forEach(card => {
            const name     = card.querySelector(".stripe-card-name")?.textContent.toLowerCase() ?? "";
            const type     = card.dataset.type?.toLowerCase() ?? "";
            const provider = card.dataset.provider?.toLowerCase() ?? "";
            card.style.display = (!q || name.includes(q) || type.includes(q) || provider.includes(q)) ? "" : "none";
        });
    });

    // ── Auto-dismiss toast ─────────────────────────────────────────────────

    const toast = document.getElementById("toast");
    if (toast) setTimeout(() => toast.remove(), 5000);

    // ── Trust counter animation ────────────────────────────────────────────

    function animateCount(el) {
        const target = parseInt(el.dataset.target ?? "0", 10);
        const suffix = el.dataset.suffix ?? "";
        const dur    = 1600;
        const start  = performance.now();
        (function step(now) {
            const prog = Math.min((now - start) / dur, 1);
            const ease = 1 - Math.pow(1 - prog, 3);
            el.textContent = Math.round(ease * target) + suffix;
            if (prog < 1) requestAnimationFrame(step);
        })(start);
    }

    const trustNums = document.querySelectorAll(".trust-num[data-target]");
    if (trustNums.length && "IntersectionObserver" in window) {
        const io = new IntersectionObserver(entries => {
            entries.forEach(e => {
                if (e.isIntersecting) { animateCount(e.target); io.unobserve(e.target); }
            });
        }, { threshold: .5 });
        trustNums.forEach(n => io.observe(n));
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  COCKPIT PARALLAX — hero responds to mouse, gyroscope, and scroll
    // ═══════════════════════════════════════════════════════════════════════

    const hero   = document.getElementById("heroSection");
    const blob1  = document.getElementById("heroBlob1");
    const blob2  = document.getElementById("heroBlob2");
    const blob3  = document.getElementById("heroBlob3");
    const grid   = document.getElementById("heroGrid");
    const inner  = document.getElementById("heroInner");

    if (!hero || !blob1) return; // only home page

    // Current & target pointer offsets (-1 … +1)
    let tx = 0, ty = 0;  // target
    let cx = 0, cy = 0;  // current (lerped)
    let raf = null;
    let gyroActive = false;

    // Lerp factor — higher = snappier
    const LERP = 0.07;

    // How many px each layer shifts at full tilt
    const B1 = 55;  // blob 1 — most movement
    const B2 = 40;  // blob 2
    const B3 = 25;  // blob 3 (centre orb)
    const GD = 18;  // grid
    const CN = 10;  // content — subtle

    function lerp(a, b, t) { return a + (b - a) * t; }

    function applyLayers() {
        blob1.style.transform = `translate(${cx * B1}px, ${cy * B1}px)`;
        blob2.style.transform = `translate(${-cx * B2}px, ${-cy * B2}px)`;
        // blob3 anchored to centre; offset from its own translate(-50%,-50%)
        blob3.style.transform = `translate(calc(-50% + ${cx * B3}px), calc(-50% + ${cy * B3}px))`;
        grid.style.transform  = `translate(${cx * GD}px, ${cy * GD}px) rotate(${cx * .4}deg)`;
        inner.style.transform = `translate(${cx * CN}px, ${cy * CN}px)`;
    }

    function tick() {
        cx = lerp(cx, tx, LERP);
        cy = lerp(cy, ty, LERP);

        // Stop raf when settled (saves battery)
        if (Math.abs(cx - tx) < 0.001 && Math.abs(cy - ty) < 0.001) {
            cx = tx; cy = ty;
            applyLayers();
            raf = null;
            return;
        }

        applyLayers();
        raf = requestAnimationFrame(tick);
    }

    function moveTo(nx, ny) {
        tx = Math.max(-1, Math.min(1, nx));
        ty = Math.max(-1, Math.min(1, ny));
        if (!raf) raf = requestAnimationFrame(tick);
    }

    // ── Desktop: mouse move ──────────────────────────────────────────────

    window.addEventListener("mousemove", e => {
        if (gyroActive) return;

        const r = hero.getBoundingClientRect();
        // Only react when mouse is over (or near) the hero
        const inHero = e.clientY < r.bottom + 100;
        if (!inHero) return;

        const nx = ((e.clientX - r.left) / r.width  - 0.5) * 2;
        const ny = ((e.clientY - r.top)  / r.height - 0.5) * 2;
        moveTo(nx, ny);
    });

    // Reset when mouse leaves hero entirely
    hero.addEventListener("mouseleave", () => {
        if (!gyroActive) moveTo(0, 0);
    });

    // ── Mobile: device orientation (gyroscope) ───────────────────────────

    function handleOrientation(e) {
        gyroActive = true;
        // gamma = left/right tilt (-90…90)  beta = front/back tilt (-180…180)
        const nx = (e.gamma ?? 0) / 30;   // ±30° → ±1
        const ny = ((e.beta  ?? 0) - 15) / 30; // tilt forward a bit as neutral
        moveTo(nx, ny);
    }

    if (typeof DeviceOrientationEvent !== "undefined") {
        // iOS 13+ requires permission
        if (typeof DeviceOrientationEvent.requestPermission === "function") {
            // Add a tap-to-enable on first touch
            hero.addEventListener("touchstart", function askPermission() {
                DeviceOrientationEvent.requestPermission()
                    .then(state => {
                        if (state === "granted") {
                            window.addEventListener("deviceorientation", handleOrientation, { passive: true });
                        }
                    })
                    .catch(() => {});
                hero.removeEventListener("touchstart", askPermission);
            }, { once: true });
        } else {
            window.addEventListener("deviceorientation", handleOrientation, { passive: true });
        }
    }

    // ── Scroll parallax — hero compresses + content lifts ─────────────────

    function onScroll() {
        const scrollY = window.scrollY;
        const heroH   = hero.offsetHeight;

        if (scrollY > heroH) return; // hero already off screen

        // Progress 0 → 1 as hero scrolls out
        const prog = scrollY / heroH;

        // Blobs drift upward faster than scroll (parallax)
        const driftY = scrollY * 0.35;
        blob1.style.transform = `translate(${cx * B1}px, ${cy * B1 - driftY * .8}px)`;
        blob2.style.transform = `translate(${-cx * B2}px, ${-cy * B2 - driftY * .5}px)`;
        blob3.style.transform = `translate(calc(-50% + ${cx * B3}px), calc(-50% + ${cy * B3 - driftY * .6}px))`;

        // Grid subtly compresses inward
        const gridScale = 1 - prog * 0.08;
        grid.style.transform = `translate(${cx * GD}px, ${cy * GD - driftY * .3}px) scale(${gridScale}) rotate(${cx * .4}deg)`;

        // Content fades and lifts as you scroll past
        inner.style.transform = `translate(${cx * CN}px, ${-scrollY * 0.15}px)`;
        inner.style.opacity   = `${1 - prog * 1.4}`;
    }

    window.addEventListener("scroll", onScroll, { passive: true });

    // Reset inner opacity when scrolled back up
    window.addEventListener("scroll", () => {
        if (window.scrollY === 0) inner.style.opacity = "1";
    }, { passive: true });

});

// ── Compare bar (global) ──────────────────────────────────────────────────

function updateCompareBar() {
    const checked = Array.from(document.querySelectorAll(".compare-checkbox:checked"));
    const bar     = document.getElementById("compareBar");
    const link    = document.getElementById("compareLink");
    const count   = document.getElementById("compareCount");

    if (checked.length > 3) {
        event.target.checked = false;
        return;
    }

    if (checked.length < 2) {
        bar.style.display = "none";
        return;
    }

    const ids = checked.map(c => c.value).join(",");
    bar.style.display  = "flex";
    count.textContent  = `${checked.length} plan${checked.length > 1 ? "s" : ""} selected`;
    link.href          = `/compare?ids=${ids}`;
}

function clearCompare() {
    document.querySelectorAll(".compare-checkbox:checked").forEach(c => c.checked = false);
    document.getElementById("compareBar").style.display = "none";
}
