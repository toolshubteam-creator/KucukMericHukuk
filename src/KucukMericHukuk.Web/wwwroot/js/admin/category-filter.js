// Faz 6.20: CSP nonce hardening — Categories/Index'teki inline onchange handler'ı
// harici dosyaya taşıdı. [data-auto-submit] taşıyan form elemanı değişince formu submit eder.
(function () {
    'use strict';
    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-auto-submit]').forEach(function (el) {
            el.addEventListener('change', function () {
                if (el.form) el.form.submit();
            });
        });
    });
})();
