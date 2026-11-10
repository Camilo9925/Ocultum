import { postForm, getJson, remove, routes, describeError } from './api.js';
import { formatBytes, formatDateTime, sha256Hex, utf8Length } from './format.js';
import { byId, toast, setBusy, setBlocked, copyText, showAlert, hideAlert, bindPasswordToggle } from './ui.js';
import { setupDropzone } from './dropzone.js';
import { renderPreview, fileKind } from './preview.js';
import { assessPassphrase, generatePassphrase, passphraseProblem } from './password.js';

const root = byId('embed-app');
const limits = {
  cover: Number(root.dataset.maxCover),
  payload: Number(root.dataset.maxPayload),
  files: Number(root.dataset.maxFiles),
  pass: Number(root.dataset.maxPass),
};
byId('max-cover-label').textContent = formatBytes(limits.cover);

const state = {
  cover: null,
  coverInfo: null,
  coverSha: null,
  mode: 'message',
  files: [],
  inspectToken: 0,
  releasePreview: null,
  lastResultId: null,
};

const el = {
  coverDrop: byId('cover-drop'),
  coverSelected: byId('cover-selected'),
  coverPreview: byId('cover-preview'),
  coverName: byId('cover-name'),
  coverMeta: byId('cover-meta'),
  coverInspecting: byId('cover-inspecting'),
  coverError: byId('cover-error'),
  message: byId('message-input'),
  messageCount: byId('message-count'),
  filesList: byId('files-list'),
  author: byId('stamp-author'),
  stampPreview: byId('stamp-preview'),
  pass: byId('pass-input'),
  confirmWrap: byId('pass-confirm-wrap'),
  confirm: byId('pass-confirm'),
  passError: byId('pass-error'),
  passWarning: byId('pass-warning'),
  strengthBar: byId('pass-strength-bar'),
  strengthLabel: byId('pass-strength-label'),
  cipher: byId('cipher'),
  mode: byId('mode'),
  compression: byId('compression'),
  checksum: byId('opt-checksum'),
  fileName: byId('opt-filename'),
  sumCover: byId('sum-cover'),
  sumPayload: byId('sum-payload'),
  sumProtection: byId('sum-protection'),
  capBar: byId('cap-bar'),
  capText: byId('cap-text'),
  capNote: byId('cap-note'),
  button: byId('embed-btn'),
  hint: byId('embed-hint'),
  error: byId('embed-error'),
};

/* ---------- cover ---------- */

function showCoverError(message) {
  showAlert(el.coverError, { title: message });
}

function clearCover() {
  state.inspectToken++;
  state.cover = null;
  state.coverInfo = null;
  state.coverSha = null;
  state.releasePreview?.();
  state.releasePreview = null;
  el.coverSelected.hidden = true;
  el.coverDrop.hidden = false;
  update();
}

async function setCover(file) {
  hideAlert(el.coverError);
  if (!fileKind(file.name)) {
    showCoverError('Formato não suportado. Use JPEG, BMP, WAV ou AU.');
    return;
  }
  if (file.size > limits.cover) {
    showCoverError(`O arquivo tem ${formatBytes(file.size)} e o limite é ${formatBytes(limits.cover)}.`);
    return;
  }

  state.releasePreview?.();
  state.cover = file;
  state.coverInfo = null;
  state.coverSha = null;
  state.releasePreview = renderPreview(el.coverPreview, file);
  el.coverName.textContent = file.name;
  el.coverMeta.textContent = formatBytes(file.size);
  el.coverDrop.hidden = true;
  el.coverSelected.hidden = false;
  el.coverInspecting.hidden = false;
  update();

  const token = ++state.inspectToken;
  sha256Hex(file)
    .then((hash) => {
      if (state.cover === file) {
        state.coverSha = hash;
        update();
      }
    })
    .catch(() => {});

  try {
    const form = new FormData();
    form.append('cover', file);
    const info = await postForm(routes.coverInspections, form);
    if (token !== state.inspectToken) return;
    state.coverInfo = info;
    el.coverMeta.textContent = `${info.formatDescription} · ${formatBytes(file.size)} · comporta cerca de ${formatBytes(info.capacityBytes)}`;
    el.coverInspecting.hidden = true;
    update();
  } catch (err) {
    if (token !== state.inspectToken) return;
    const { title, hint } = describeError(err);
    clearCover();
    showAlert(el.coverError, { title, hint });
  }
}

setupDropzone(el.coverDrop, { onFiles: ([file]) => setCover(file) });
byId('cover-clear').addEventListener('click', clearCover);

/* ---------- content ---------- */

function setMode(mode) {
  state.mode = mode;
  byId('pane-message').hidden = mode !== 'message';
  byId('pane-files').hidden = mode !== 'files';
  byId('pane-stamp').hidden = mode !== 'stamp';
  update();
}

for (const radio of document.querySelectorAll('input[name="mode"]')) {
  radio.addEventListener('change', () => radio.checked && setMode(radio.value));
}

function addFiles(files) {
  for (const file of files) {
    if (state.files.length >= limits.files) {
      toast(`No máximo ${limits.files} arquivos por vez.`, 'warning');
      break;
    }
    state.files.push(file);
  }
  renderFiles();
}

function renderFiles() {
  el.filesList.replaceChildren();
  state.files.forEach((file, index) => {
    const li = document.createElement('li');
    li.className = 'list-group-item d-flex align-items-center gap-2 bg-transparent px-0';
    li.innerHTML = '<i class="bi bi-file-earmark text-body-secondary"></i><span class="flex-grow-1 text-truncate"></span><span class="small text-body-secondary"></span><button type="button" class="btn btn-sm oc-btn-ghost" aria-label="Remover arquivo"><i class="bi bi-x-lg"></i></button>';
    li.children[1].textContent = file.name;
    li.children[2].textContent = formatBytes(file.size);
    li.querySelector('button').addEventListener('click', () => {
      state.files.splice(index, 1);
      renderFiles();
    });
    el.filesList.append(li);
  });
  update();
}

setupDropzone(byId('files-drop'), { multiple: true, onFiles: addFiles });
el.message.addEventListener('input', update);
el.author.addEventListener('input', update);

function stampText(now) {
  const author = el.author.value.trim();
  return [
    'OCULTUM — CARIMBO DE AUTORIA',
    `Autor: ${author || '(informe o autor)'}`,
    `Data: ${now ? now.toISOString() : '(no momento de esconder)'}`,
    `Arquivo original: ${state.cover?.name ?? '(escolha a cobertura)'}`,
    `SHA-256 do original: ${state.coverSha ?? (state.cover ? '(calculando…)' : '(escolha a cobertura)')}`,
  ].join('\n');
}

function currentPayload() {
  if (state.mode === 'files') {
    const bytes = state.files.reduce((sum, f) => sum + f.size, 0);
    return { bytes, ready: state.files.length > 0 && bytes > 0, label: state.files.length === 1 ? state.files[0].name : `${state.files.length} arquivos (.zip)` };
  }
  if (state.mode === 'stamp') {
    const text = stampText(new Date());
    return { bytes: utf8Length(text), ready: el.author.value.trim().length > 0 && state.coverSha !== null, label: 'Carimbo de autoria' };
  }
  const text = el.message.value;
  return { bytes: utf8Length(text), ready: text.trim().length > 0, label: 'Mensagem de texto' };
}

/* ---------- protection ---------- */

bindPasswordToggle(el.pass, byId('pass-toggle'));
el.pass.addEventListener('input', update);
el.confirm.addEventListener('input', update);
byId('pass-generate').addEventListener('click', () => {
  const generated = generatePassphrase(20);
  el.pass.value = generated;
  el.confirm.value = generated;
  el.pass.type = 'text';
  byId('pass-toggle').querySelector('i').className = 'bi bi-eye-slash';
  update();
  copyText(generated, 'Senha gerada e copiada. Guarde-a: sem ela não há como revelar.');
});

const barColor = { danger: 'var(--oc-bad)', warning: 'var(--oc-warn)', info: '#38bdf8', success: 'var(--oc-ok)', secondary: 'transparent' };

async function loadCiphers() {
  try {
    const ciphers = await getJson(routes.ciphers);
    const none = new Option('Nenhuma (só esconder, sem cifrar)', 'none');
    el.cipher.append(none);
    for (const cipher of ciphers) el.cipher.append(new Option(cipher.name, cipher.name));
    el.cipher.dataset.ciphers = JSON.stringify(ciphers);
  } catch {
    // Advanced ciphers are optional; the defaults still work.
  }
}

el.cipher.addEventListener('change', () => {
  const ciphers = JSON.parse(el.cipher.dataset.ciphers ?? '[]');
  const chosen = ciphers.find((c) => c.name === el.cipher.value);
  el.mode.replaceChildren(new Option('Padrão (cbc)', ''));
  const blockCipher = chosen && !(chosen.modes.length === 1 && chosen.modes[0] === 'stream');
  if (blockCipher) for (const m of chosen.modes) el.mode.append(new Option(m, m));
  el.mode.disabled = !blockCipher;
});

/* ---------- summary ---------- */

function update() {
  const payload = currentPayload();
  const pass = el.pass.value;
  const problems = [];

  el.messageCount.textContent = `${formatBytes(utf8Length(el.message.value))}`;
  el.stampPreview.textContent = stampText(null);

  // passphrase
  const strength = assessPassphrase(pass);
  el.strengthBar.style.width = `${(strength.score / 5) * 100}%`;
  el.strengthBar.style.backgroundColor = barColor[strength.color];
  el.strengthLabel.textContent = pass ? `Força: ${strength.label}` : '';
  el.passWarning.hidden = pass.length > 0;
  el.confirmWrap.hidden = pass.length === 0;

  const charProblem = passphraseProblem(pass, limits.pass);
  const mismatch = pass.length > 0 && el.confirm.value !== pass && el.confirm.value.length > 0;
  const needsConfirm = pass.length > 0 && el.confirm.value !== pass;
  el.passError.hidden = !(charProblem || mismatch);
  el.passError.textContent = charProblem ?? (mismatch ? 'As senhas não conferem.' : '');

  // summary text
  setSummary(el.sumCover, state.cover ? `${state.cover.name} · ${formatBytes(state.cover.size)}` : 'Nenhuma');
  setSummary(el.sumPayload, payload.bytes > 0 ? `${payload.label} · ${formatBytes(payload.bytes)}` : 'Vazio');
  setSummary(el.sumProtection, pass ? `Senha (${strength.label.toLowerCase()})` : 'Sem senha');

  // capacity meter
  const capacity = state.coverInfo?.capacityBytes;
  let exceeds = false;
  if (capacity) {
    const pct = (payload.bytes / capacity) * 100;
    exceeds = payload.bytes > capacity;
    el.capBar.style.width = `${Math.min(100, pct)}%`;
    el.capBar.className = `progress-bar ${exceeds ? 'is-bad' : pct >= 70 ? 'is-warn' : ''}`;
    el.capBar.parentElement.setAttribute('aria-valuenow', String(Math.round(Math.min(100, pct))));
    el.capText.textContent = `${formatBytes(payload.bytes)} de ${formatBytes(capacity)} (${pct < 1 && payload.bytes > 0 ? '<1' : Math.round(pct)}%)`;
    el.capNote.className = `small mt-2 ${exceeds ? 'text-danger' : pct >= 70 ? 'text-warning' : 'text-body-secondary'}`;
    el.capNote.textContent = exceeds
      ? 'Não cabe. Escolha uma cobertura maior ou reduza o conteúdo.'
      : pct >= 70
        ? 'Quase cheio: quanto mais cheio, mais fácil de detectar.'
        : 'Cabe com folga.';
  } else {
    el.capBar.style.width = '0%';
    el.capText.textContent = state.cover ? 'medindo…' : '—';
    el.capNote.textContent = '';
  }

  // what is missing
  if (!state.cover) problems.push('Escolha a cobertura.');
  else if (!state.coverInfo) problems.push('Aguarde a medição da capacidade.');
  if (!payload.ready) {
    problems.push(
      state.mode === 'files' ? 'Escolha ao menos um arquivo.'
        : state.mode === 'stamp' ? (el.author.value.trim() ? 'Aguarde o cálculo do SHA-256 da cobertura.' : 'Informe o autor do carimbo.')
          : 'Escreva a mensagem.',
    );
  }
  if (exceeds) problems.push('O conteúdo não cabe na cobertura.');
  if (charProblem) problems.push('Ajuste a senha.');
  else if (needsConfirm) problems.push('Confirme a senha.');

  setBlocked(el.button, problems.length > 0);
  el.hint.textContent = problems.length > 0 ? problems[0] : 'Tudo pronto.';
}

function setSummary(node, text) {
  node.textContent = text;
  const isEmpty = ['Nenhuma', 'Vazio', 'Sem senha'].includes(text);
  node.classList.toggle('is-set', !isEmpty);
  node.classList.toggle('text-body-secondary', isEmpty);
}

/* ---------- submit ---------- */

async function submit() {
  hideAlert(el.error);
  const form = new FormData();
  form.append('cover', state.cover);

  if (state.mode === 'files') for (const file of state.files) form.append('payloadFile', file);
  else form.append('message', state.mode === 'stamp' ? stampText(new Date()) : el.message.value);

  form.append('passphrase', el.pass.value);
  if (el.cipher.value) form.append('encryption', el.mode.value && !el.mode.disabled ? `${el.cipher.value} ${el.mode.value}` : el.cipher.value);
  if (el.compression.value !== '') form.append('compressionLevel', el.compression.value);
  form.append('embedChecksum', String(el.checksum.checked));
  form.append('embedFileName', String(el.fileName.checked));

  setBusy(el.button, true, 'Escondendo…');
  try {
    const result = await postForm(routes.stegoFiles, form);
    showResult(result);
  } catch (err) {
    const description = describeError(err);
    showAlert(el.error, description);
    toast(description.title, 'danger');
  } finally {
    setBusy(el.button, false);
  }
}

el.button.addEventListener('click', submit);

/* ---------- result ---------- */

function showResult(result) {
  state.lastResultId = result.id;
  byId('result-name').textContent = result.fileName;
  byId('result-meta').textContent = `${formatBytes(result.sizeBytes)} · ${result.isPasswordProtected ? 'protegido por senha' : 'sem senha'}`;
  const download = byId('result-download');
  download.href = routes.stegoContent(result.id);
  download.setAttribute('download', result.fileName);
  download.classList.remove('disabled');
  byId('result-sha').textContent = result.sha256;
  byId('result-payload-sha').textContent = result.payloadSha256;
  byId('result-expires').textContent = formatDateTime(result.expiresAt);
  byId('result-command').textContent = result.command;
  window.bootstrap.Modal.getOrCreateInstance(byId('result-modal')).show();
}

byId('result-sha-copy').addEventListener('click', () => copyText(byId('result-sha').textContent, 'SHA-256 copiado.'));
byId('result-discard').addEventListener('click', async () => {
  if (!state.lastResultId) return;
  try {
    await remove(routes.stegoItem(state.lastResultId));
    byId('result-download').classList.add('disabled');
    toast('Arquivo descartado do aplicativo.', 'success');
  } catch (err) {
    toast(describeError(err).title, 'danger');
  }
});

byId('result-again').addEventListener('click', () => {
  clearCover();
  el.message.value = '';
  state.files = [];
  renderFiles();
  el.pass.value = '';
  el.confirm.value = '';
  update();
  window.scrollTo({ top: 0, behavior: 'smooth' });
});

loadCiphers();
update();
