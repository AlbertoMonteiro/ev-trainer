---

description: "Task list for Layout para desktop"
---

# Tasks: Layout para desktop

**Input**: Design documents from `/specs/007-desktop-layout/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/layout.md](./contracts/layout.md),
[quickstart.md](./quickstart.md)

**Tests**: OBRIGATÓRIOS (constituição, Princípio III) para estrutura (regiões, ordem do DOM, painel
lateral) — escrever, rodar com `dotnet test --solution PokemonEvTrainer.slnx`, **ver falhar**, então
implementar. Aspectos puramente visuais (CSS) são verificados com as medições do quickstart (research
R8), anotadas na tarefa de validação de cada história.

**Organization**: US1 aba EVs no desktop (P1), US2 IVs/Ajustes/Avaliar captura lado a lado (P2), US3
painel lateral de Pokémon (P3).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)
- **[Story]**: US1, US2, US3

## Convenções

- dotnet CLI primeiro; `dotnet format PokemonEvTrainer.slnx` após cada tarefa com código.
- **Nenhum componente decide o que renderizar pela largura** (research R2): só classes de região no
  markup + `@media (min-width: 1024px)` / `@media (min-width: 1280px)` em
  `src/EvTrainer.Web/wwwroot/css/app.css`. Abaixo de 1024 px nada pode mudar visualmente (SC-004).
- A ordem dos filhos no DOM continua a do celular; o desktop reposiciona com `grid-template-areas`
  (research R3).
- Textos novos só em `src/EvTrainer.Web/Localization/strings.pt-BR.json` (Princípio VII).
- Edições em `app.css`, `Home.razor` e `strings.pt-BR.json` são sequenciais entre si.
- Ao mexer em `CaptureView.razor`, validar com `dotnet build PokemonEvTrainer.slnx --no-incremental`
  (sem AD0001 — analisador do RC1).

---

## Phase 1: Setup

- [X] T001 Confirmar base verde (`dotnet format PokemonEvTrainer.slnx --verify-no-changes`, `dotnet test --solution PokemonEvTrainer.slnx`) e registrar as medições de antes em 1366 × 768 (largura usada por `main.page`, cards por linha na lista principal) com os snippets de [quickstart.md](./quickstart.md), para comparar com SC-001/SC-002
  - **Antes (1366 × 768)**: `main.page` = 41% da largura; 2 cards por linha; 6 cards visíveis sem rolar; sem rolagem horizontal; 435 testes verdes.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: estrutura da página (`.app-body`) e o contêiner de desktop (até 1440 px), toast, hover e foco — base de todas as histórias.

- [X] T002 Teste `tests/EvTrainer.Tests/Components/LayoutRegionsTests.cs` (bUnit, renderizando `Home` com um Pokémon): `main.page` fica dentro de `.app-body`, e o cabeçalho `header.app-header` fica fora dela; com a tela vazia (sem Pokémon) a mesma estrutura vale
- [X] T003 Em `src/EvTrainer.Web/Pages/Home.razor` envolver `main.page` em `<div class="app-body">`; em `src/EvTrainer.Web/wwwroot/css/app.css`, bloco `@media (min-width: 1024px)`: `#app` com `max-width: 1440px` (centralizado), `main.page` com margem lateral de 24 px; toast no canto inferior direito com `width: 420px` (FR-007); estados de hover sob `@media (hover: hover)` para `.opponent`, botões e itens de lista, e `:focus-visible` reforçado nos cards (FR-008) — faz T002 passar
- [X] T004 Rodar `dotnet format` e `dotnet test`; conferir em 360 px que nada mudou

**Checkpoint**: base pronta.

---

## Phase 3: User Story 1 - Treinar EVs no desktop aproveitando a largura (Priority: P1) 🎯 MVP

**Goal**: na aba EVs, a partir de 1024 px, resumo fixo à esquerda e grade de adversários com ≥ 4 colunas em 1366 px.

**Independent Test**: 1366 × 768 ⇒ resumo visível ao rolar a lista; ≥ 4 cards na primeira linha; registrar = 1 clique; aviso no canto inferior direito.

### Tests for User Story 1 ⚠️

- [X] T005 [US1] Ampliar `tests/EvTrainer.Tests/Components/LayoutRegionsTests.cs`: na aba EVs existe `.ev-layout` cujos filhos diretos, em ordem, são `.ev-summary-area` (contendo `.summary`), a barra de busca (`.search-row`), `.recent-list` (quando há recentes), `.main-list` e o histórico (`.history-card`) — a ordem do celular é preservada

### Implementation for User Story 1

- [X] T006 [US1] Em `src/EvTrainer.Web/Components/Evs/EvTab.razor`: envolver o conteúdo em `<div class="ev-layout">` e o `<EvSummary />` em `<div class="ev-summary-area">`, sem mudar a ordem — faz T005 passar
- [X] T007 [US1] Em `src/EvTrainer.Web/wwwroot/css/app.css`, dentro de `@media (min-width: 1024px)`: `.ev-layout` em grid `340px minmax(0, 1fr)` com `grid-template-areas` (resumo na coluna 1, todo o resto na coluna 2); `.ev-summary-area` com `position: sticky; top: <altura do cabeçalho + 12px>; align-self: start`; `.opponent-grid` com `grid-template-columns: repeat(auto-fill, minmax(160px, 1fr))` (research R4)
- [X] T008 [US1] Rodar `dotnet format` e `dotnet test`; validar [quickstart.md](./quickstart.md) passo 1 (sem o painel) e 8 com as medições (≥ 4 cards por linha, sem rolagem horizontal) e anotar os números nesta tarefa
  - **Depois (1366 × 768, sem painel ainda)**: `main.page` = 99% da largura; 5 cards por linha; 34 cards visíveis sem rolar (antes 6); resumo fixo a 72 px do topo ao rolar; aviso no canto inferior direito (420 px); sem rolagem horizontal. **360 px**: 2 cards por linha, resumo estático, mesma ordem, sem rolagem horizontal.

**Checkpoint**: MVP — a ação principal aproveita a tela do desktop.

---

## Phase 4: User Story 2 - IVs, ajustes e avaliação de captura lado a lado (Priority: P2)

**Goal**: aba IVs, Ajustes e Avaliar captura em duas colunas a partir de 1024 px.

**Independent Test**: 1366 × 768, Avaliar captura com Squirtle Nv 5 Timid `20/9/12/11/12/11` ⇒ campos e resultado visíveis sem rolar a página; aba IVs ⇒ tabela e formulário lado a lado.

### Tests for User Story 2 ⚠️

- [X] T009 [P] [US2] Ampliar `tests/EvTrainer.Tests/Components/LayoutRegionsTests.cs`: aba IVs com `.iv-layout` cujos filhos, em ordem, contêm a tabela (`.iv-table`), `.known-ivs` e `.level-form`; aba Ajustes com `.settings-layout` contendo, em ordem, o formulário do Pokémon, o painel de backup, a importação de planilha e o apagar
- [X] T010 [P] [US2] Ampliar `tests/EvTrainer.Tests/Components/Capture/CaptureViewTests.cs`: `.capture-view .capture-columns` com `.capture-inputs` (espécie, nível, nature, stats e os botões `add-capture`/`new-capture`) seguido de `.capture-output` (com `.capture-table`, `.capture-verdict` e `.best-capture` quando houver resultado/espécie); os demais testes de captura continuam verdes

### Implementation for User Story 2

- [X] T011 [US2] Em `src/EvTrainer.Web/Components/Ivs/IvTab.razor`: envolver os três componentes em `<div class="iv-layout">` (mesma ordem) e, em `src/EvTrainer.Web/Pages/Home.razor`, os quatro componentes de Ajustes em `<div class="settings-layout">` — faz T009 passar
- [X] T012 [US2] Em `src/EvTrainer.Web/Components/Capture/CaptureView.razor`: agrupar os campos e as ações em `.capture-inputs` e o resultado (`CaptureResultPanel`) e o `BestCaptureCard` em `.capture-output`, dentro de `.capture-columns` (cabeçalho e dica fora, ocupando a largura); compilar com `dotnet build PokemonEvTrainer.slnx --no-incremental` e, se aparecer AD0001, mover o bloco problemático para um componente filho (memória do projeto) — faz T010 passar
- [X] T013 [US2] Em `src/EvTrainer.Web/wwwroot/css/app.css`, dentro de `@media (min-width: 1024px)`: `.iv-layout` em grid com tabela e IVs conhecidos na coluna 1 e `.level-form` na coluna 2 (`align-self: start`); `.settings-layout` com o formulário na coluna 1 e os demais painéis na coluna 2; `.capture-columns` em duas colunas (`minmax(0, 1fr) minmax(0, 1.2fr)`) com `.capture-output` `align-self: start`
- [X] T014 [US2] Rodar `dotnet format`, `dotnet build --no-incremental` e `dotnet test`; validar [quickstart.md](./quickstart.md) passos 3, 4 e 5 (avaliação sem rolar a página em 1366 × 768) e anotar
  - **1366 × 768**: IVs — tabela (757 px) e IVs conhecidos à esquerda, formulário (541 px) à direita; Ajustes — formulário à esquerda, backup/importar/apagar à direita; captura — campos (568 px) | resultado e melhor (681 px), fundo do resultado em 733 px < 768 (antes de compactar: 831 px; a tabela da captura usa botões de 34 px só com mouse, `pointer: fine`); sem rolagem horizontal.

**Checkpoint**: telas de formulário sem rolagem desnecessária.

---

## Phase 5: User Story 3 - Lista de Pokémon sempre à mão no desktop (Priority: P3)

**Goal**: a partir de 1280 px, painel lateral com todos os Pokémon; trocar = 1 clique.

**Independent Test**: 3 Pokémon, 1366 × 768 ⇒ clicar no 3º no painel o seleciona mantendo a aba.

### Tests for User Story 3 ⚠️

- [X] T015 [P] [US3] Teste `tests/EvTrainer.Tests/Components/Shell/PokemonSidebarTests.cs` (bUnit, renderizando `Home`): `aside.pokemon-sidebar` com `aria-label` `Sidebar.Title` e título visível; um `button.pokemon-item[data-id]` por Pokémon na ordem do menu, com sprite, forma atual e `Menu.Total`; o selecionado com `aria-current="true"`; clicar em outro o seleciona e mantém a aba atual (ex.: IVs continua IVs); com Avaliar captura aberta, clicar num Pokémon fecha a avaliação e mostra as abas; `button.add` cria um Pokémon e abre Ajustes; sem Pokémon, o painel tem só o botão de adicionar
- [X] T016 [P] [US3] Conferir `tests/EvTrainer.Tests/Components/Shell/PokemonMenuTests.cs`: continua verde sem mudanças depois da extração da lista (mesma marcação `button.pokemon-item`)

### Implementation for User Story 3

- [X] T017 [US3] Extrair a lista de `src/EvTrainer.Web/Components/Shell/PokemonMenu.razor` para `src/EvTrainer.Web/Components/Shell/PokemonList.razor` (parâmetro `EventCallback<string> OnChoose`), e usá-la no menu — mantém T016 verde
- [X] T018 [US3] Criar `src/EvTrainer.Web/Components/Shell/PokemonSidebar.razor` (`<aside class="pokemon-sidebar" aria-label=…>` com `h2` `Sidebar.Title`, `PokemonList` e `button.add` `Menu.AddPokemon`; parâmetros `OnChoose`, `OnAdd`) e incluí-lo em `src/EvTrainer.Web/Pages/Home.razor` antes de `main.page`, dentro de `.app-body`; escolher um Pokémon chama `Store.Select`, fecha a captura e mantém `_tab`; adicionar segue o fluxo do menu (Ajustes); chave `Sidebar.Title` ("Seus Pokémon") em `strings.pt-BR.json` — faz T015 passar
- [X] T019 [US3] Em `src/EvTrainer.Web/wwwroot/css/app.css`: `.pokemon-sidebar { display: none; }` por padrão; em `@media (min-width: 1280px)`: `.app-body` em grid `240px minmax(0, 1fr)`, painel `position: sticky` abaixo do cabeçalho com rolagem própria, e a lista dentro de `.menu-panel` oculta
- [X] T020 [US3] Rodar `dotnet format` e `dotnet test`; validar [quickstart.md](./quickstart.md) passos 2 e 6 (1100 px sem painel) e anotar
  - **1366 × 768 com painel**: painel 240 px; `main.page` 1087 px; painel + conteúdo = 98% da largura; 4 cards por linha; 28 cards visíveis sem rolar (antes 6); clicar no 3º Pokémon o seleciona mantendo a aba IVs; lista do menu ☰ oculta; com a captura aberta, escolher no painel fecha a captura. **1100 px**: sem painel, 4 cards por linha, sem rolagem horizontal.

**Checkpoint**: todas as histórias funcionando.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T021 Ampliar `tests/EvTrainer.Tests/Localization/NoHardcodedTextTests.cs`: o painel lateral (com e sem Pokémon) não tem texto fixo (SC-005 da feature 001 / Princípio VII)
- [X] T022 Validar [quickstart.md](./quickstart.md) completo (passos 1–11) em 360, 768, 1100, 1366 e 2560 px, temas claro e escuro, teclado, redimensionamento com texto digitado; anotar SC-001 (≥ 70% de largura usada), SC-002 (≥ 2× cards visíveis vs. T001), SC-003, SC-004 e SC-005
  - SC-001 98% (≥ 70%) · SC-002 28 cards visíveis × 6 antes (4,7×) · SC-003 resultado da captura até 733 px de 768 · SC-004 360 px: 2 cards/linha, mesma ordem, resumo estático · SC-005 sem rolagem horizontal em 360, 768, 1100, 1366 e 2560 px · 2560 px: conteúdo limitado a 1700 px e centralizado (6 cards/linha) · redimensionar 1366↔360 manteve a busca "rota 22" e os campos da captura (Abra, Nv 9, HP 27) · Tab mostra o foco no card · tema escuro ok.
  - A captura de tela revelou a natureza sobrepondo o total no painel de 240 px; corrigido (o painel mostra o total, o menu mostra a natureza; nome com reticências) e medido sem sobreposição.
- [X] T023 [P] Atualizar `README.md` (Features): layout de desktop com painel lateral; incluir captura de tela de desktop em `docs/screenshots/` se o README usar capturas
- [X] T024 Portão final: `dotnet format PokemonEvTrainer.slnx --verify-no-changes`, `dotnet build PokemonEvTrainer.slnx --no-incremental` (sem AD0001) e `dotnet test --solution PokemonEvTrainer.slnx`

---

## Dependencies & Execution Order

- **Setup (T001)** → **Fundação (T002–T004)** → US1 → US2 → US3 → Polish.
- US1, US2 e US3 são independentes em comportamento, mas todas editam `app.css` (e US2/US3 editam
  `Home.razor`): executar em sequência de prioridade.
- Dentro de cada história: testes falhando → markup (regiões) → CSS → validação com medições.

### Parallel Opportunities

- US2: T009 e T010 (arquivos de teste diferentes).
- US3: T015 e T016.
- Polish: T023 em paralelo com T021.

## Parallel Example: User Story 2

```text
Task: "T009 Ampliar tests/EvTrainer.Tests/Components/LayoutRegionsTests.cs (IVs e Ajustes)"
Task: "T010 Ampliar tests/EvTrainer.Tests/Components/Capture/CaptureViewTests.cs (colunas da captura)"
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Fase 1 e Fase 2.
2. US1 (T005–T008) → validar → pode publicar: registrar batalhas no desktop já aproveita a tela.

### Incremental Delivery

1. + US2 → formulários lado a lado.
2. + US3 → painel lateral.
3. Polish → texto fixo, validação completa, README.
