export const routes = {
  stegoFiles: '/api/v1/stego-files',
  extractions: '/api/v1/extractions',
  coverInspections: '/api/v1/cover-inspections',
  ciphers: '/api/v1/ciphers',
  analyses: '/api/v1/analyses',
  robustnessTests: '/api/v1/robustness-tests',
  stegoContent: (id) => `/api/v1/stego-files/${id}/content`,
  stegoItem: (id) => `/api/v1/stego-files/${id}`,
  extractionContent: (id) => `/api/v1/extractions/${id}/content`,
  extractionItem: (id) => `/api/v1/extractions/${id}`,
};

export class ApiError extends Error {
  constructor(problem, status) {
    super(problem?.title ?? 'Erro');
    this.status = status;
    this.errorCode = problem?.errorCode ?? 'UNKNOWN';
    this.detail = problem?.detail ?? null;
    this.problem = problem ?? {};
  }
}

async function ensureOk(response) {
  if (response.ok) return response;
  let problem = null;
  try {
    problem = await response.json();
  } catch {
    // The body was not JSON; the status code is all we have.
  }
  throw new ApiError(problem ?? { status: response.status }, response.status);
}

async function send(url, options) {
  let response;
  try {
    response = await fetch(url, options);
  } catch (err) {
    if (err?.name === 'AbortError') throw err;
    const offline = new ApiError({ errorCode: 'NETWORK', title: 'Sem resposta do aplicativo' }, 0);
    throw offline;
  }
  return ensureOk(response);
}

export async function postForm(url, form, { signal } = {}) {
  const response = await send(url, { method: 'POST', body: form, signal });
  return response.status === 204 ? null : response.json();
}

export async function getJson(url) {
  const response = await send(url, { headers: { Accept: 'application/json' } });
  return response.json();
}

export async function getBlob(url) {
  const response = await send(url);
  return response.blob();
}

export async function remove(url) {
  await send(url, { method: 'DELETE' });
}

const kb = (n) => (n == null ? '' : `${(n / 1024).toLocaleString('pt-BR', { maximumFractionDigits: 1 })} KB`);

/** Human, Portuguese descriptions for the stable error codes returned by the API. */
export function describeError(error) {
  if (!(error instanceof ApiError)) {
    return { title: 'Algo deu errado.', hint: error?.message ?? '' };
  }
  const p = error.problem;
  switch (error.errorCode) {
    case 'CAPACITY_EXCEEDED':
      return {
        title: 'O conteúdo não cabe nesta cobertura.',
        hint: p.payloadBytes != null && p.capacityBytes != null
          ? `O conteúdo tem ${kb(p.payloadBytes)} e a cobertura comporta ${kb(p.capacityBytes)}. Escolha uma cobertura maior ou reduza o conteúdo.`
          : 'Escolha uma cobertura maior ou reduza o conteúdo.',
      };
    case 'WRONG_PASSPHRASE_OR_NO_DATA':
      return {
        title: 'Não foi possível revelar nada com essa senha.',
        hint: 'O Steghide dá a mesma resposta para senha incorreta e para um arquivo que não esconde nada, então não dá para saber qual dos dois é. Confira a senha e lembre que o arquivo não pode ter sido recomprimido.',
      };
    case 'UNSUPPORTED_COVER_FORMAT':
      return {
        title: 'Esse tipo de arquivo não é aceito.',
        hint: p.detail && !p.detail.startsWith('Supported formats')
          ? traduzirDetalhe(p.detail)
          : 'Use JPEG, BMP, WAV ou AU. O formato é verificado pelo conteúdo do arquivo, não pelo nome.',
      };
    case 'FILE_TOO_LARGE':
      return { title: 'O arquivo é maior que o limite permitido.', hint: p.detail ?? '' };
    case 'EMPTY_PAYLOAD':
      return { title: 'Não há nada para esconder.', hint: 'Escreva uma mensagem ou escolha um arquivo que não esteja vazio.' };
    case 'INVALID_INPUT':
      return { title: 'Alguma informação está inválida.', hint: traduzirDetalhe(p.detail ?? p.title ?? '') };
    case 'VALIDATION_FAILED':
      return { title: 'Faltam informações obrigatórias.', hint: 'Confira se todos os campos foram preenchidos.' };
    case 'NOT_FOUND':
      return { title: 'Esse arquivo não está mais disponível.', hint: 'Os arquivos temporários expiram depois de alguns minutos. Gere novamente.' };
    case 'IMAGE_PROCESSING_FAILED':
      return { title: 'Não foi possível processar a imagem.', hint: traduzirDetalhe(p.detail ?? '') };
    case 'COVER_NOT_PROCESSABLE':
      return {
        title: 'O Steghide não conseguiu processar este arquivo.',
        hint: 'É uma limitação do próprio Steghide: ele trava com capas muito grandes combinadas a conteúdos grandes, com BMP de paleta (8 bits) e com áudio incompleto. Tente um JPEG, uma imagem menor ou menos conteúdo.',
      };
    case 'STEGHIDE_UNAVAILABLE':
      return { title: 'O Steghide não foi encontrado.', hint: 'Confira a opção Steghide:ExecutablePath no appsettings.json (veja o README).' };
    case 'STEGHIDE_FAILURE':
      return { title: 'O Steghide falhou.', hint: p.detail ?? 'Tente novamente com outro arquivo.' };
    case 'NETWORK':
      return { title: 'O Ocultum não respondeu.', hint: 'A janela do aplicativo ainda está aberta? Reinicie o Ocultum e tente de novo.' };
    case 'CROSS_ORIGIN_BLOCKED':
    case 'INVALID_HOST':
      return { title: 'Requisição bloqueada por segurança.', hint: 'Acesse o aplicativo por http://127.0.0.1 ou http://localhost.' };
    default:
      return { title: 'Algo deu errado.', hint: p.detail ?? '' };
  }
}

function traduzirDetalhe(detail) {
  const map = [
    [/Visual analysis supports JPEG and BMP images only/i, 'A análise visual funciona só com imagens JPEG e BMP.'],
    [/robustness test supports JPEG/i, 'O teste de robustez funciona só com arquivos JPEG.'],
    [/passphrase contains unsupported characters/i, 'A senha tem caracteres que o Steghide não consegue carregar. Use letras latinas (acentos são aceitos), números e pontuação comum.'],
    [/Use only Latin letters/i, 'Use letras latinas (acentos são aceitos), números e pontuação comum.'],
    [/passphrase is too long/i, 'A senha é longa demais.'],
    [/different dimensions/i, 'As duas imagens precisam ter exatamente o mesmo tamanho (largura e altura).'],
    [/either a message or files/i, 'Informe uma mensagem ou arquivos, não os dois.'],
    [/Too many files/i, 'Há arquivos demais.'],
    [/Unknown cipher/i, 'Cifra desconhecida.'],
  ];
  const text = detail ?? '';
  for (const [pattern, replacement] of map) if (pattern.test(text)) return replacement;
  return text;
}
