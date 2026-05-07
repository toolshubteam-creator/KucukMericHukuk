(function () {
    "use strict";

    function getAntiForgeryToken(scope) {
        const root = scope || document;
        const el = root.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : "";
    }

    function escapeHtml(s) {
        if (s === null || s === undefined) return "";
        return String(s).replace(/[&<>"']/g, function (c) {
            return ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c];
        });
    }

    async function uploadOne(file, url, token) {
        const fd = new FormData();
        fd.append("file", file);
        if (token) fd.append("__RequestVerificationToken", token);
        try {
            const res = await fetch(url, {
                method: "POST",
                body: fd,
                credentials: "same-origin",
            });
            return await res.json();
        } catch (err) {
            return { success: false, errorMessage: "Ağ hatası." };
        }
    }

    function init() {
        const zone = document.getElementById("media-upload-zone");
        if (!zone) return;
        const input = document.getElementById("media-upload-input");
        const pick = document.getElementById("media-upload-pick");
        const progress = document.getElementById("media-upload-progress");

        async function handleFiles(files) {
            if (!files || files.length === 0) return;
            progress.hidden = false;
            progress.innerHTML = "";
            const url = zone.dataset.uploadUrl;
            const token = getAntiForgeryToken(zone) || getAntiForgeryToken(document);

            const items = Array.from(files);
            for (let i = 0; i < items.length; i++) {
                const file = items[i];
                const row = document.createElement("div");
                row.className = "mb-2";
                row.innerHTML = '<div class="d-flex align-items-center gap-2">' +
                    '<div class="text-truncate flex-grow-1">' + escapeHtml(file.name) + '</div>' +
                    '<div class="text-secondary small mp-row-status">Yükleniyor...</div>' +
                    '</div>';
                progress.appendChild(row);

                const result = await uploadOne(file, url, token);
                const status = row.querySelector(".mp-row-status");
                if (result.success) {
                    status.textContent = "✓ Yüklendi";
                    status.className = "text-success small mp-row-status";
                } else {
                    status.textContent = result.errorMessage || "Hata";
                    status.className = "text-danger small mp-row-status";
                }
            }

            setTimeout(function () { window.location.reload(); }, 1200);
        }

        if (pick) pick.addEventListener("click", function () { input.click(); });
        input.addEventListener("change", function () {
            handleFiles(input.files);
            input.value = "";
        });
        ["dragenter", "dragover"].forEach(function (ev) {
            zone.addEventListener(ev, function (e) { e.preventDefault(); zone.classList.add("media-upload-zone-hover"); });
        });
        ["dragleave", "drop"].forEach(function (ev) {
            zone.addEventListener(ev, function (e) { e.preventDefault(); zone.classList.remove("media-upload-zone-hover"); });
        });
        zone.addEventListener("drop", function (e) { handleFiles(e.dataTransfer.files); });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
