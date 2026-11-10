import { setupTheme } from './theme.js';

const toggle = document.getElementById('theme-toggle');
if (toggle) setupTheme(toggle);

if (window.bootstrap) {
  for (const el of document.querySelectorAll('[data-bs-toggle="tooltip"]')) {
    window.bootstrap.Tooltip.getOrCreateInstance(el);
  }
}
