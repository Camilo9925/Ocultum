import { postForm, remove, routes, describeError } from './api.js';
import { formatBytes, formatDateTime } from './format.js';
import { byId, toast, setBusy, setBlocked, copyText, showAlert, hideAlert, bindPasswordToggle } from './ui.js';
import { setupDropzone } from './dropzone.js';
import { renderPreview, fileKind } from './preview.js';
import { passphraseProblem } from './password.js';

const root = byId('extract-app');
const limits = { cover: Number(root.dataset.maxCover), pass: Number(root.dataset.maxPass) };

const state = { file: null, releasePreview: null, result: null };
const el = {
  drop: byId('stego-drop'),
  selected: byId('stego-selected'),
  preview: byId('stego-preview'),
  name: byId('stego-name'),
  meta: byId('stego-meta'),
  pass: byId('pass-input'),
  passError: byId('pass-error'),
  button: byId('extract-btn'),
  empty: byId('extract-empty'),
  error: byId('extract-error'),
  result: byId('extract-result'),
  textWrap: byId('result-text-wrap'),
};

function refresh() {
  const problem = passphraseProblem(el.pass.value, limits.pass);
  el.passError.hidden = !problem;
  el.passError.textContent = problem ?? '';
  setBlocked(el.button, !state.file || Boolean(problem));
}

function clearFile() {
  state.file = null;
  state.releasePreview?.();
  state.releasePreview = null;
  el.selected.hidden = true;
  el.drop.hidden = false;
  refresh();
}

function setFile(file) {
  hideAlert(el.error);
  if (!fileKind(file.name)) {
    el.error.hidden = false;
    showAlert(el.error, { title: 'Esse tipo de arquivo não é aceito.', hint: 'Use JPEG, BMP, WAV ou AU.' });
    return;
  }
  if (file.size > limits.cover) {
    showAlert(el.error, { title: 'O arquivo é maior que o limite permitido.', hint: `Limite: ${formatBytes(limits.cover)}.` });
    return;
  }
  state.releasePreview?.();
  state.file = file;
  state.releasePreview = renderPreview(el.preview, file);
  el.name.textContent = file.name;
  el.meta.textContent = formatBytes(file.size);
  el.drop.hidden = true;
  el.selected.hidden = false;
  refresh();
}

setupDropzone(el.drop, { onFiles: ([file]) => setFile(file) });
byId('stego-clear').addEventListener('click', clearFile);
bindPasswordToggle(el.pass, byId('pass-toggle'));
el.pass.addEventListener('input', refresh);
el.pass.addEventListener('keydown', (event) => {
  if (event.key === 'Enter' && !el.button.disabled) extract();
});

async function extract() {
  hideAlert(el.error);
  const form = new FormData();
  form.append('stegoFile', state.file);
  form.append('passphrase', el.pass.value);

  setBusy(el.button, true, 'Revelando…');
  try {
    const result = await postForm(routes.extractions, form);
    state.result = result;
    renderResult(result);
  } catch (err) {
    el.result.hidden = true;
    el.empty.hidden = true;
    showAlert(el.error, describeError(err));
  } finally {
    setBusy(el.button, false);
  }
}

el.button.addEventListener('click', extract);

function renderResult(result) {
  el.empty.hidden = true;
  el.result.hidden = false;

  const isText = result.kind === 'text';
  el.textWrap.hidden = !isText;
  if (isText) byId('result-text').textContent = result.text;

  const icon = result.kind === 'archive' ? 'bi-file-earmark-zip' : isText ? 'bi-file-earmark-text' : 'bi-file-earmark';
  byId('result-name').innerHTML = `<i class="bi ${icon} me-2"></i>`;
  byId('result-name').append(document.createTextNode(result.fileName));
  byId('result-meta').textContent = `${formatBytes(result.sizeBytes)}${result.kind === 'archive' ? ' · vários arquivos reunidos em um .zip' : ''}`;

  const download = byId('result-download');
  download.href = routes.extractionContent(result.id);
  download.setAttribute('download', result.fileName);
  download.classList.remove('disabled');
  download.querySelector('[data-label]').textContent = isText ? 'Baixar como arquivo' : result.kind === 'archive' ? 'Baixar o .zip' : 'Baixar arquivo';

  byId('result-sha').textContent = result.sha256;
  byId('result-encryption').textContent = result.encryption === 'none' ? 'Sem cifra' : result.encryption;
  byId('result-compressed').textContent = result.isCompressed ? 'Sim' : 'Não';
  byId('result-expires').textContent = formatDateTime(result.expiresAt);
  byId('result-command').textContent = result.command;
  compareExpected();
  el.result.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}

function compareExpected() {
  const expected = byId('expected-sha').value.trim().toLowerCase();
  const out = byId('expected-result');
  if (!expected || !state.result) {
    out.textContent = '';
    out.className = 'small mt-1';
    return;
  }
  const same = expected === state.result.sha256;
  out.className = `small mt-1 ${same ? 'text-success' : 'text-danger'}`;
  out.innerHTML = same
    ? '<i class="bi bi-check-circle-fill me-1"></i>Confere: o conteúdo é idêntico ao original.'
    : '<i class="bi bi-x-circle-fill me-1"></i>Não confere: o conteúdo é diferente do original.';
}

byId('expected-sha').addEventListener('input', compareExpected);
byId('result-copy').addEventListener('click', () => copyText(state.result.text ?? '', 'Mensagem copiada.'));
byId('result-sha-copy').addEventListener('click', () => copyText(byId('result-sha').textContent, 'SHA-256 copiado.'));
byId('result-discard').addEventListener('click', async () => {
  if (!state.result) return;
  try {
    await remove(routes.extractionItem(state.result.id));
    byId('result-download').classList.add('disabled');
    toast('Arquivo descartado do aplicativo.', 'success');
  } catch (err) {
    toast(describeError(err).title, 'danger');
  }
});

refresh();
