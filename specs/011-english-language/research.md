# Research: App em inglês

Não havia itens NEEDS CLARIFICATION no contexto técnico; abaixo, as decisões de desenho.

## R1 — Código do idioma inglês

- **Decision**: `en` (arquivo `strings.en.json`), sem variante regional.
- **Rationale**: o app não distingue en-US/en-GB; `lang="en"` é válido e o matcher mapeia qualquer
  `en-*` para ele. O catálogo carrega idiomas pelo nome do recurso, então nada no carregamento muda.
- **Alternatives considered**: `en-US` — sugere uma variante que não existe de fato nos textos.

## R2 — Escolha pelo navegador

- **Decision**: função pura `LanguageMatcher.Pick(IEnumerable<string>)`: percorre a lista na ordem;
  primeira entrada cujo subtag primário (antes de `-`, sem diferenciar maiúsculas) é `pt` → `pt-BR`,
  `en` → `en`; nenhuma → `en`. A lista vem de `navigator.languages` (com `navigator.language` de
  reserva) por `IBrowserApi.Languages()`.
- **Rationale**: FR-003/FR-004/FR-005; testável sem browser; `navigator.languages` é o idioma
  configurado, não geolocalização (nenhuma permissão envolvida).
- **Alternatives considered**: `CultureInfo.CurrentUICulture` no WASM — reflete só o primeiro idioma
  do navegador e depende da carga de dados de cultura; `Accept-Language` — não há servidor.

## R3 — Preferência salva vs. detectada

- **Decision**: `UiPreferences.Language` vira `string?` com padrão `null`. Na inicialização:
  `idioma = prefs.Language (se disponível no catálogo) ?? Pick(navegador)`. Só `SetLanguage` (menu)
  grava o idioma. Ao salvar outras preferências (tema, filtro), `null` continua `null`.
- **Rationale**: FR-008/FR-009 e a assunção "detectado não é gravado"; quem já tem `"language":
  "pt-BR"` salvo segue em português; idioma salvo desconhecido é ignorado (edge case).
- **Alternatives considered**: gravar o detectado na primeira abertura — congelaria o idioma mesmo se o
  navegador mudar antes de o jogador escolher; campo extra `languageChosen` — redundante.

## R4 — `lang` e texto antes do Blazor

- **Decision**: o script inline do `index.html` calcula o mesmo idioma (pref salva ou
  `navigator.languages` com a mesma regra) e define `document.documentElement.lang` e os textos do
  `#blazor-error-ui` (2 strings por idioma, no próprio script). `interop.js` ganha `languages()`.
- **Rationale**: FR-010 e o edge case "primeira pintura"; a tela de carregamento só mostra o nome do
  app (igual nos dois idiomas), então não há troca visível de texto.
- **Alternatives considered**: deixar o `lang` só para o Blazor — leitor de tela e tradutor automático
  do navegador veriam `pt-BR` durante o carregamento; manter o texto de erro só em português — viola
  FR-002.

## R5 — Tradução dos locais de referência

- **Decision**: `reference-data.en.json` embutido, mapa `texto pt → texto en` para cada chave de
  `trainersByLocation` e cada `wildLocation` distinto (~80 entradas), com nomes oficiais do FireRed
  (ex.: "Rota 22" → "Route 22", "Laboratório do Prof. Oak" → "Prof. Oak's Lab", "Ginásio de Pewter
  (Brock)" → "Pewter Gym (Brock)", "Mt. Moon (raro)" → "Mt. Moon (rare)", "Pesca" → "Fishing",
  "Rota 24, 25 (foge); rival em Cerulean" → "Route 24, 25 (flees); rival in Cerulean"). Classe
  `LocationNames` com `Display(ptText, language)` (sem tradução → texto original). Um teste garante
  que todo local de `reference-data.json` tem entrada.
- **Rationale**: Princípio VII ("traduções dos dados de referência" em arquivo próprio); não mexe em
  `reference-data.json` nem na identidade dos locais (a chave continua o texto pt, como hoje).
- **Alternatives considered**: duplicar `reference-data.json` por idioma — duplica EV yields e listas de
  adversários; IDs de local + textos no catálogo de strings — reescreve o formato de referência sem
  ganho.

## R6 — Busca bilíngue

- **Decision**: `OpponentSearch` prepara o local pt e o en de cada adversário e de cada local de
  treinador; casa se qualquer um casar. `RouteExpander` reconhece `routes? N…` além de `rotas? N…` e
  expande mantendo a palavra encontrada ("routes 12–15" → "route 12, route 13, …"). Os locais de
  treinador devolvidos no `SearchHit` continuam o texto pt (identidade); o cartão traduz na exibição.
- **Rationale**: FR-012/SC-005 sem reconstruir o índice ao trocar de idioma; buscar "rota 3" com o app
  em inglês também funciona (inofensivo e útil para quem alterna).
- **Alternatives considered**: índice só do idioma ativo — exigiria reconstruir na troca e não traz
  benefício ao usuário.

## R7 — Textos com formato dependente de idioma

- **Decision**: manter `string.Format` com cultura invariável (como hoje); números do app são inteiros
  sem separador de milhar e não há datas formatadas na interface além do nome do arquivo de backup
  (AAAA-MM-DD, igual nos dois idiomas). Plurais seguem o padrão atual de chaves separadas quando
  existirem.
- **Rationale**: nenhuma regressão e nenhuma dependência de dados de cultura no WASM.
- **Alternatives considered**: formatar com a cultura do idioma — sem caso real hoje.

## R8 — Nome do idioma no menu

- **Decision**: `Language.Name` = "English" em `strings.en.json` (pt-BR já tem "Português (Brasil)");
  o menu já lê o nome de cada idioma no próprio catálogo (FR-006). A troca já re-renderiza sem
  recarregar (`LanguageState.Changed`).
- **Rationale**: reaproveita o seletor existente; só falta haver 2 idiomas.
