# Ocultum — Planejamento

Interface web em C# para o **Steghide 0.5.1**, trabalho de Segurança da Informação (Prof. Tiago Beloti, N2, 5 pts). Entrega: **18/11/2026**.

> Nome de trabalho "Ocultum" (do latim *oculto*). Pode ser trocado sem impacto.

---

## 1. Objetivo e critérios de avaliação

Construir uma aplicação que use o `steghide.exe` como "API" para:

1. **Esconder** informação (arquivo ou mensagem) em um arquivo de cobertura, com senha opcional.
2. **Recuperar** a informação escondida.

Critérios do professor: **técnica, funcionalidades, facilidade de uso, inovação e propósito.**

| Critério | Como o projeto atende |
|---|---|
| Técnica | Arquitetura em camadas, API REST, testes de integração com o binário real, tratamento tipado de erros, segurança no manuseio de arquivos |
| Funcionalidades | Esconder/revelar + medidor de capacidade, opções avançadas, múltiplos arquivos, histórico |
| Facilidade de uso | Drag-and-drop, preview, validação em tempo real, mensagens de erro humanas, tema claro/escuro, execução com duplo clique (pacote já com o Steghide), README de uso e arquivos de exemplo |
| Inovação | Laboratório de esteganálise (histograma, entropia, diferença visual, teste de robustez), carimbo de autoria |
| Propósito | Presets ligados aos slides: privacidade sob censura, marca d'água/autoria, limites de robustez |

---

## 2. O que o Steghide permite (base do design)

- **Formatos de cobertura:** JPEG, BMP, WAV e AU. O payload pode ser qualquer arquivo.
- **Comandos:** `embed`, `extract`, `info` (capacidade e dados embutidos), `encinfo` (cifras e modos).
- **Opções usadas:** `-cf`, `-ef`, `-sf`, `-xf`, `-p`, `-e`, `-z`, `-Z`, `-K`, `-N`, `-f`, `-q`.
- **Criptografia padrão:** Rijndael-128 (AES) em CBC. **Integridade:** CRC32.
- **Exit code:** `0` sucesso, `1` falha.
- **Senha:** o app sempre envia `-p`; string vazia significa "sem senha". Sem senha, qualquer pessoa com o Steghide extrai o conteúdo. A UI avisa isso.
- **Limitação:** senha errada e "não há nada escondido" geram o mesmo erro. A UI deve dizer isso com honestidade.
- **Plataforma:** o `steghide.exe` fornecido é um binário Windows (build Cygwin), com `cygwin1.dll` e outras DLLs na mesma pasta. O app roda no Windows e chama esse executável, exatamente o ambiente do enunciado.
- **Memória:** carrega o arquivo inteiro, então o cover precisa de limite de tamanho.
- **Nome original:** o Steghide grava o nome do arquivo escondido. O app nunca confia nele para escrever em disco.

---

## 3. Decisões de stack

| Item | Escolha | Motivo |
|---|---|---|
| Framework | **ASP.NET Core MVC, .NET 10 (LTS)** | Domínio do autor, upload/download nativos, menos peças móveis. Blazor é o investimento futuro da Microsoft, mas MVC é mais seguro para este escopo e prazo |
| UI | Razor + JS puro em módulos ES + **Bootstrap 5.3** (tema escuro nativo) + Bootstrap Icons + fonte Inter | Limpo, moderno, sem build pipeline |
| Gráficos | Chart.js | Histogramas do laboratório |
| Imagens | ImageSharp | Histograma, diferença, PSNR, recompressão |
| Persistência | EF Core + SQLite (só para histórico, prioridade P2) | Stack conhecida |
| Testes | xUnit + FluentAssertions | |
| Bibliotecas front | **Locais** em `wwwroot/lib` (LibMan) | Funciona sem internet na apresentação |
| Plataforma | **Windows** | O Steghide fornecido é `win32` |
| Distribuição | `dotnet publish` self-contained `win-x64` com a pasta do Steghide incluída | Professor não instala .NET nem configura nada; duplo clique abre o navegador |
| Hospedagem | Kestrel em `127.0.0.1` | Ferramenta local; sem exposição na rede |

---

## 4. Convenções de código

**Idioma:** todo o código em **inglês** (identificadores, comentários, logs, mensagens de commit, nomes de arquivos). Textos exibidos ao usuário em **PT-BR**, ficando nas views.

**Nomenclatura**

| Tipo | Convenção | Exemplo |
|---|---|---|
| Serviço de aplicação | sufixo `Service` + interface `I…Service` | `EmbeddingService`, `IEmbeddingService` |
| Repositório (só se houver persistência) | sufixo `Repository` | `OperationRepository`, `IOperationRepository` |
| Adaptador do Steghide | sufixo `Client` | `SteghideClient`, `ISteghideClient` |
| Controller | sufixo `Controller` | `StegoFilesController` |
| DTO de API | `…Request` / `…Response` (records) | `CreateStegoFileRequest`, `StegoFileResponse` |
| Modelo de view MVC | sufixo `ViewModel` | `EmbedPageViewModel` |
| Configuração | sufixo `Options` | `SteghideOptions` |
| Validação | sufixo `Validator` | `CoverFileValidator` |
| Exceção | sufixo `Exception` | `CapacityExceededException` |
| Background | sufixo `BackgroundService` | `WorkspaceCleanupBackgroundService` |

**Regras gerais**
- Métodos assíncronos terminam em `Async` e recebem `CancellationToken` como último parâmetro.
- Campos privados `_camelCase`; namespaces file-scoped; um tipo por arquivo; `Nullable` habilitado; DTOs como `record`.
- Enums no singular. Injeção de dependência por construtor.
- **Comentários só quando o porquê não é óbvio.** Sem comentário que repita o código.
- Sem lógica de negócio em controller; controller só valida entrada, chama o serviço e devolve o resultado.
- **Commits:** Conventional Commits (`feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`).

---

## 5. Arquitetura

Três projetos (propositalmente enxuto para o tamanho do sistema):

```
Ocultum/
├── src/
│   ├── Ocultum.Core/             # domínio + contratos + serviços de aplicação (sem I/O direto)
│   │   ├── Models/               # StegoFormat, EmbedOptions, InspectionResult...
│   │   ├── Services/             # EmbeddingService, ExtractionService, CoverInspectionService, SteganalysisService
│   │   ├── Abstractions/         # ISteghideClient, ITemporaryWorkspace, IFileSignatureReader
│   │   ├── Errors/               # exceções tipadas
│   │   └── Validation/
│   ├── Ocultum.Infrastructure/   # implementações externas
│   │   ├── Steghide/             # SteghideClient, SteghideOutputParser, SteghideErrorMapper
│   │   ├── Storage/              # TemporaryWorkspace, WorkspaceCleanupBackgroundService
│   │   ├── Imaging/              # ImageAnalysisService (ImageSharp)
│   │   └── Persistence/          # (P2) OcultumDbContext, OperationRepository
│   └── Ocultum.Web/
│       ├── Controllers/Api/      # controllers REST
│       ├── Controllers/Pages/    # controllers que só devolvem views
│       ├── Contracts/            # Requests/Responses
│       ├── ViewModels/ Views/ wwwroot/
│       └── Program.cs
├── tests/Ocultum.Tests/
├── tools/steghide/               # cópia da pasta do Steghide fornecida + copying.txt
├── samples/                      # cover.jpg, cover.bmp, cover.wav, secret.txt para testar rápido
└── README.md                     # guia de uso (ver seção 10)
```

O caminho do executável vem de `SteghideOptions.ExecutablePath`. O `appsettings.json` aponta para `tools/steghide/steghide.exe` (pacote de entrega); no desenvolvimento, o `appsettings.Development.json` ou o user-secrets pode apontar para a pasta onde o Steghide já está instalado na máquina.

**Componentes-chave**

| Componente | Responsabilidade |
|---|---|
| `SteghideClient` | Executa o processo com `ProcessStartInfo.ArgumentList`, `WorkingDirectory` na pasta temporária, timeout, kill da árvore de processos e semáforo de concorrência. Fixa `LANG=C` para mensagens estáveis |
| `SteghideErrorMapper` | Converte stdout/stderr + exit code em exceções tipadas: `WrongPassphraseOrNoDataException`, `CapacityExceededException`, `UnsupportedCoverFormatException`, etc. |
| `TemporaryWorkspace` | Pasta isolada por operação, com arquivos nomeados por GUID. O Steghide recebe só nomes simples (evita problemas de acento/espaço/path do Cygwin). Limpeza por TTL |
| `EmbeddingService` | Orquestra: valida cover, consulta capacidade, monta payload (arquivo, texto ou zip de vários), chama o client, calcula SHA-256 |
| `ExtractionService` | Roda `info` para descobrir o conteúdo, depois `extract -xf` com **nome de saída controlado** (nunca o nome embutido) |
| `CoverInspectionService` | Capacidade e formato do cover |
| `SteganalysisService` | Histograma, entropia, diferença, PSNR e teste de robustez |
| `OperationRepository` | **Só existe se o histórico (P2) entrar**; persistência em SQLite, só metadados |

---

## 6. API REST

**Princípios**
- Prefixo e versão: `/api/v1`.
- Recursos no **plural, substantivos, kebab-case**; verbos HTTP definem a ação. JSON em `camelCase`.
- **Sem HATEOAS:** respostas não trazem `_links`, `href` nem URLs. O cliente monta as rotas pela convenção. Também não uso o header `Location`.
- Erros em **`application/problem+json`** (ProblemDetails) com a extensão `errorCode` estável (ex.: `CAPACITY_EXCEEDED`), sem URIs de documentação.
- Uploads em `multipart/form-data`; downloads binários em `…/content`.

**Rotas**

| Método | Rota | Descrição | Sucesso |
|---|---|---|---|
| `POST` | `/api/v1/stego-files` | Embute dados em um cover e cria o stego file | `201` + metadados |
| `GET` | `/api/v1/stego-files/{id}/content` | Baixa o stego file | `200` binário |
| `DELETE` | `/api/v1/stego-files/{id}` | Descarta antecipadamente o arquivo temporário | `204` |
| `POST` | `/api/v1/extractions` | Extrai dados de um stego file | `201` + metadados (e texto, se for mensagem) |
| `GET` | `/api/v1/extractions/{id}/content` | Baixa o arquivo extraído | `200` binário |
| `DELETE` | `/api/v1/extractions/{id}` | Descarta antecipadamente | `204` |
| `POST` | `/api/v1/cover-inspections` | Formato e capacidade de um cover (usa `steghide info`) | `200` |
| `GET` | `/api/v1/ciphers` | Cifras e modos disponíveis (`encinfo`, com cache) | `200` |
| `POST` | `/api/v1/analyses` | Histograma, entropia, diferença e PSNR (cover × stego) | `200` |
| `POST` | `/api/v1/robustness-tests` | Recomprime o stego e tenta extrair | `200` |
| `GET` | `/api/v1/operations?type=&page=&pageSize=` | *(P2)* Histórico paginado | `200` |
| `GET` | `/api/v1/operations/{id}` | *(P2)* Detalhe | `200` |
| `DELETE` | `/api/v1/operations/{id}` | *(P2)* Remove do histórico | `204` |

**Códigos de erro**

| Status | Quando |
|---|---|
| `400` | Campos ausentes ou inválidos (validação) |
| `404` | `id` inexistente ou expirado |
| `413` | Arquivo acima do limite |
| `415` | Formato de cover não suportado (assinatura inválida) |
| `422` | Falha semântica: capacidade excedida, senha incorreta ou sem dados, payload vazio |
| `500` | Falha inesperada do processo Steghide |

**Exemplo — `POST /api/v1/stego-files`** (campos: `cover`, `payloadFile` ou `message`, `passphrase`, `encryption`, `compressionLevel`, `embedChecksum`, `embedFileName`)

```json
{
  "id": "0f6c1d0e8a6f4b7c9a2e5b1d3c7a9e42",
  "fileName": "photo.jpg",
  "sizeBytes": 248113,
  "sha256": "9b74c9897bac770ffc029102a200c5de…",
  "isPasswordProtected": true,
  "expiresAt": "2026-11-09T18:45:00Z"
}
```

**Exemplo — `POST /api/v1/extractions`** (campos: `stegoFile`, `passphrase`)

```json
{
  "id": "5a2f…",
  "kind": "text",
  "text": "Meet at 8pm.",
  "fileName": "message.txt",
  "sizeBytes": 12,
  "sha256": "3c2e…",
  "isCompressed": true,
  "encryption": "rijndael-128 cbc"
}
```

**Exemplo — erro `422`**

```json
{
  "status": 422,
  "title": "Not enough capacity in the cover file.",
  "detail": "Payload is 5.2 KB but the cover holds 3.5 KB.",
  "errorCode": "CAPACITY_EXCEEDED"
}
```

**Rotas de páginas (MVC, separadas da API)**

| Rota | Página |
|---|---|
| `/` | Esconder |
| `/extract` | Revelar |
| `/lab` | Laboratório de esteganálise |
| `/history` | Histórico *(P2)* |
| `/about` | Sobre (esteganografia × criptografia) |

---

## 7. Funcionalidades

### Obrigatórias
- **Esconder:** cover JPEG/BMP/WAV/AU; conteúdo como arquivo **ou** mensagem de texto; senha opcional com confirmação e botão mostrar/ocultar; download do resultado.
- **Revelar:** envia stego file e senha; mensagem de texto aparece na tela com botão de copiar; arquivo vem para download.

### Extras

| Prio | Funcionalidade | Detalhe |
|---|---|---|
| **P1** | Medidor de capacidade em tempo real | Ao selecionar o cover, chama `cover-inspections` e mostra barra "usado × disponível"; bloqueia o envio se exceder |
| **P1** | Drag-and-drop e preview | Imagem em preview, player para WAV/AU, loading e toasts |
| **P1** | Presets de uso | **Mensagem secreta**, **Enviar arquivo**, **Carimbo de autoria** (texto com autor, data e SHA-256 do original, como marca d'água) |
| **P1** | Opções avançadas recolhidas | Cifra/modo (vindos de `/ciphers`), compressão 1–9, checksum, nome embutido; tooltips explicativos |
| **P1** | Senha | Indicador de força e gerador de passphrase |
| **P1** | "Ver comando Steghide" | Mostra o comando executado com a senha mascarada, evidenciando o Steghide como API |
| **P2** | Vários arquivos como payload | Zip automático; na extração, oferece o zip |
| **P2** | SHA-256 | Do original e do recuperado, lado a lado |
| **P2** | Laboratório de esteganálise | Ver abaixo |
| **P2** | Histórico | Metadados apenas (sem senha, sem conteúdo); aqui nasce o `OperationRepository` |
| **P3** | Aba Sobre | Esteganografia × criptografia, resumo dos slides |
| **P3** | OpenAPI | Documento gerado a partir dos controllers |

### Laboratório de esteganálise
Conecta com a seção "Métodos de Detecção" dos slides:
- Histograma de cores e entropia de Shannon, cover × stego.
- Slider antes × depois, mapa de diferença amplificado e PSNR.
- **Tese a demonstrar:** o Steghide preserva as estatísticas de primeira ordem (README), então o histograma quase não muda.
- **Teste de robustez:** recomprime o JPEG stego (ex.: qualidade 85) e tenta extrair; a falha ilustra os slides 31–32 (WhatsApp, compressão).
- **Stretch:** "LSB ingênuo" didático em que o teste qui-quadrado detecta a alteração e o Steghide não.
- Fora de escopo: ataque de dicionário contra senhas.

---

## 8. UI / UX

- Barra superior com abas, alternância de tema e tipografia limpa.
- **Esconder:** três cards (1 Cobertura · 2 Conteúdo · 3 Proteção) + painel lateral fixo com resumo e medidor de capacidade. Botão principal sempre visível.
- **Resultado:** download, SHA-256 e aviso de robustez ("não reenvie por apps que recomprimem imagem").
- Responsivo, foco visível, contraste adequado; mensagens de erro em PT-BR, sem jargão.

---

## 9. Segurança

- Kestrel escuta apenas em `127.0.0.1`; antiforgery nas páginas; limites de upload.
- Validação do cover pela **assinatura de bytes** (FFD8, "BM", RIFF/WAVE, ".snd"), não só pela extensão.
- Nomes de arquivo sanitizados; extração sempre com `-xf` controlado (evita path traversal pelo nome embutido).
- Senha nunca é logada nem persistida; histórico não guarda conteúdo.
- Downloads por `id` aleatório (GUID), com TTL e limpeza automática.
- Limitação conhecida e documentada: `-p` expõe a senha na linha de comando durante a execução do processo.

---

## 10. README de uso

Arquivo `README.md` na raiz, em PT-BR, voltado ao professor/avaliador. Estrutura:

1. **O que é** — uma frase, mais print da tela principal.
2. **Início rápido** — pré-requisito: Windows; extrair o pacote publicado, duplo clique em `Ocultum.exe` (o navegador abre sozinho); como encerrar.
3. **Execução a partir do código** — `dotnet run` (requer .NET 10 SDK) e como configurar o caminho do Steghide.
4. **Primeiro uso em 2 minutos** — passo a passo com os arquivos de `samples/`:
   - Esconder uma mensagem em `cover.jpg` com e sem senha, e baixar o resultado.
   - Revelar a mensagem com a senha correta e com a errada (mostra o erro).
5. **Guia das telas** — Esconder, Revelar, Laboratório, Histórico, Sobre; o que cada campo faz; formatos aceitos (JPEG, BMP, WAV, AU); como ler o medidor de capacidade.
6. **Boas práticas e limitações** — sem senha qualquer um extrai; compressão (WhatsApp etc.) destrói o conteúdo; senha errada e "sem dados" geram o mesmo erro; limite de tamanho.
7. **API REST** — tabela de rotas e exemplos `curl` para `stego-files`, `extractions` e `cover-inspections`.
8. **Configuração** — tabela de opções do `appsettings.json` (nome, padrão, descrição): caminho do Steghide, TTL, limites de tamanho, porta.
9. **Solução de problemas** — porta ocupada, Steghide não encontrado, bloqueio do antivírus, arquivo rejeitado (formato/capacidade), caracteres especiais na senha.
10. **Testes** — como rodar `dotnet test` (e quais exigem o Steghide).
11. **Estrutura do projeto** — árvore resumida e responsabilidade das camadas.
12. **Créditos e licenças** — Steghide (GPL, link do código-fonte), bibliotecas usadas, autor.

---

## 11. Riscos e spikes (validar com o `steghide.exe` real antes de codar)

| Risco | Mitigação |
|---|---|
| Porta em uso na máquina do professor | Porta configurável; documentado no README |
| `info` pede "y/n" no terminal e pode travar | Testar stdin redirecionado com `n` ou stdin fechado |
| Senha com acento/aspas (Cygwin antigo usa codepage ANSI) | Testar; se falhar, avisar ou restringir na UI |
| Mensagens variam com o locale | `LANG=C` no processo + parser por regex |
| Senha visível na linha de comando | App local, processo curto, nunca logada |
| Arquivo inteiro em memória | Limite de tamanho do cover (ex.: 20 MB) |
| Antivírus com DLLs Cygwin | Testar em Windows limpo |
| Licença GPL | Incluir `copying.txt` e `credits.txt` na distribuição |

---

## 12. Testes

- **Unitários:** `FileSignatureReader`, `SteghideOutputParser`, `SteghideErrorMapper` (com saídas reais capturadas), regras de capacidade.
- **Integração (com o `steghide.exe` real):** ida e volta em JPEG/BMP/WAV/AU, com e sem senha, senha errada, capacidade excedida, texto com Unicode, vários arquivos (zip).
- **API:** status codes, formato ProblemDetails e ausência de campos de link.
- **Manual:** roteiro de demonstração de 3 minutos, seguindo o próprio README, em um Windows limpo.

---

## 13. Cronograma (09/11 → 18/11)

| Dia | Foco |
|---|---|
| 09–10 | Spikes do Steghide, scaffold da solution, `SteghideClient` com testes |
| 11–12 | Esconder e revelar de ponta a ponta (API + UI base) |
| 13–14 | Medidor de capacidade, presets, opções avançadas, múltiplos arquivos |
| 15–16 | Laboratório, histórico, aba Sobre |
| 17 | Polimento, `samples/`, README completo, publish self-contained e teste em Windows limpo |
| 18 | Folga e entrega |

Se o tempo apertar, corta-se de baixo para cima (P3 → P2); os P1 não se mexem.

---

## 14. Entregáveis e definição de pronto

- Repositório com histórico de Conventional Commits.
- Pacote publicado (`win-x64`, self-contained) com a pasta do Steghide incluída.
- Pasta `samples/` com arquivos de exemplo.
- `README.md` de uso (seção 10) com prints e roteiro de demonstração.
- Todos os testes verdes e fluxo completo validado em um Windows limpo.

---

## 15. Decisões em aberto

Todas confirmadas:

1. **MVC + .NET 10:** confirmado (SDK 10.0 instalado).
2. **Laboratório de esteganálise:** entra no escopo (histograma, entropia, PSNR, diferença e teste de robustez). O "LSB ingênuo" com qui-quadrado (stretch) fica para depois.
3. **Nome:** "Ocultum" mantido.
4. **Histórico (P2):** **adiado**. Por enquanto o projeto não usa banco de dados: sem EF Core/SQLite, sem `OperationRepository`, sem a tela `/history` e sem as rotas `/api/v1/operations`. Fica para uma próxima etapa.

**Ajuste de stack:** a análise de imagem usa **SkiaSharp** (MIT) em vez do ImageSharp. O ImageSharp 4 exige licença comercial em builds Release e o 3.x tem vulnerabilidades conhecidas.

---

## Fontes

- Manual, README, `todo.txt` e `history.txt` do Steghide 0.5.1 (anexo)
- Slides "A Esteganografia: Conceitos e Evolução" (anexo)
- [.NET 10 é LTS](https://www.c-sharpcorner.com/news/net-10-is-here-faster-smarter-and-lts)
- [Blazor como principal investimento da Microsoft em UI web](https://devclass.com/2025/05/29/microsoft-designates-blazor-as-its-main-future-investment-in-web-ui-for-net/)
- [StegExpose — detecção de esteganografia LSB](https://ar5iv.arxiv.org/html/1410.6656)
