// Faz 7.4.3b — 404 Kayıtları → "Redirect Kur" tek-tık köprü.
// [data-notfound-redirect] butonuna tıklayınca SweetAlert input modal açılır;
// admin ToPath girer, onaylayınca aynı satırın hidden [data-redirect-form-for]
// formunu doldurup submit eder. Server tarafında RedirectsService.CreateAsync
// + (başarılıysa) NotFoundLog satırı silinir.
(function () {
    'use strict';

    function init() {
        document.querySelectorAll('[data-notfound-redirect]').forEach(setupButton);
    }

    function setupButton(btn) {
        btn.addEventListener('click', function () {
            const id = btn.dataset.notfoundId;
            const url = btn.dataset.notfoundUrl;
            if (!id || !url) return;

            const form = document.querySelector(`[data-redirect-form-for="${cssEscape(id)}"]`);
            if (!form) return;

            if (typeof Swal === 'undefined') {
                // Fallback: SweetAlert yüklenmemişse browser prompt.
                const to = window.prompt(`Yönlendirme hedefi (örn. /tr-TR/yeni-sayfa):\nKaynak: ${url}`);
                if (to && to.trim()) {
                    submitForm(form, to.trim(), '301');
                }
                return;
            }

            Swal.fire({
                title: 'Redirect Kur',
                html: `
                    <div class="text-start">
                        <p class="mb-2"><strong>Kaynak (404 URL):</strong></p>
                        <code class="d-block mb-3 text-break">${escapeHtml(url)}</code>
                        <label class="form-label">Hedef URL</label>
                        <input id="swal-to-path" class="swal2-input m-0 w-100" placeholder="/tr-TR/yeni-sayfa" type="text" />
                        <div class="mt-3">
                            <label class="form-label">Durum Kodu</label>
                            <select id="swal-status-code" class="swal2-select m-0 w-100">
                                <option value="301" selected>301 — Kalıcı</option>
                                <option value="302">302 — Geçici</option>
                            </select>
                        </div>
                    </div>
                `,
                showCancelButton: true,
                confirmButtonText: 'Yönlendirme oluştur',
                cancelButtonText: 'Vazgeç',
                reverseButtons: true,
                focusConfirm: false,
                preConfirm: () => {
                    const toInput = document.getElementById('swal-to-path');
                    const statusSelect = document.getElementById('swal-status-code');
                    const to = (toInput?.value || '').trim();
                    if (!to) {
                        Swal.showValidationMessage('Hedef URL zorunludur.');
                        return false;
                    }
                    return { to, status: statusSelect?.value || '301' };
                }
            }).then((res) => {
                if (res.isConfirmed && res.value) {
                    submitForm(form, res.value.to, res.value.status);
                }
            });
        });
    }

    function submitForm(form, toPath, statusCode) {
        const toInput = form.querySelector('input[name="toPath"]');
        const statusInput = form.querySelector('input[name="statusCode"]');
        if (toInput) toInput.value = toPath;
        if (statusInput) statusInput.value = statusCode;
        form.submit();
    }

    function escapeHtml(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    // GUID karakterleri zaten CSS-safe (hex + dash), ama defansif.
    function cssEscape(value) {
        return (window.CSS && window.CSS.escape) ? window.CSS.escape(value) : value;
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
