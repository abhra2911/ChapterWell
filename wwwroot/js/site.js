// Site scripts

function toggleTheme() {
    const html = document.documentElement;
    const isDark = html.getAttribute('data-theme') === 'dark';
    const next = isDark ? 'light' : 'dark';
    html.setAttribute('data-theme', next);
    localStorage.setItem('theme', next);

    // Let any page-specific code (e.g. dashboard charts) recolour itself.
    if (typeof window.onThemeChange === 'function') {
        window.onThemeChange();
    }
}

// ----- Top books carousel: prev/next scroll buttons -----
function initBookCarousels() {
    var sections = document.querySelectorAll('.carousel-section');
    for (var i = 0; i < sections.length; i++) {
        (function (section) {
            var track = section.querySelector('.book-carousel');
            var prev = section.querySelector('.carousel-btn.prev');
            var next = section.querySelector('.carousel-btn.next');
            if (!track || !prev || !next) return;

            // Scroll by roughly one "page" of cards.
            function step() {
                return Math.max(track.clientWidth * 0.85, 200);
            }

            prev.addEventListener('click', function () {
                track.scrollBy({ left: -step(), behavior: 'smooth' });
            });
            next.addEventListener('click', function () {
                track.scrollBy({ left: step(), behavior: 'smooth' });
            });

            // Fade/disable the buttons when there's nothing more to scroll to.
            // A few px of slack absorbs scroll-snap / sub-pixel rounding.
            function updateButtons() {
                var slack = 8;
                var maxScroll = track.scrollWidth - track.clientWidth;
                prev.disabled = track.scrollLeft <= slack;
                next.disabled = track.scrollLeft >= maxScroll - slack;
            }

            track.addEventListener('scroll', updateButtons);
            window.addEventListener('resize', updateButtons);
            updateButtons();
        })(sections[i]);
    }
}

document.addEventListener('DOMContentLoaded', initBookCarousels);
