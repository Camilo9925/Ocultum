export const byId = (id) => document.getElementById(id);

const ICONS = { success: 'bi-check-circle-fill', danger: 'bi-x-octagon-fill', info: 'bi-info-circle-fill', warning: 'bi-exclamation-triangle-fill' };

export function toast(message, kind = 'info') {
  const host = byId('toasts');
  if (!host || !window.bootstrap) return;
  const el = document.createElement('div');
  el.className = 'toast align-items-center';
  el.setAttribute('role', kind === 'danger' ? 'alert' : 'status');
  el.innerHTML = `<div class="d-flex"><div class="toast-body d-flex gap-2"><i class="bi ${ICONS[kind] ?? ICONS.info} text-${kind}"></i><span></span></div><button type="button" class="btn-close me-2 m-auto" data-bs-dismiss="toast" aria-label="Fechar"></button></div>`;
  el.querySelector('span').textContent = message;
  host.append(el);
  el.addEventListener('hidden.bs.toast', () => el.remove());
  window.bootstrap.Toast.getOrCreateInstance(el, { delay: kind === 'danger' ? 7000 : 3500 }).show();
}

export function setBusy(button, busy, busyLabel = 'Processando…') {
  const spinner = button.querySelector('.spinner-border');
  const icon = button.querySelector('[data-idle-icon]');
  const label = button.querySelector('[data-label]');
  if (label && !button.dataset.idleLabel) button.dataset.idleLabel = label.textContent;
  if (spinner) spinner.hidden = !busy;
  if (icon) icon.hidden = busy;
  if (label) label.textContent = busy ? busyLabel : button.dataset.idleLabel;
  button.dataset.busy = busy ? 'true' : 'false';
  button.disabled = busy || button.dataset.blocked === 'true';
}

/** Enables or disables a button without fighting the busy state. */
export function setBlocked(button, blocked) {
  button.dataset.blocked = blocked ? 'true' : 'false';
  button.disabled = blocked || button.dataset.busy === 'true';
}

export async function copyText(text, okMessage = 'Copiado.') {
  try {
    await navigator.clipboard.writeText(text);
  } catch {
    const area = document.createElement('textarea');
    area.value = text;
    area.style.position = 'fixed';
    area.style.opacity = '0';
    document.body.append(area);
    area.select();
    document.execCommand('copy');
    area.remove();
  }
  toast(okMessage, 'success');
}

export function showAlert(container, { title, hint }) {
  container.replaceChildren();
  const strong = document.createElement('strong');
  strong.textContent = title;
  container.append(strong);
  if (hint) {
    const p = document.createElement('div');
    p.className = 'small mt-1';
    p.textContent = hint;
    container.append(p);
  }
  container.hidden = false;
}

export function hideAlert(container) {
  container.hidden = true;
  container.replaceChildren();
}

export function bindPasswordToggle(input, button) {
  button.addEventListener('click', () => {
    const show = input.type === 'password';
    input.type = show ? 'text' : 'password';
    const icon = button.querySelector('i');
    if (icon) icon.className = show ? 'bi bi-eye-slash' : 'bi bi-eye';
    input.focus();
  });
}
