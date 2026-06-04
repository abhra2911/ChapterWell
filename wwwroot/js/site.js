// Dark mode: read saved preference, apply before first paint
(function () {
    var saved = localStorage.getItem('theme');
    if (saved === 'dark') {
        document.documentElement.setAttribute('data-theme', 'dark');
    }
})();

document.addEventListener('DOMContentLoaded', function () {
    // Apply theme to html element (in case body wasn't ready above)
    var saved = localStorage.getItem('theme') || 'light';
    document.documentElement.setAttribute('data-theme', saved);

    // Wire up every toggle button (there may be one in _Layout and one in _PortalLayout)
    document.querySelectorAll('.theme-toggle-btn').forEach(function (btn) {
        updateToggleLabel(btn, saved);
        btn.addEventListener('click', function () {
            var current = document.documentElement.getAttribute('data-theme') || 'light';
            var next = current === 'dark' ? 'light' : 'dark';
            document.documentElement.setAttribute('data-theme', next);
            localStorage.setItem('theme', next);
            document.querySelectorAll('.theme-toggle-btn').forEach(function (b) {
                updateToggleLabel(b, next);
            });
        });
    });

    function updateToggleLabel(btn, theme) {
        var icon = btn.querySelector('.toggle-icon');
        var text = btn.querySelector('.toggle-text');
        if (icon) icon.textContent = theme === 'dark' ? '☀' : '🌙';
        if (text) text.textContent = theme === 'dark' ? 'Light' : 'Dark';
    }
});
