document.addEventListener("DOMContentLoaded", () => {

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

            // Populate hidden form fields
            document.getElementById("hiddenPlanId").value   = btn.dataset.planId   ?? "";
            document.getElementById("hiddenPlanName").value = btn.dataset.plan      ?? "";
            document.getElementById("hiddenTier").value     = btn.dataset.tier      ?? "";
            document.getElementById("hiddenProvider").value = btn.dataset.provider  ?? "";
            document.getElementById("hiddenPremium").value  = btn.dataset.premium   ?? "";
            document.getElementById("hiddenCoverage").value = btn.dataset.coverage  ?? "";
            document.getElementById("hiddenType").value     = btn.dataset.type      ?? "";
            document.getElementById("hiddenFeatures").value = btn.dataset.features  ?? "";

            // Populate modal display
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

    window.addEventListener("click", e => {
        if (e.target === modal) closeModal();
    });

    document.addEventListener("keydown", e => {
        if (e.key === "Escape") closeModal();
    });

    // ── Live search ────────────────────────────────────────────────────────

    searchBox?.addEventListener("input", () => {
        const q = searchBox.value.toLowerCase().trim();

        document.querySelectorAll(".insurance-plan").forEach(card => {
            const name     = card.querySelector(".plan-name")?.textContent.toLowerCase() ?? "";
            const type     = card.dataset.type?.toLowerCase() ?? "";
            const provider = card.querySelector(".plan-provider")?.textContent.toLowerCase() ?? "";
            card.style.display = (!q || name.includes(q) || type.includes(q) || provider.includes(q)) ? "" : "none";
        });
    });

    // ── Auto-dismiss toast after 5 s ──────────────────────────────────────

    const toast = document.getElementById("toast");
    if (toast) setTimeout(() => toast.remove(), 5000);

});
