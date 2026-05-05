(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        const confirmForms = document.querySelectorAll('form[data-confirm]');

        confirmForms.forEach(function (form) {
            form.addEventListener('submit', function (e) {
                if (form.dataset.confirmed === 'true') {
                    return;
                }

                e.preventDefault();

                const message = form.dataset.confirm || 'Devam edilsin mi?';
                const variant = form.dataset.confirmVariant || 'default';

                if (typeof Swal === 'undefined') {
                    if (window.confirm(message)) {
                        form.dataset.confirmed = 'true';
                        form.submit();
                    }
                    return;
                }

                const swalConfig = {
                    title: form.dataset.confirmTitle || 'Onay gerekli',
                    text: message,
                    icon: variant === 'danger' ? 'warning' : 'question',
                    showCancelButton: true,
                    confirmButtonText: form.dataset.confirmButton || 'Onayla',
                    cancelButtonText: 'Vazgeç',
                    reverseButtons: true,
                    focusCancel: variant === 'danger'
                };

                if (variant === 'danger') {
                    swalConfig.confirmButtonColor = '#dc3545';
                }

                Swal.fire(swalConfig).then(function (result) {
                    if (result.isConfirmed) {
                        form.dataset.confirmed = 'true';
                        form.submit();
                    }
                });
            });
        });
    });
})();
