const SAFE_ALPHABET = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!#$%&*+-=?@_';

/** Cryptographically random passphrase using only characters Steghide carries safely. */
export function generatePassphrase(length = 20) {
  const limit = Math.floor(0x100000000 / SAFE_ALPHABET.length) * SAFE_ALPHABET.length;
  const out = [];
  const buffer = new Uint32Array(1);
  while (out.length < length) {
    crypto.getRandomValues(buffer);
    if (buffer[0] < limit) out.push(SAFE_ALPHABET[buffer[0] % SAFE_ALPHABET.length]);
  }
  return out.join('');
}

/** Why the passphrase cannot be used, or null. The Cygwin build of Steghide only carries Latin-1. */
export function passphraseProblem(passphrase, maxLength) {
  if (passphrase.length > maxLength) return `A senha pode ter no máximo ${maxLength} caracteres.`;
  for (const ch of passphrase) {
    const code = ch.codePointAt(0);
    if (code < 32 || (code >= 127 && code <= 159) || code > 255) {
      return `O caractere "${ch}" não é aceito. Use letras latinas (acentos são aceitos), números e pontuação comum.`;
    }
  }
  return null;
}

/** Rough strength estimate from the size of the character pool and the length. */
export function assessPassphrase(passphrase) {
  if (!passphrase) return { score: 0, label: '', color: 'secondary' };

  let pool = 0;
  if (/[a-z]/.test(passphrase)) pool += 26;
  if (/[A-Z]/.test(passphrase)) pool += 26;
  if (/\d/.test(passphrase)) pool += 10;
  if (/[^A-Za-z0-9]/.test(passphrase)) pool += 32;

  const unique = new Set(passphrase).size;
  const bits = Math.log2(Math.max(pool, 2)) * Math.min(passphrase.length, unique * 2);

  if (bits < 28) return { score: 1, label: 'Muito fraca', color: 'danger' };
  if (bits < 40) return { score: 2, label: 'Fraca', color: 'warning' };
  if (bits < 60) return { score: 3, label: 'Razoável', color: 'info' };
  if (bits < 80) return { score: 4, label: 'Forte', color: 'success' };
  return { score: 5, label: 'Muito forte', color: 'success' };
}
