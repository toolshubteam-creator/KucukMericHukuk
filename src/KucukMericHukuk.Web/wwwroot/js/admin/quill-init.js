(function () {
    'use strict';

    if (typeof Quill === 'undefined') {
        console.warn('[quill-init] Quill yuklenmedi.');
        return;
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
                    toolbar: [
                        [{ 'header': [2, 3, 4, false] }],
                        ['bold', 'italic', 'underline', 'strike'],
                        [{ 'list': 'ordered' }, { 'list': 'bullet' }],
                        ['blockquote', 'code-block'],
                        ['link'],
                        ['clean']
                    ]
                }
            });

            const form = textarea.closest('form');
            if (form) {
                form.addEventListener('submit', function () {
                    textarea.value = quill.root.innerHTML;
                });
            }
        });
    });
})();
