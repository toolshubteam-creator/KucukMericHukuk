(function () {
    "use strict";

    const state = {
        modal: null,
        callback: null,
        page: 1,
        keyword: "",
        hasNext: false,
    };

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

    function renderGrid(items) {
        const grid = document.getElementById("mp-grid");
        const empty = document.getElementById("mp-empty");
        if (items.length === 0) {
            grid.innerHTML = "";
            empty.hidden = false;
            return;
        }
        empty.hidden = true;
        grid.innerHTML = items.map(function (it) {
            return '<div class="col-6 col-md-4 col-lg-3">' +
                '<div class="card card-sm mp-card" data-id="' + it.id +
                '" data-url="' + escapeHtml(it.url) +
                '" data-alt="' + escapeHtml(it.altText || it.originalFileName) + '">' +
                '<img src="' + escapeHtml(it.thumbnailUrl) +
                '" alt="' + escapeHtml(it.altText || it.originalFileName) +
                '" class="card-img-top mp-card-thumb" loading="lazy" />' +
                '<div class="card-body p-2">' +
                '<div class="text-truncate small" title="' + escapeHtml(it.originalFileName) + '">' +
                escapeHtml(it.originalFileName) +
                '</div></div></div></div>';
        }).join("");

        grid.querySelectorAll(".mp-card").forEach(function (card) {
            card.addEventListener("click", function () {
                const id = parseInt(card.dataset.id, 10);
                const url = card.dataset.url;
                const alt = card.dataset.alt;
                if (state.callback) {
                    state.callback({ id: id, url: url, altText: alt });
                }
                state.modal.hide();
            });
        });
    }

    async function loadGallery() {
        const params = new URLSearchParams({ page: state.page, pageSize: 24 });
        if (state.keyword) params.set("keyword", state.keyword);
        try {
            const res = await fetch("/admin/medias/picker-list?" + params.toString(), { credentials: "same-origin" });
            if (!res.ok) {
                document.getElementById("mp-status").textContent = "Yüklenemedi.";
                return;
            }
            const data = await res.json();
            renderGrid(data.items || []);
            state.hasNext = !!data.hasNext;
            document.getElementById("mp-prev").disabled = state.page <= 1;
            document.getElementById("mp-next").disabled = !state.hasNext;
            document.getElementById("mp-status").textContent =
                "Sayfa " + data.pageNumber + " — toplam " + data.totalCount + " dosya";
        } catch (err) {
            document.getElementById("mp-status").textContent = "Ağ hatası.";
        }
    }

    async function uploadFile(file, zoneEl, onComplete) {
        const url = zoneEl.dataset.uploadUrl;
        const fd = new FormData();
        fd.append("file", file);
        const token = getAntiForgeryToken(zoneEl) || getAntiForgeryToken(document);
        if (token) fd.append("__RequestVerificationToken", token);
        try {
            const res = await fetch(url, {
                method: "POST",
                body: fd,
                credentials: "same-origin",
            });
            const data = await res.json();
            onComplete(data);
        } catch (err) {
            onComplete({ success: false, errorMessage: "Yükleme başarısız (ağ hatası)." });
        }
    }

    function bindUploadZone(zoneEl, inputEl, pickBtnEl, progressEl, onUploaded) {
        function handleFiles(files) {
            if (!files || files.length === 0) return;
            const file = files[0];
            progressEl.hidden = false;
            progressEl.innerHTML = '<div class="progress"><div class="progress-bar progress-bar-indeterminate"></div></div>' +
                '<div class="text-secondary small mt-1">' + escapeHtml(file.name) + ' yükleniyor...</div>';
            uploadFile(file, zoneEl, function (data) {
                if (data.success) {
                    progressEl.innerHTML = '<div class="alert alert-success">Yüklendi: ' + escapeHtml(data.originalFileName) + '</div>';
                    setTimeout(function () { progressEl.hidden = true; }, 1500);
                    if (onUploaded) onUploaded(data);
                } else {
                    progressEl.innerHTML = '<div class="alert alert-danger">' + escapeHtml(data.errorMessage || "Hata.") + '</div>';
                }
            });
        }

        if (pickBtnEl) {
            pickBtnEl.addEventListener("click", function () { inputEl.click(); });
        }
        inputEl.addEventListener("change", function () {
            handleFiles(inputEl.files);
            inputEl.value = "";
        });
        ["dragenter", "dragover"].forEach(function (ev) {
            zoneEl.addEventListener(ev, function (e) { e.preventDefault(); zoneEl.classList.add("media-upload-zone-hover"); });
        });
        ["dragleave", "drop"].forEach(function (ev) {
            zoneEl.addEventListener(ev, function (e) { e.preventDefault(); zoneEl.classList.remove("media-upload-zone-hover"); });
        });
        zoneEl.addEventListener("drop", function (e) {
            handleFiles(e.dataTransfer.files);
        });
    }

    window.MediaPicker = {
        open: function (callback) {
            state.callback = callback;
            state.page = 1;
            state.keyword = "";

            const modalEl = document.getElementById("mediaPickerModal");
            if (!modalEl) {
                console.error("MediaPickerModal partial not found on this page.");
                return;
            }
            state.modal = bootstrap.Modal.getOrCreateInstance(modalEl);

            if (!modalEl.dataset.bound) {
                modalEl.dataset.bound = "1";
                document.getElementById("mp-search-btn").addEventListener("click", function () {
                    state.keyword = document.getElementById("mp-search").value || "";
                    state.page = 1;
                    loadGallery();
                });
                document.getElementById("mp-search").addEventListener("keydown", function (e) {
                    if (e.key === "Enter") { e.preventDefault(); document.getElementById("mp-search-btn").click(); }
                });
                document.getElementById("mp-prev").addEventListener("click", function () {
                    if (state.page > 1) { state.page--; loadGallery(); }
                });
                document.getElementById("mp-next").addEventListener("click", function () {
                    if (state.hasNext) { state.page++; loadGallery(); }
                });

                const zone = document.getElementById("mp-upload-zone");
                const input = document.getElementById("mp-upload-input");
                const pick = document.getElementById("mp-upload-pick");
                const progress = document.getElementById("mp-upload-progress");
                bindUploadZone(zone, input, pick, progress, function (uploaded) {
                    if (state.callback) {
                        state.callback({ id: uploaded.id, url: uploaded.url, altText: uploaded.altText || uploaded.originalFileName });
                    }
                    state.modal.hide();
                });
            }

            loadGallery();
            const galleryTab = document.querySelector('[data-bs-target="#mp-gallery"]');
            if (galleryTab) bootstrap.Tab.getOrCreateInstance(galleryTab).show();

            state.modal.show();
        }
    };
})();
