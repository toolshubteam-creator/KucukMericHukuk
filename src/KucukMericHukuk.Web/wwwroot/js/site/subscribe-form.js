/* Küçükmeriç Hukuk — Subscribe Form AJAX (Faz 7.2a-fix)
   Vanilla JS, no jQuery. Progressive enhancement:
   - JS aktif: fetch POST + inline mesaj + turnstile.reset
   - JS pasif: form native submit eder → controller fallback redirect döner */

(function () {
    "use strict";

    var form = document.querySelector('[data-subscribe-form]');
    if (!form) return;

    var messageBox = form.querySelector('[data-subscribe-message]');
    var emailInput = form.querySelector('input[name="Email"]');
    var kvkkInput = form.querySelector('input[name="KvkkConsent"][type="checkbox"]');
    var submitBtn = form.querySelector('button[type="submit"]');
    var turnstileEl = form.querySelector('.cf-turnstile');

    function showMessage(success, text) {
        if (!messageBox) return;
        messageBox.textContent = text || '';
        messageBox.classList.remove('subscribe-band__message--success', 'subscribe-band__message--error');
        messageBox.classList.add(success ? 'subscribe-band__message--success' : 'subscribe-band__message--error');
        messageBox.removeAttribute('hidden');
    }

    function resetTurnstile() {
        if (window.turnstile && turnstileEl && typeof window.turnstile.reset === 'function') {
            try { window.turnstile.reset(turnstileEl); } catch (e) { /* defansif */ }
        }
    }

    function setBusy(busy) {
        if (submitBtn) submitBtn.disabled = busy;
    }

    form.addEventListener('submit', function (event) {
        event.preventDefault();
        setBusy(true);

        var formData = new FormData(form);

        fetch(form.action, {
            method: 'POST',
            body: formData,
            credentials: 'same-origin',
            headers: { 'Accept': 'application/json' }
        })
            .then(function (response) {
                var ct = response.headers.get('content-type') || '';
                if (!ct.indexOf || ct.indexOf('application/json') === -1) {
                    throw new Error('non-json-response');
                }
                return response.json();
            })
            .then(function (data) {
                showMessage(!!data.success, data.message || (data.success ? 'Tamamlandi.' : 'Bir hata olustu.'));
                if (data.success) {
                    if (emailInput) emailInput.value = '';
                    if (kvkkInput) kvkkInput.checked = false;
                }
            })
            .catch(function () {
                showMessage(false, 'Bir hata oluştu. Lütfen daha sonra tekrar deneyin.');
            })
            .then(function () {
                // Turnstile token tek kullanimlik — basari/basarisiz fark etmeksizin reset.
                resetTurnstile();
                setBusy(false);
            });
    });
})();
