/* Küçükmeriç Hukuk — Public Site JS
   Inline JS yasak; tüm public scripts bu dosyada veya
   wwwroot/js/site/'da modüler dosyalarda (Faz 4.2+).
   Vanilla JS, ES2020+, no jQuery. */

(function () {
    "use strict";

    // Lucide icons — data-lucide attribute'larını SVG'ye dönüştür
    if (window.lucide && typeof window.lucide.createIcons === "function") {
        window.lucide.createIcons();
    }

    // Scroll-triggered fade-up — prefers-reduced-motion respect
    const reduced = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (reduced || !("IntersectionObserver" in window)) {
        return;
    }

    const items = document.querySelectorAll("[data-fade-up]");
    items.forEach((el) => {
        el.style.opacity = "0";
        el.style.transform = "translateY(20px)";
        el.style.transition = "opacity 0.6s ease, transform 0.6s ease";
    });

    const obs = new IntersectionObserver(
        (entries) => {
            entries.forEach((entry) => {
                if (entry.isIntersecting) {
                    entry.target.style.opacity = "1";
                    entry.target.style.transform = "translateY(0)";
                    obs.unobserve(entry.target);
                }
            });
        },
        { threshold: 0.1, rootMargin: "0px 0px -50px 0px" }
    );

    items.forEach((el) => obs.observe(el));
})();
