(function () {
    "use strict";

    function init() {
        document.querySelectorAll("[data-media-picker-target]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                const targetSelector = btn.dataset.mediaPickerTarget;
                const previewSelector = btn.dataset.mediaPreview;
                const targetEl = targetSelector ? document.querySelector(targetSelector) : null;
                const previewEl = previewSelector ? document.querySelector(previewSelector) : null;
                if (!targetEl) return;
                if (!window.MediaPicker) {
                    console.error("MediaPicker not loaded.");
                    return;
                }
                window.MediaPicker.open(function (item) {
                    targetEl.value = item.url;
                    targetEl.dispatchEvent(new Event("change", { bubbles: true }));
                    if (previewEl) {
                        previewEl.src = item.url;
                        previewEl.alt = item.altText || "";
                        previewEl.hidden = false;
                    }
                });
            });
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
