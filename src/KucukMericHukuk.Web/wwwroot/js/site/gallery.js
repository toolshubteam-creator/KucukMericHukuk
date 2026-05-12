// Faz 6.5: Native lightbox for /Galeri.
// No external lib (KISS). Click thumbnail -> open full image; ESC or click outside -> close.
(function () {
    'use strict';

    const lightbox = document.getElementById('gallery-lightbox');
    if (!lightbox) return;

    const lightboxImage = lightbox.querySelector('.gallery-lightbox__image');
    const closeButton = lightbox.querySelector('.gallery-lightbox__close');
    const triggers = document.querySelectorAll('.gallery-item');

    function openLightbox(url, alt) {
        if (!url) return;
        lightboxImage.src = url;
        lightboxImage.alt = alt || '';
        lightbox.removeAttribute('hidden');
        lightbox.classList.add('is-open');
        document.body.style.overflow = 'hidden';
        closeButton.focus();
    }

    function closeLightbox() {
        lightbox.classList.remove('is-open');
        lightbox.setAttribute('hidden', '');
        lightboxImage.src = '';
        lightboxImage.alt = '';
        document.body.style.overflow = '';
    }

    triggers.forEach(function (trigger) {
        trigger.addEventListener('click', function () {
            openLightbox(trigger.dataset.imageUrl, trigger.dataset.imageAlt);
        });
    });

    closeButton.addEventListener('click', function (e) {
        e.stopPropagation();
        closeLightbox();
    });

    // Click on backdrop (anywhere outside the image) closes
    lightbox.addEventListener('click', function (e) {
        if (e.target === lightbox) {
            closeLightbox();
        }
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && lightbox.classList.contains('is-open')) {
            closeLightbox();
        }
    });
})();
