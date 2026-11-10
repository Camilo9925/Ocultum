/** Makes an element a click/keyboard/drag-and-drop file target. */
export function setupDropzone(root, { multiple = false, onFiles }) {
  const input = root.querySelector('input[type="file"]');

  root.addEventListener('click', (event) => {
    if (event.target === input) return;
    input.click();
  });
  root.addEventListener('keydown', (event) => {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      input.click();
    }
  });
  input.addEventListener('change', () => {
    const files = [...input.files];
    input.value = '';
    if (files.length > 0) onFiles(multiple ? files : [files[0]]);
  });

  for (const type of ['dragenter', 'dragover']) {
    root.addEventListener(type, (event) => {
      event.preventDefault();
      root.classList.add('is-dragover');
    });
  }
  for (const type of ['dragleave', 'drop']) {
    root.addEventListener(type, (event) => {
      event.preventDefault();
      root.classList.remove('is-dragover');
    });
  }
  root.addEventListener('drop', (event) => {
    const files = [...(event.dataTransfer?.files ?? [])];
    if (files.length > 0) onFiles(multiple ? files : [files[0]]);
  });
}
