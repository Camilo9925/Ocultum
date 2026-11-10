import { postForm, getBlob, remove, routes, describeError } from './api.js';
import { formatBytes, formatNumber } from './format.js';
import { byId, setBusy, setBlocked, showAlert, hideAlert } from './ui.js';
import { setupDropzone } from './dropzone.js';
import { fileKind } from './preview.js';
import { passphraseProblem } from './password.js';

const root = byId('lab-app');
const limits = { cover: Number(root.dataset.maxCover), pass: Number(root.dataset.maxPass) };

const state = { source: 'generate', cover: null, stego: null, analysis: null, urls: [], charts: [] };
const el = {
  coverName: byId('lab-cover-name'),
  stegoName: byId('lab-stego-name'),
  stegoCol: byId('lab-stego-col'),
  messageCol: byId('lab-message-col'),
  message: byId('lab-message'),
  pass: byId('lab-pass'),
  error: byId('lab-error'),
  analyze: byId('lab-analyze'),
  results: byId('lab-results'),
};

function isImage(file) {
  return fileKind(file.name) === 'image';
}

function pick(file, slot) {
  hideAlert(el.error);
  if (!isImage(file)) {
    showAlert(el.error, { title: 'A análise visual funciona só com imagens JPEG e BMP.' });
    return;
  }
  if (file.size > limits.cover) {
    showAlert(el.error, { title: 'O arquivo é maior que o limite permitido.', hint: `Limite: ${formatBytes(limits.cover)}.` });
    return;
  }
  state[slot] = file;
  (slot === 'cover' ? el.coverName : el.stegoName).textContent = `${file.name} · ${formatBytes(file.size)}`;
  refresh();
}

setupDropzone(byId('lab-cover-drop'), { onFiles: ([f]) => pick(f, 'cover') });
setupDropzone(byId('lab-stego-drop'), { onFiles: ([f]) => pick(f, 'stego') });

for (const radio of document.querySelectorAll('input[name="lab-source"]')) {
  radio.addEventListener('change', () => {
    if (!radio.checked) return;
    state.source = radio.value;
    el.stegoCol.hidden = radio.value !== 'upload';
    el.messageCol.hidden = radio.value !== 'generate';
    refresh();
  });
}
el.message.addEventListener('input', refresh);
el.pass.addEventListener('input', refresh);

function refresh() {
  const passProblem = passphraseProblem(el.pass.value, limits.pass);
  const ready = state.source === 'generate'
    ? Boolean(state.cover) && el.message.value.trim().length > 0
    : Boolean(state.cover) && Boolean(state.stego);
  setBlocked(el.analyze, !ready || Boolean(passProblem));
  if (passProblem) {
    showAlert(el.error, { title: passProblem });
    el.error.dataset.source = 'pass';
  } else if (el.error.dataset.source === 'pass') {
    hideAlert(el.error);
    delete el.error.dataset.source;
  }
}

/* ---------- analysis ---------- */

el.analyze.addEventListener('click', async () => {
  hideAlert(el.error);
  setBusy(el.analyze, true, 'Analisando…');
  try {
    if (state.source === 'generate') state.stego = await generateStego();
    const form = new FormData();
    form.append('cover', state.cover);
    form.append('stego', state.stego);
    const analysis = await postForm(routes.analyses, form);
    state.analysis = analysis;
    render(analysis);
  } catch (err) {
    showAlert(el.error, describeError(err));
  } finally {
    setBusy(el.analyze, false);
  }
});

async function generateStego() {
  const form = new FormData();
  form.append('cover', state.cover);
  form.append('message', el.message.value);
  form.append('passphrase', el.pass.value);
  const created = await postForm(routes.stegoFiles, form);
  try {
    const blob = await getBlob(routes.stegoContent(created.id));
    return new File([blob], created.fileName, { type: blob.type });
  } finally {
    remove(routes.stegoItem(created.id)).catch(() => {});
  }
}

function render(analysis) {
  el.results.hidden = false;
  const { cover, stego } = analysis;
  const deltaEntropy = stego.entropy - cover.entropy;

  byId('m-psnr').textContent = analysis.identical ? '∞ (idênticas)' : `${formatNumber(analysis.psnr, 1)} dB`;
  byId('m-entropy').textContent = `${formatNumber(cover.entropy, 4)} → ${formatNumber(stego.entropy, 4)}`;
  byId('m-changed').textContent = `${formatNumber(analysis.changedPixelsPercent, 2)}%`;
  byId('m-max').textContent = String(analysis.maxPixelDifference);

  const verdict = byId('lab-verdict');
  verdict.className = 'alert d-flex gap-2';
  if (analysis.identical) {
    verdict.classList.add('alert-warning');
    verdict.innerHTML = '<i class="bi bi-exclamation-triangle-fill"></i><div>As duas imagens são idênticas pixel a pixel. Não há segredo nesta comparação (ou o arquivo não foi alterado).</div>';
  } else if (analysis.psnr >= 35 && Math.abs(deltaEntropy) < 0.05) {
    verdict.classList.add('alert-success');
    verdict.innerHTML = `<i class="bi bi-check-circle-fill"></i><div><strong>Praticamente invisível.</strong> A entropia mudou ${formatNumber(Math.abs(deltaEntropy), 4)} bit e o PSNR é ${formatNumber(analysis.psnr, 1)} dB: as estatísticas de primeira ordem foram preservadas, então só olhar o histograma não revela o segredo. É a promessa do Steghide.</div>`;
  } else {
    verdict.classList.add('alert-warning');
    verdict.innerHTML = `<i class="bi bi-exclamation-triangle-fill"></i><div><strong>A alteração é visível nas estatísticas.</strong> PSNR de ${formatNumber(analysis.psnr, 1)} dB e Δ entropia de ${formatNumber(deltaEntropy, 4)} bit. Isso costuma acontecer quando a mensagem ocupa uma fração grande da capacidade.</div>`;
  }

  // images
  for (const url of state.urls) URL.revokeObjectURL(url);
  state.urls = [URL.createObjectURL(state.cover), URL.createObjectURL(state.stego)];
  byId('compare-before').src = state.urls[0];
  byId('compare-after').src = state.urls[1];
  byId('diff-image').src = `data:image/png;base64,${analysis.differenceImage}`;
  byId('diff-amp').textContent = String(analysis.amplification);
  setSplit(byId('compare-range').value);

  drawCharts();
  byId('robust-result').replaceChildren();
  scrollToResults();
}

function scrollToResults() {
  el.results.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

/* ---------- compare slider ---------- */

function setSplit(value) {
  byId('compare').style.setProperty('--split', `${value}%`);
}
byId('compare-range').addEventListener('input', (event) => setSplit(event.target.value));

/* ---------- charts ---------- */

function cssVar(name) {
  return getComputedStyle(document.documentElement).getPropertyValue(name).trim();
}

function drawCharts() {
  if (!state.analysis || !window.Chart) return;
  for (const chart of state.charts) chart.destroy();
  state.charts = [];

  const channel = document.querySelector('input[name="channel"]:checked').value;
  const coverData = state.analysis.cover.histogram[channel];
  const stegoData = state.analysis.stego.histogram[channel];
  const labels = coverData.map((_, i) => i);
  const text = cssVar('--bs-secondary-color');
  const grid = cssVar('--oc-card-border');
  const scales = {
    x: { ticks: { color: text, maxTicksLimit: 9 }, grid: { color: grid } },
    y: { ticks: { color: text }, grid: { color: grid } },
  };

  state.charts.push(new window.Chart(byId('chart-hist'), {
    type: 'line',
    data: {
      labels,
      datasets: [
        { label: 'Original', data: coverData, borderColor: '#7c6cff', borderWidth: 1.5, pointRadius: 0, tension: 0.2 },
        { label: 'Com segredo', data: stegoData, borderColor: '#22d3ee', borderWidth: 1.5, pointRadius: 0, borderDash: [4, 3], tension: 0.2 },
      ],
    },
    options: { maintainAspectRatio: false, animation: false, scales, plugins: { legend: { labels: { color: text } } } },
  }));

  state.charts.push(new window.Chart(byId('chart-delta'), {
    type: 'bar',
    data: { labels, datasets: [{ label: 'Com segredo − original', data: stegoData.map((v, i) => v - coverData[i]), backgroundColor: '#f59e0b' }] },
    options: { maintainAspectRatio: false, animation: false, scales, plugins: { legend: { labels: { color: text } } } },
  }));
}

for (const radio of document.querySelectorAll('input[name="channel"]')) radio.addEventListener('change', drawCharts);
document.addEventListener('oc:themechange', drawCharts);

/* ---------- robustness ---------- */

const quality = byId('robust-quality');
quality.addEventListener('input', () => {
  byId('robust-quality-label').textContent = quality.value;
});

byId('robust-run').addEventListener('click', async (event) => {
  const button = event.currentTarget;
  const out = byId('robust-result');
  out.replaceChildren();
  const form = new FormData();
  form.append('stegoFile', state.stego);
  form.append('passphrase', el.pass.value);
  form.append('quality', quality.value);

  setBusy(button, true, 'Testando…');
  try {
    const r = await postForm(routes.robustnessTests, form);
    const box = document.createElement('div');
    box.className = `alert ${r.survived ? 'alert-success' : 'alert-danger'} d-flex gap-2 mb-0`;
    box.innerHTML = `<i class="bi ${r.survived ? 'bi-check-circle-fill' : 'bi-x-octagon-fill'}"></i><div></div>`;
    box.lastElementChild.innerHTML = r.survived
      ? `<strong>O segredo sobreviveu</strong> à recompressão em qualidade ${r.quality} (${formatBytes(r.originalSizeBytes)} → ${formatBytes(r.recompressedSizeBytes)}). Com qualidades mais baixas, ele costuma se perder.`
      : `<strong>O segredo foi destruído.</strong> Depois de recomprimir em qualidade ${r.quality} (${formatBytes(r.originalSizeBytes)} → ${formatBytes(r.recompressedSizeBytes)}), o Steghide não encontrou mais nada. É exatamente o que acontece quando a imagem passa pelo WhatsApp ou por redes sociais.`;
    out.append(box);
  } catch (err) {
    const alert = document.createElement('div');
    alert.className = 'alert alert-danger mb-0';
    showAlert(alert, describeError(err));
    out.append(alert);
  } finally {
    setBusy(button, false);
  }
});

refresh();
