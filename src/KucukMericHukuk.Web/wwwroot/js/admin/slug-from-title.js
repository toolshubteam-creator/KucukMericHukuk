(function () {
    'use strict';
    function slugify(text) {
        return (text || '')
            .toString()
            .toLowerCase()
            .replace(/ı/g, 'i').replace(/ğ/g, 'g').replace(/ü/g, 'u')
            .replace(/ş/g, 's').replace(/ö/g, 'o').replace(/ç/g, 'c')
            .replace(/[^a-z0-9\s-]/g, '')
            .trim()
            .replace(/\s+/g, '-')
            .replace(/-+/g, '-');
    }
    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('input[data-slug-source]').forEach(function (titleInput) {
            var targetSel = titleInput.dataset.slugSource;
            var slugInput = document.querySelector(targetSel);
            if (!slugInput) return;
            var userTouched = slugInput.value.length > 0;
            slugInput.addEventListener('input', function () { userTouched = true; });
            titleInput.addEventListener('input', function () {
                if (!userTouched) slugInput.value = slugify(titleInput.value);
            });
        });
    });
})();
