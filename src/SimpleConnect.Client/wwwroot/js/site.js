window.simpleConnect = {
    refreshTooltips: function () {
        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
            var instance = bootstrap.Tooltip.getInstance(el);
            if (instance) instance.dispose();
        });
        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
            new bootstrap.Tooltip(el);
        });
    },

    showModal: function (id, dotNetRef) {
        var el = document.getElementById(id);
        if (!el) return;
        el.addEventListener('hidden.bs.modal', function () {
            dotNetRef.invokeMethodAsync('OnModalHidden');
        }, { once: true });
        bootstrap.Modal.getOrCreateInstance(el).show();
    },

    hideModal: function (id) {
        var el = document.getElementById(id);
        if (!el) return;
        var instance = bootstrap.Modal.getInstance(el);
        if (instance) instance.hide();
    }
};
