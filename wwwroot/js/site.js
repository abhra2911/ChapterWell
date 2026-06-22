/* ============================================================================
   The Central Library — site scripts
   ========================================================================== */

/* ---------------------------------------------------------------------------
   Theme: toggle + label sync.
   NOTE: the *initial* theme is applied by a tiny inline script in the <head>
   of each layout (before first paint) so navigating between server-rendered
   pages never flashes the wrong theme. This file only handles the toggle and
   keeps the button label/icon in sync.
   ------------------------------------------------------------------------- */
function currentTheme() {
    return document.documentElement.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
}

function syncThemeButtons() {
    var dark = currentTheme() === 'dark';
    var label = dark ? 'Light Mode' : 'Dark Mode';
    var icon = dark ? 'bi-sun' : 'bi-moon-stars';
    document.querySelectorAll('.dark-mode-btn').forEach(function (btn) {
        btn.innerHTML = '<i class="bi ' + icon + '"></i><span>' + label + '</span>';
        btn.setAttribute('aria-label', 'Switch to ' + (dark ? 'light' : 'dark') + ' mode');
    });
}

function toggleTheme() {
    var next = currentTheme() === 'dark' ? 'light' : 'dark';
    document.documentElement.setAttribute('data-theme', next);
    try { localStorage.setItem('theme', next); } catch (e) { /* ignore */ }
    syncThemeButtons();
    if (typeof window.onThemeChange === 'function') { window.onThemeChange(); }
}

/* ---------------------------------------------------------------------------
   Toasts — success/error notifications shown on the parent page.
   The server renders a <div id="toast-data" data-message data-kind> when there
   is a TempData success message; we read it on load and pop a toast.
   ------------------------------------------------------------------------- */
function ensureToastStack() {
    var stack = document.querySelector('.toast-stack');
    if (!stack) {
        stack = document.createElement('div');
        stack.className = 'toast-stack';
        document.body.appendChild(stack);
    }
    return stack;
}

function showToast(message, kind) {
    if (!message) return;
    var stack = ensureToastStack();
    var icon = kind === 'error' ? 'bi-exclamation-octagon-fill' : 'bi-check-circle-fill';
    var el = document.createElement('div');
    el.className = 'app-toast' + (kind === 'error' ? ' error' : '');
    el.setAttribute('role', 'status');
    el.innerHTML =
        '<i class="bi ' + icon + '"></i>' +
        '<div class="toast-text"></div>' +
        '<button type="button" class="toast-close" aria-label="Dismiss">&times;</button>';
    el.querySelector('.toast-text').textContent = message;

    function dismiss() {
        el.classList.add('hiding');
        setTimeout(function () { el.remove(); }, 250);
    }
    el.querySelector('.toast-close').addEventListener('click', dismiss);
    stack.appendChild(el);
    setTimeout(dismiss, 4200);
}

function initToastsFromServer() {
    var data = document.getElementById('toast-data');
    if (!data) return;
    var msg = data.getAttribute('data-message');
    var kind = data.getAttribute('data-kind') || 'success';
    if (msg) showToast(msg, kind);
}

/* ---------------------------------------------------------------------------
   In-modal validation.
   Every modal form gets validated client-side first. On failure we show a
   friendly message *inside the modal* (.modal-error) instead of letting the
   browser's native bubbles fire, and we never submit. On success the form
   posts normally; the controller then redirects with a success message that
   surfaces as a toast on the parent page (after the modal has closed).
   ------------------------------------------------------------------------- */
function getModalError(form) {
    var modal = form.closest('.modal');
    return modal ? modal.querySelector('.modal-error') : null;
}

function setModalError(form, message) {
    var box = getModalError(form);
    if (!box) return;
    box.querySelector('.modal-error-text').textContent = message;
    box.hidden = false;
}

function clearModalError(form) {
    var box = getModalError(form);
    if (box) box.hidden = true;
}

/* A friendlier message than the raw browser default for common cases. */
function fieldMessage(field) {
    var label = field.getAttribute('data-label') || field.getAttribute('placeholder') || 'This field';
    if (field.validity.valueMissing) return label + ' is required.';
    if (field.validity.typeMismatch && field.type === 'email') return 'Please enter a valid email address.';
    if (field.validity.tooShort) return label + ' must be at least ' + field.minLength + ' characters.';
    if (field.validity.rangeUnderflow || field.validity.rangeOverflow) return 'Please enter a valid value for ' + label + '.';
    return field.validationMessage || (label + ' is invalid.');
}

function initModalValidation() {
    document.querySelectorAll('.modal form').forEach(function (form) {
        form.setAttribute('novalidate', 'novalidate');

        form.addEventListener('submit', function (e) {
            clearModalError(form);

            // Native constraint validation first.
            var firstInvalid = null;
            var fields = form.querySelectorAll('input, select, textarea');
            for (var i = 0; i < fields.length; i++) {
                if (!fields[i].checkValidity()) { firstInvalid = fields[i]; break; }
            }
            if (firstInvalid) {
                e.preventDefault();
                setModalError(form, fieldMessage(firstInvalid));
                firstInvalid.focus();
                return;
            }

            // Cross-field rule: "confirm password" must match "new password".
            var pw = form.querySelector('[data-role="new-password"]');
            var confirm = form.querySelector('[data-role="confirm-password"]');
            if (pw && confirm && pw.value !== confirm.value) {
                e.preventDefault();
                setModalError(form, 'The new password and confirmation do not match.');
                confirm.focus();
                return;
            }
            // valid -> allow normal POST
        });
    });

    // Clear stale errors each time a modal is reopened by the user.
    document.querySelectorAll('.modal').forEach(function (modal) {
        modal.addEventListener('hidden.bs.modal', function () {
            var box = modal.querySelector('.modal-error');
            if (box && !box.hasAttribute('data-server')) box.hidden = true;
        });
    });
}

/* ---------------------------------------------------------------------------
   Data-driven modals: fill a shared modal from the clicked trigger's data-*.
   Used by Edit Book, Delete Book, View Member and Book Details.
   ------------------------------------------------------------------------- */
function initDataModals() {
    document.querySelectorAll('.modal[data-fillable]').forEach(function (modal) {
        modal.addEventListener('show.bs.modal', function (event) {
            var trigger = event.relatedTarget;
            if (!trigger) return;

            // Any element inside the modal with data-field="x" gets the value
            // of the trigger's data-x attribute (text content or input value).
            modal.querySelectorAll('[data-field]').forEach(function (slot) {
                var key = slot.getAttribute('data-field');
                var val = trigger.getAttribute('data-' + key);
                if (val === null) val = '';
                if ('value' in slot && (slot.tagName === 'INPUT' || slot.tagName === 'SELECT' || slot.tagName === 'TEXTAREA')) {
                    slot.value = val;
                } else if (slot.tagName === 'IMG') {
                    slot.src = val || '/images/placeholder-cover.svg';
                } else {
                    slot.textContent = val;
                }
            });

            // Optional availability pill in details modals.
            var pill = modal.querySelector('[data-avail-pill]');
            if (pill) {
                var avail = trigger.getAttribute('data-available') === 'true';
                pill.className = 'avail-pill ' + (avail ? 'yes' : 'no');
                pill.textContent = avail ? 'Available' : 'Unavailable';
            }
        });
    });
}

/* ---------------------------------------------------------------------------
   Live client-side catalog filtering (search + genre + availability).
   Works over any container marked [data-filter-root]; each item carries
   data-title / data-author / data-genre / data-available.
   ------------------------------------------------------------------------- */
function initCatalogFilters() {
    document.querySelectorAll('[data-filter-root]').forEach(function (root) {
        var search = root.querySelector('[data-filter-search]');
        var chips = root.querySelectorAll('[data-filter-genre]');
        var availChips = root.querySelectorAll('[data-filter-avail]');
        var items = root.querySelectorAll('[data-filter-item]');
        var countEl = root.querySelector('[data-filter-count]');
        var emptyEl = root.querySelector('[data-filter-empty]');

        var state = { q: '', genre: 'all', avail: 'all' };

        function apply() {
            var shown = 0;
            items.forEach(function (item) {
                var title = (item.getAttribute('data-title') || '').toLowerCase();
                var author = (item.getAttribute('data-author') || '').toLowerCase();
                var genre = (item.getAttribute('data-genre') || '').toLowerCase();
                var available = item.getAttribute('data-available') === 'true';

                var matchQ = !state.q || title.indexOf(state.q) > -1 || author.indexOf(state.q) > -1 || genre.indexOf(state.q) > -1;
                var matchG = state.genre === 'all' || genre === state.genre;
                var matchA = state.avail === 'all' || (state.avail === 'available' && available);

                var visible = matchQ && matchG && matchA;
                item.style.display = visible ? '' : 'none';
                if (visible) shown++;
            });
            if (countEl) countEl.textContent = shown + (shown === 1 ? ' result' : ' results');
            if (emptyEl) emptyEl.style.display = shown === 0 ? '' : 'none';
        }

        if (search) {
            search.addEventListener('input', function () {
                state.q = search.value.trim().toLowerCase();
                apply();
            });
        }

        chips.forEach(function (chip) {
            chip.addEventListener('click', function () {
                chips.forEach(function (c) { c.classList.remove('active'); });
                chip.classList.add('active');
                state.genre = (chip.getAttribute('data-filter-genre') || 'all').toLowerCase();
                apply();
            });
        });

        availChips.forEach(function (chip) {
            chip.addEventListener('click', function () {
                availChips.forEach(function (c) { c.classList.remove('active'); });
                chip.classList.add('active');
                state.avail = (chip.getAttribute('data-filter-avail') || 'all').toLowerCase();
                apply();
            });
        });

        apply();
    });
}

/* ---------------------------------------------------------------------------
   Top-books carousel: prev/next scroll buttons.
   ------------------------------------------------------------------------- */
function initBookCarousels() {
    document.querySelectorAll('.carousel-section').forEach(function (section) {
        var track = section.querySelector('.book-carousel');
        var prev = section.querySelector('.carousel-btn.prev');
        var next = section.querySelector('.carousel-btn.next');
        if (!track || !prev || !next) return;

        function step() { return Math.max(track.clientWidth * 0.85, 200); }

        prev.addEventListener('click', function () { track.scrollBy({ left: -step(), behavior: 'smooth' }); });
        next.addEventListener('click', function () { track.scrollBy({ left: step(), behavior: 'smooth' }); });

        function updateButtons() {
            var slack = 8;
            var maxScroll = track.scrollWidth - track.clientWidth;
            prev.disabled = track.scrollLeft <= slack;
            next.disabled = track.scrollLeft >= maxScroll - slack;
        }

        track.addEventListener('scroll', updateButtons);
        window.addEventListener('resize', updateButtons);
        updateButtons();
    });
}

/* ---------------------------------------------------------------------------
   Issue Book modal: ISBN lookup + title/author typeahead, in place of the
   old long <select>. Both modes write to the same hidden #issue-book input
   so the existing validation/POST contract is untouched.
   ------------------------------------------------------------------------- */
function initIssueBookPicker() {
    var modal = document.getElementById('issueBookModal');
    if (!modal) return;

    var booksDataEl = document.getElementById('issuable-books-data');
    var books = [];
    try { books = JSON.parse(booksDataEl.textContent || '[]'); } catch (e) { books = []; }

    var hiddenInput = modal.querySelector('#issue-book');
    var submitBtn = modal.querySelector('#issue-submit-btn');

    var modeBtns = modal.querySelectorAll('.issue-mode-btn');
    var panels = modal.querySelectorAll('.issue-mode-panel');

    var isbnInput = modal.querySelector('#issue-isbn');
    var isbnResult = modal.querySelector('#issue-isbn-result');

    var searchInput = modal.querySelector('#issue-search');
    var searchList = modal.querySelector('#issue-search-list');
    var searchResult = modal.querySelector('#issue-search-result');

    function normalizeIsbn(s) {
        return (s || '').replace(/[\s-]/g, '').toUpperCase();
    }

    function setSelectedBook(book, resultEl) {
        hiddenInput.value = book.id;
        submitBtn.disabled = false;
        resultEl.innerHTML =
            '<div class="issue-book-card">' +
            '<div class="ibc-info">' +
            '<span class="ibc-title">' + escapeHtml(book.title) + '</span>' +
            '<span class="ibc-meta">' + escapeHtml(book.author) + ' &middot; ' + book.available + ' copy' + (book.available === 1 ? '' : 'ies') + ' available</span>' +
            '</div>' +
            '<button type="button" class="ibc-clear" aria-label="Clear selection">&times;</button>' +
            '</div>';
        resultEl.querySelector('.ibc-clear').addEventListener('click', function () {
            clearSelection();
        });
    }

    function setError(message, resultEl) {
        hiddenInput.value = '';
        submitBtn.disabled = true;
        resultEl.innerHTML =
            '<div class="issue-book-card error">' +
            '<div class="ibc-info"><span class="ibc-title">' + escapeHtml(message) + '</span></div>' +
            '</div>';
    }

    function clearSelection() {
        hiddenInput.value = '';
        submitBtn.disabled = true;
        isbnResult.innerHTML = '';
        searchResult.innerHTML = '';
        isbnInput.value = '';
        searchInput.value = '';
        searchList.hidden = true;
    }

    function escapeHtml(s) {
        var div = document.createElement('div');
        div.textContent = s == null ? '' : s;
        return div.innerHTML;
    }

    // ---- Mode toggle (ISBN vs Search) -------------------------------------
    modeBtns.forEach(function (btn) {
        btn.addEventListener('click', function () {
            modeBtns.forEach(function (b) {
                b.classList.toggle('active', b === btn);
                b.setAttribute('aria-selected', b === btn ? 'true' : 'false');
            });
            var mode = btn.getAttribute('data-mode');
            panels.forEach(function (p) {
                p.classList.toggle('d-none', p.getAttribute('data-panel') !== mode);
            });
            clearSelection();
        });
    });

    // ---- ISBN lookup --------------------------------------------------------
    isbnInput.addEventListener('input', function () {
        var raw = isbnInput.value.trim();
        if (!raw) { isbnResult.innerHTML = ''; hiddenInput.value = ''; submitBtn.disabled = true; return; }

        var target = normalizeIsbn(raw);
        var match = books.find(function (b) { return normalizeIsbn(b.isbn) === target; });

        if (!match) {
            setError('No available book matches that ISBN.', isbnResult);
            return;
        }
        if (match.available <= 0) {
            setError(match.title + ' has no copies available right now.', isbnResult);
            return;
        }
        setSelectedBook(match, isbnResult);
    });

    // ---- Title / author typeahead -----------------------------------------
    var highlightedIndex = -1;
    var currentMatches = [];

    function renderSearchList(matches) {
        currentMatches = matches;
        highlightedIndex = -1;

        if (matches.length === 0) {
            searchList.innerHTML = '<div class="book-typeahead-empty">No matching books found.</div>';
            searchList.hidden = false;
            return;
        }
        searchList.innerHTML = matches.map(function (b, i) {
            return '<div class="book-typeahead-item" data-index="' + i + '">' +
                '<span class="ta-title">' + escapeHtml(b.title) + '</span>' +
                '<span class="ta-meta">' + escapeHtml(b.author) + ' &middot; ' + b.available + ' available</span>' +
                '</div>';
        }).join('');
        searchList.hidden = false;

        searchList.querySelectorAll('.book-typeahead-item').forEach(function (el) {
            el.addEventListener('click', function () {
                var book = currentMatches[parseInt(el.getAttribute('data-index'), 10)];
                searchInput.value = book.title;
                searchList.hidden = true;
                setSelectedBook(book, searchResult);
            });
        });
    }

    searchInput.addEventListener('input', function () {
        var q = searchInput.value.trim().toLowerCase();
        searchResult.innerHTML = '';
        hiddenInput.value = '';
        submitBtn.disabled = true;

        if (q.length < 2) { searchList.hidden = true; return; }

        var matches = books.filter(function (b) {
            return b.title.toLowerCase().indexOf(q) !== -1 || b.author.toLowerCase().indexOf(q) !== -1;
        }).slice(0, 8);

        renderSearchList(matches);
    });

    searchInput.addEventListener('keydown', function (e) {
        var items = searchList.querySelectorAll('.book-typeahead-item');
        if (searchList.hidden || items.length === 0) return;

        if (e.key === 'ArrowDown') {
            e.preventDefault();
            highlightedIndex = Math.min(highlightedIndex + 1, items.length - 1);
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            highlightedIndex = Math.max(highlightedIndex - 1, 0);
        } else if (e.key === 'Enter') {
            e.preventDefault();
            if (highlightedIndex >= 0) items[highlightedIndex].click();
            return;
        } else if (e.key === 'Escape') {
            searchList.hidden = true;
            return;
        } else {
            return;
        }
        items.forEach(function (el, i) { el.classList.toggle('highlighted', i === highlightedIndex); });
    });

    document.addEventListener('click', function (e) {
        if (!searchList.contains(e.target) && e.target !== searchInput) {
            searchList.hidden = true;
        }
    });

    // Reset everything whenever the modal is reopened.
    modal.addEventListener('show.bs.modal', function () {
        clearSelection();
        modeBtns[0].click();
    });
}

/* ---------------------------------------------------------------------------
   Reserve a Book modal (member Reservations tab): a single title/author
   typeahead over books that already have zero copies available — there's
   no ISBN mode here since the member is browsing, not holding the book.
   ------------------------------------------------------------------------- */
function initReserveBookPicker() {
    var modal = document.getElementById('reserveBookModal');
    if (!modal) return;

    var booksDataEl = document.getElementById('reservable-books-data');
    var books = [];
    try { books = JSON.parse(booksDataEl.textContent || '[]'); } catch (e) { books = []; }

    var hiddenInput = modal.querySelector('#reserve-book');
    var submitBtn = modal.querySelector('#reserve-submit-btn');
    var searchInput = modal.querySelector('#reserve-search');
    var searchList = modal.querySelector('#reserve-search-list');
    var searchResult = modal.querySelector('#reserve-search-result');

    function escapeHtml(s) {
        var div = document.createElement('div');
        div.textContent = s == null ? '' : s;
        return div.innerHTML;
    }

    function setSelectedBook(book) {
        hiddenInput.value = book.id;
        submitBtn.disabled = false;
        searchResult.innerHTML =
            '<div class="issue-book-card">' +
            '<div class="ibc-info">' +
            '<span class="ibc-title">' + escapeHtml(book.title) + '</span>' +
            '<span class="ibc-meta">' + escapeHtml(book.author) + ' &middot; currently unavailable</span>' +
            '</div>' +
            '<button type="button" class="ibc-clear" aria-label="Clear selection">&times;</button>' +
            '</div>';
        searchResult.querySelector('.ibc-clear').addEventListener('click', clearSelection);
    }

    function clearSelection() {
        hiddenInput.value = '';
        submitBtn.disabled = true;
        searchResult.innerHTML = '';
        searchInput.value = '';
        searchList.hidden = true;
    }

    var highlightedIndex = -1;
    var currentMatches = [];

    function renderSearchList(matches) {
        currentMatches = matches;
        highlightedIndex = -1;

        if (matches.length === 0) {
            searchList.innerHTML = '<div class="book-typeahead-empty">No matching books found.</div>';
            searchList.hidden = false;
            return;
        }
        searchList.innerHTML = matches.map(function (b, i) {
            return '<div class="book-typeahead-item" data-index="' + i + '">' +
                '<span class="ta-title">' + escapeHtml(b.title) + '</span>' +
                '<span class="ta-meta">' + escapeHtml(b.author) + '</span>' +
                '</div>';
        }).join('');
        searchList.hidden = false;

        searchList.querySelectorAll('.book-typeahead-item').forEach(function (el) {
            el.addEventListener('click', function () {
                var book = currentMatches[parseInt(el.getAttribute('data-index'), 10)];
                searchInput.value = book.title;
                searchList.hidden = true;
                setSelectedBook(book);
            });
        });
    }

    searchInput.addEventListener('input', function () {
        var q = searchInput.value.trim().toLowerCase();
        searchResult.innerHTML = '';
        hiddenInput.value = '';
        submitBtn.disabled = true;

        if (q.length < 2) { searchList.hidden = true; return; }

        var matches = books.filter(function (b) {
            return b.title.toLowerCase().indexOf(q) !== -1 || b.author.toLowerCase().indexOf(q) !== -1;
        }).slice(0, 8);

        renderSearchList(matches);
    });

    searchInput.addEventListener('keydown', function (e) {
        var items = searchList.querySelectorAll('.book-typeahead-item');
        if (searchList.hidden || items.length === 0) return;

        if (e.key === 'ArrowDown') {
            e.preventDefault();
            highlightedIndex = Math.min(highlightedIndex + 1, items.length - 1);
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            highlightedIndex = Math.max(highlightedIndex - 1, 0);
        } else if (e.key === 'Enter') {
            e.preventDefault();
            if (highlightedIndex >= 0) items[highlightedIndex].click();
            return;
        } else if (e.key === 'Escape') {
            searchList.hidden = true;
            return;
        } else {
            return;
        }
        items.forEach(function (el, i) { el.classList.toggle('highlighted', i === highlightedIndex); });
    });

    document.addEventListener('click', function (e) {
        if (!searchList.contains(e.target) && e.target !== searchInput) {
            searchList.hidden = true;
        }
    });

    modal.addEventListener('show.bs.modal', clearSelection);
}

/* ---------------------------------------------------------------------------
   Boot
   ------------------------------------------------------------------------- */
document.addEventListener('DOMContentLoaded', function () {
    syncThemeButtons();
    initToastsFromServer();
    initModalValidation();
    initDataModals();
    initCatalogFilters();
    initBookCarousels();
    initIssueBookPicker();
    initReserveBookPicker();
});