(function () {
    const toggle = document.querySelector('[data-admin-user-menu-toggle]');

    if (!toggle || !window.bootstrap || !window.bootstrap.Dropdown) {
        return;
    }

    const dropdown = window.bootstrap.Dropdown.getOrCreateInstance(toggle, {
        autoClose: true,
        popperConfig: null
    });

    toggle.addEventListener('click', function (event) {
        event.preventDefault();
        event.stopImmediatePropagation();
        dropdown.toggle();
    }, true);
})();
