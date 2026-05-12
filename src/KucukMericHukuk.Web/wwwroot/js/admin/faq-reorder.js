// Faz 6.6b: Faq Index drag-drop reorder.
// Mevcut Edit form DisplayOrder input edge-case olarak korunur; bu JS ana akistir.
(function () {
    'use strict';

    if (typeof Sortable === 'undefined') {
        console.warn('[faq-reorder] SortableJS yuklenmedi.');
        return;
    }

    const tbody = document.getElementById('faq-sortable');
    if (!tbody) return;

    // Filtrelenmis liste durumunda drag-drop devre disi (data attribute kontrol)
    if (tbody.dataset.reorderEnabled !== 'true') return;

    const reorderUrl = tbody.dataset.reorderUrl;
    if (!reorderUrl) {
        console.error('[faq-reorder] data-reorder-url eksik.');
        return;
    }

    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    if (!tokenInput) {
        console.error('[faq-reorder] Anti-forgery token bulunamadi.');
        return;
    }
    const token = tokenInput.value;

    Sortable.create(tbody, {
        handle: '.faq-drag-handle',
        animation: 150,
        ghostClass: 'faq-sortable-ghost',
        chosenClass: 'faq-sortable-chosen',
        onEnd: function () {
            const rows = Array.from(tbody.querySelectorAll('tr[data-faq-id]'));
            const items = rows.map(function (row, index) {
                return {
                    id: parseInt(row.dataset.faqId, 10),
                    displayOrder: index
                };
            });

            fetch(reorderUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token,
                    'Accept': 'application/json'
                },
                body: JSON.stringify(items),
                credentials: 'same-origin'
            })
                .then(function (response) {
                    if (!response.ok) {
                        return response.json().then(function (err) {
                            throw new Error(err.error || 'Sıralama kaydedilemedi.');
                        }).catch(function () {
                            throw new Error('Sıralama kaydedilemedi.');
                        });
                    }
                    // Sıra sütunundaki DisplayOrder badge'lerini de güncelle (görsel tutarlilik)
                    rows.forEach(function (row, index) {
                        const cells = row.querySelectorAll('td');
                        // 0: drag handle, 1: DisplayOrder
                        if (cells.length >= 2) cells[1].textContent = index;
                    });

                    if (typeof Swal !== 'undefined') {
                        Swal.fire({
                            icon: 'success',
                            title: 'Sıralama kaydedildi',
                            timer: 1200,
                            showConfirmButton: false,
                            toast: true,
                            position: 'top-end'
                        });
                    }
                })
                .catch(function (err) {
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({
                            icon: 'error',
                            title: 'Sıralama kaydedilemedi',
                            text: err.message,
                            confirmButtonText: 'Tamam'
                        }).then(function () {
                            window.location.reload();
                        });
                    } else {
                        window.location.reload();
                    }
                });
        }
    });
})();
