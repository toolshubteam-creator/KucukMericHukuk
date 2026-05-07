(function () {
    'use strict';
    document.addEventListener('DOMContentLoaded', function () {
        if (typeof Choices === 'undefined') {
            console.warn('[choices-init] Choices.js yuklenmedi.');
            return;
        }
        document.querySelectorAll('select[data-choices]').forEach(function (sel) {
            new Choices(sel, {
                removeItemButton: true,
                shouldSort: false,
                placeholder: true,
                placeholderValue: 'Etiket seçin veya yazın...',
                searchPlaceholderValue: 'Ara...',
                noResultsText: 'Sonuç bulunamadı',
                noChoicesText: 'Seçenek kalmadı',
                itemSelectText: 'Seç',
            });
        });
    });
})();
