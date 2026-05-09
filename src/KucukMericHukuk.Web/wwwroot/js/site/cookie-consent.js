/* Küçükmeriç Hukuk — Public Site Cookie Consent
   Vanilla JS, no jQuery. localStorage flag ile dismissible. */

(function () {
    "use strict";

    var STORAGE_KEY = 'kmh_cookie_consent_v1';
    var banner = document.getElementById('cookie-consent');
    var acceptBtn = document.getElementById('cookie-consent-accept');

    if (!banner || !acceptBtn) return;

    try {
        if (localStorage.getItem(STORAGE_KEY) === 'accepted') {
            return; // zaten kabul edilmiş, banner gösterilmez
        }
    } catch (e) {
        // localStorage erişim sorunu (private mode vs.) — banner göster
    }

    banner.removeAttribute('hidden');

    acceptBtn.addEventListener('click', function () {
        try {
            localStorage.setItem(STORAGE_KEY, 'accepted');
        } catch (e) {
            // sessizce devam — kullanıcı gene de kapatabilsin
        }
        banner.setAttribute('hidden', '');
    });
})();
