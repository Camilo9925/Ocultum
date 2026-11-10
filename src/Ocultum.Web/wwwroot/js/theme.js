const KEY = 'oc-theme';

function current() {
  return document.documentElement.getAttribute('data-bs-theme') === 'light' ? 'light' : 'dark';
}

function paint() {
  const light = current() === 'light';
  for (const icon of document.querySelectorAll('[data-theme-icon]')) {
    icon.className = `bi ${light ? 'bi-moon-stars-fill' : 'bi-sun-fill'}`;
  }
}

export function setupTheme(button) {
  paint();
  button.addEventListener('click', () => {
    const next = current() === 'light' ? 'dark' : 'light';
    document.documentElement.setAttribute('data-bs-theme', next);
    try {
      localStorage.setItem(KEY, next);
    } catch {
      // Storage can be blocked; the choice just won't persist.
    }
    paint();
    document.dispatchEvent(new CustomEvent('oc:themechange', { detail: next }));
  });
}
