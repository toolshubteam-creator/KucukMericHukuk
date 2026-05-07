(function () {
    'use strict';

    if (typeof Quill === 'undefined') {
        console.warn('[quill-init] Quill yuklenmedi.');
        return;
    }

    function imageHandlerFor(quill) {
        return function () {
            if (!window.MediaPicker || typeof window.MediaPicker.open !== 'function') {
                console.error('[quill-init] MediaPicker yuklenmemis. _MediaPickerModal partial ve media-picker.js sayfada olmali.');
                return;
            }
            window.MediaPicker.open(function (item) {
                if (!item || !item.url) return;
                const range = quill.getSelection(true);
                quill.insertEmbed(range.index, 'image', item.url, Quill.sources.USER);
                quill.setSelection(range.index + 1, 0, Quill.sources.SILENT);

                // Quill 2.x default image format alt attribute koymaz; picker'dan
                // gelen alt-text'i son <img>'a yansıt.
                if (item.altText) {
                    setTimeout(function () {
                        const imgs = quill.root.querySelectorAll('img');
                        const inserted = imgs[imgs.length - 1];
                        if (inserted && !inserted.alt) inserted.alt = item.altText;
                    }, 0);
                }
            });
        };
    }

    document.addEventListener('DOMContentLoaded', function () {
        const targets = document.querySelectorAll('textarea[data-quill-target="true"]');
        if (!targets.length) return;

        targets.forEach(function (textarea) {
            const container = document.createElement('div');
            container.className = 'quill-editor-container';
            container.innerHTML = textarea.value || '';

            textarea.parentNode.insertBefore(container, textarea);
            textarea.classList.add('d-none');

            const quill = new Quill(container, {
                theme: 'snow',
                modules: {
                    toolbar: {
                        container: [
                            [{ 'header': [2, 3, 4, false] }],
                            ['bold', 'italic', 'underline', 'strike'],
                            [{ 'list': 'ordered' }, { 'list': 'bullet' }],
                            ['blockquote', 'code-block'],
                            ['link', 'image'],
                            ['clean']
                        ]
                    }
                }
            });

            // Image handler — Quill instance'ından sonra register; toolbar config
            // içinde fonksiyon ref erken bind ediliyor.
            const toolbar = quill.getModule('toolbar');
            toolbar.addHandler('image', imageHandlerFor(quill));

            const form = textarea.closest('form');
            if (form) {
                form.addEventListener('submit', function () {
                    textarea.value = quill.root.innerHTML;
                });
            }
        });
    });
})();
