// Faz 7.4.3a — Redirect admin formu: ToPath değişiminde canlı döngü kontrolü.
// Form üzerindeki [data-redirect-form] container'da [data-from-path], [data-to-path],
// [data-cycle-warning] elemanları aranır. Endpoint URL + excludeId data-attribute'larda.
(function () {
    'use strict';

    function init() {
        const forms = document.querySelectorAll('[data-redirect-form]');
        forms.forEach(setupForm);
    }

    function setupForm(form) {
        const fromInput = form.querySelector('[data-from-path]');
        const toInput = form.querySelector('[data-to-path]');
        const warning = form.querySelector('[data-cycle-warning]');
        const endpoint = form.dataset.checkCycleUrl;
        const excludeId = form.dataset.excludeId;

        if (!fromInput || !toInput || !warning || !endpoint) return;

        let debounceTimer = null;
        function check() {
            const from = (fromInput.value || '').trim();
            const to = (toInput.value || '').trim();
            if (!from || !to) {
                hide();
                return;
            }
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(() => callApi(from, to), 350);
        }

        function callApi(from, to) {
            const url = new URL(endpoint, window.location.origin);
            url.searchParams.set('from', from);
            url.searchParams.set('to', to);
            if (excludeId) url.searchParams.set('excludeId', excludeId);

            fetch(url.toString(), { headers: { 'Accept': 'application/json' } })
                .then(r => r.ok ? r.json() : null)
                .then(data => {
                    if (!data) return;
                    if (data.ok) hide();
                    else show(data.message || 'Bu zincir döngü oluşturur.');
                })
                .catch(() => { /* sessiz fail — server validation devam eder */ });
        }

        function show(msg) {
            warning.textContent = msg;
            warning.classList.remove('d-none');
        }

        function hide() {
            warning.classList.add('d-none');
            warning.textContent = '';
        }

        fromInput.addEventListener('blur', check);
        toInput.addEventListener('blur', check);
        toInput.addEventListener('input', check);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
