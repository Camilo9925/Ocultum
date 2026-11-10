const IMAGE = ['jpg', 'jpeg', 'bmp'];
const AUDIO = ['wav', 'au'];

export function extensionOf(name) {
  const dot = name.lastIndexOf('.');
  return dot < 0 ? '' : name.slice(dot + 1).toLowerCase();
}

/** Returns 'image', 'audio' or null for the formats Steghide accepts. */
export function fileKind(name) {
  const ext = extensionOf(name);
  if (IMAGE.includes(ext)) return 'image';
  if (AUDIO.includes(ext)) return 'audio';
  return null;
}

/** Renders a thumbnail or audio player inside the container; returns a function that releases the object URL. */
export function renderPreview(container, file) {
  container.replaceChildren();
  container.classList.remove('is-audio');
  const kind = fileKind(file.name);
  const url = URL.createObjectURL(file);

  if (kind === 'image') {
    const img = document.createElement('img');
    img.alt = `Prévia de ${file.name}`;
    img.src = url;
    img.addEventListener('error', () => container.replaceChildren(icon('bi-file-earmark-image')));
    container.append(img);
  } else if (kind === 'audio') {
    container.classList.add('is-audio');
    const audio = document.createElement('audio');
    audio.controls = true;
    audio.preload = 'metadata';
    audio.src = url;
    audio.setAttribute('aria-label', `Player de ${file.name}`);
    audio.addEventListener('error', () => {
      container.classList.remove('is-audio');
      container.replaceChildren(icon('bi-file-earmark-music'));
    });
    container.append(audio);
  } else {
    container.append(icon('bi-file-earmark'));
  }

  return () => URL.revokeObjectURL(url);
}

function icon(name) {
  const i = document.createElement('i');
  i.className = `bi ${name}`;
  return i;
}
