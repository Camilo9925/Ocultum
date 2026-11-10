(function () {
  try {
    var stored = localStorage.getItem('oc-theme');
    var theme = stored || (window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark');
    document.documentElement.setAttribute('data-bs-theme', theme);
  } catch (e) {
    document.documentElement.setAttribute('data-bs-theme', 'dark');
  }
})();
