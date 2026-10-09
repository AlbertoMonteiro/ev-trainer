---

description: "Task list for Sprites dos Pokémon"
---

# Tasks: Sprites dos Pokémon

**Input**: Design documents from `/specs/002-pokemon-sprites/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/pokemon-sprite.md](./contracts/pokemon-sprite.md),
[quickstart.md](./quickstart.md)

**Tests**: OBRIGATÓRIOS (constituição, Princípio III). Cada teste é escrito e executado com
`dotnet test --solution PokemonEvTrainer.slnx`, **falha**, e só então vem a implementação.

**Organization**: por história de usuário (US1 sprites nos cards, US2 selo de reserva, US3 seletor
de espécie e menu).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)
- **[Story]**: US1, US2, US3

## Convenções (valem para todas as tarefas)

- Mesmas da feature 001: dotnet CLI primeiro, `dotnet format PokemonEvTrainer.slnx` ao fim de cada
  tarefa com código, nenhum texto visível fixo (chaves em
  `src/EvTrainer.Web/Localization/strings.pt-BR.json`), domínio sem Blazor/IO.
- Testes de componentes herdam `tests/EvTrainer.Tests/Support/AppTestContext.cs` (bUnit 2.x,
  `JSInterop` estrito).
- `Sprite`: caminho `sprites/{dex}.png`, `dex` 1–386, PNG 64×64 (data-model.md).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: obter os 386 sprites de forma reproduzível.

- [X] T001 Criar o file-based app `scripts/DownloadSprites.cs` (mesmo estilo de `scripts/BuildPokedex.cs`: `#:property TreatWarningsAsErrors=false`): para cada `dex` de 1 a 386 baixar `https://raw.githubusercontent.com/PokeAPI/sprites/35fdbe9bdec8f519f882c3edc3c0185f08af4d86/sprites/pokemon/versions/generation-iii/firered-leafgreen/{dex}.png` (concorrência 8, `User-Agent` próprio), validar assinatura PNG (`89 50 4E 47 0D 0A 1A 0A`) e dimensões 64×64 lidas do cabeçalho IHDR, gravar em `src/EvTrainer.Web/wwwroot/sprites/{dex}.png`, falhar com código ≠ 0 se algum arquivo faltar ou for inválido, e gerar `src/EvTrainer.Web/wwwroot/sprites/CREDITS.md` com a URL de origem, o commit fixo, o aviso "Pokémon images © The Pokémon Company" e a licença CC0 do repositório PokeAPI/sprites; imprimir `sprites: 386 files`
- [X] T002 Rodar `dotnet run scripts/DownloadSprites.cs` na raiz e conferir 386 PNG + `CREDITS.md` em `src/EvTrainer.Web/wwwroot/sprites/` com tamanho total ≤ 400 KB (SC-004)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: regra do selo e componente `PokemonSprite`, usados pelas três histórias.

**⚠️ CRITICAL**: nenhuma história começa antes desta fase.

### Testes (escrever primeiro, ver falhar)

- [X] T003 [P] Teste `tests/EvTrainer.Tests/Domain/SpriteBadgeTests.cs` para `SpriteBadge.For(name, data, dex)`: Caterpie ⇒ `Dex 10, Stats [0], HasSprite true`; Ivysaur ⇒ `Dex 2, Stats [3, 4]`; Venusaur (2 SpA, 1 SpD) ⇒ `Stats [3, 4]` (ordem por rendimento desc., depois índice; "no máximo 2"); `"Nidoran F"` (alias) ⇒ `Dex 29`; Calcium ⇒ `Dex null, Stats [3], Initial "C", HasSprite false`; Treecko ⇒ `Dex 252, Stats []` ("vazio se não houver dado de EV"); `"Agumon"` ⇒ `Dex null, Stats [], Initial "A", HasSprite false`
- [X] T004 [P] Teste `tests/EvTrainer.Tests/Components/Shared/PokemonSpriteTests.cs` (bUnit) conforme [contracts/pokemon-sprite.md](./contracts/pokemon-sprite.md): `Name="Caterpie", Size=40` renderiza `span.sprite[aria-hidden=true]` com `--sprite-size: 40px` e `--badge: var(--stat-0)`, `.badge` com o texto da chave `Sprite.Dex` para 10, e `img[src='sprites/10.png'][alt=''][width=40][height=40][loading=lazy][decoding=async]` sem classe `loaded`; disparar `onload` no `img` ⇒ classe `loaded`; disparar `onerror` ⇒ `img` removido e `.badge` mantido; Ivysaur ⇒ `--badge` com `linear-gradient(135deg, var(--stat-3), var(--stat-4))`; Treecko ⇒ `var(--accent)`; Calcium ⇒ sem `img`, rótulo "C"

### Implementação

- [X] T005 Implementar `src/EvTrainer.Web/Domain/SpriteBadge.cs` (`sealed record SpriteBadge(int? Dex, IReadOnlyList<int> Stats, string Initial)` com `HasSprite => Dex is >= 1 and <= 386` e `static For(string name, ReferenceData data, Pokedex dex)`) conforme [data-model.md](./data-model.md) — faz T003 passar
- [X] T006 Criar `src/EvTrainer.Web/Components/Shared/PokemonSprite.razor` (parâmetros `Name`, `Size`; injeta `ReferenceData`, `Pokedex`; herda `StoreAwareComponent`; usa `@onload`/`@onerror` nativos, sem JS) e adicionar `@using EvTrainer.Web.Components.Shared` em `src/EvTrainer.Web/_Imports.razor`; adicionar a chave `"Sprite.Dex": "#{0}"` em `src/EvTrainer.Web/Localization/strings.pt-BR.json` — faz T004 passar
- [X] T007 [P] Estilos em `src/EvTrainer.Web/wwwroot/css/app.css`: `.sprite` (caixa `var(--sprite-size)` fixa, `position: relative`, `flex-shrink: 0`, cantos arredondados), `.sprite .badge` (preenche a caixa, `background: var(--badge)`, texto branco em Chakra Petch 600 ~11 px, centralizado), `.sprite img` (`position: absolute; inset: 0; image-rendering: pixelated; opacity: 0; transition: opacity .2s`), `.sprite img.loaded { opacity: 1 }` e `@media (prefers-reduced-motion: reduce) { .sprite img { transition: none } }`

**Checkpoint**: componente pronto e testado isoladamente.

---

## Phase 3: User Story 1 - Reconhecer adversários pela imagem (Priority: P1) 🎯 MVP

**Goal**: sprite à esquerda do nome em todos os cards de adversário.

**Independent Test**: aba EVs com um Pokémon cadastrado ⇒ cada card de Pokémon mostra
`sprites/{dex}.png`, o toque registra a batalha como antes.

### Tests for User Story 1 ⚠️

- [X] T008 [P] [US1] Ampliar `tests/EvTrainer.Tests/Components/Evs/OpponentCardTests.cs`: card de Abra contém `button.opponent .sprite img[src='sprites/63.png']` antes de `.name`; clicar no card (inclusive na área do sprite) continua chamando `OnPick("Abra")`; card de Calcium contém `.sprite .badge` e nenhum `img`
- [X] T009 [P] [US1] Ampliar `tests/EvTrainer.Tests/Components/Evs/OpponentSearchTests.cs`: recentes, lista e resultados de busca ("rota 4") mostram `.sprite` em todos os cards

### Implementation for User Story 1

- [X] T010 [US1] Em `src/EvTrainer.Web/Components/Evs/OpponentCard.razor`, envolver o conteúdo num layout de duas colunas: `<PokemonSprite Name="@Opponent.Name" Size="40" />` à esquerda e o bloco atual (nome, etiquetas, `×N`, treinadores) à direita, mantendo o `<button>` como elemento raiz — faz T008/T009 passarem
- [X] T011 [US1] Ajustar `.opponent` em `src/EvTrainer.Web/wwwroot/css/app.css` para `flex-direction: row; align-items: center; gap: 8px` com o bloco de texto em coluna (`min-width: 0` para manter o corte com "…") e conferir em 360 px que a grade de 2 colunas não gera rolagem horizontal
- [X] T012 [US1] Rodar `dotnet format` e `dotnet test`; validar [quickstart.md §3](./quickstart.md) passo 1

**Checkpoint**: MVP — lista de adversários com sprites.

---

## Phase 4: User Story 2 - Visual agradável sem a imagem (Priority: P2)

**Goal**: selo colorido em qualquer falha, sem ícone quebrado e sem deslocar layout.

**Independent Test**: bloquear `sprites/*` ⇒ todos os lugares mostram selos com cor e número
corretos, cards do mesmo tamanho.

### Tests for User Story 2 ⚠️

- [X] T013 [P] [US2] Teste `tests/EvTrainer.Tests/Components/Evs/SpriteFallbackTests.cs` (bUnit): renderizar `EvTab` com um Pokémon cadastrado, disparar `onerror` em todos os `img` dos cards e verificar que nenhum `img` sobra, que cada `.sprite` tem `.badge` com o rótulo certo (Caterpie "#10", vitaminas pela inicial) e que todos os `.sprite` mantêm `--sprite-size: 40px`
- [X] T014 [P] [US2] Ampliar `tests/EvTrainer.Tests/Localization/NoHardcodedTextTests.cs`: com o idioma sintético `xx`, os rótulos dos selos usam `«Sprite.Dex»` (o teste existente deve continuar passando com sprites em todas as telas; iniciais de vitaminas e nomes desconhecidos entram como dado)

### Implementation for User Story 2

- [X] T015 [US2] Garantir no `PokemonSprite.razor` que, após `onerror`, o componente não volte a renderizar o `img` em re-renderizações (estado por instância com `@key` no card pelo nome) — faz T013 passar
- [X] T016 [US2] Conferir contraste do texto do selo sobre `--stat-2` (amarelo) em `src/EvTrainer.Web/wwwroot/css/app.css`: usar texto escuro quando a cor base for `--stat-2` (classe `badge-dark` aplicada pelo componente quando `Stats[0] == 2`), nos dois temas
- [X] T017 [US2] Rodar `dotnet format` e `dotnet test`; validar [quickstart.md §3](./quickstart.md) passo 4 (DevTools bloqueando `sprites/*`)

**Checkpoint**: falhas de imagem invisíveis para o usuário.

---

## Phase 5: User Story 3 - Sprites ao escolher espécie e no menu (Priority: P3)

**Goal**: sprites no seletor de espécie, na linha evolutiva e no menu, mais o crédito.

**Independent Test**: buscar "squir" no seletor ⇒ opções e linha com sprites; menu ⇒ sprite de
cada Pokémon em treino e linha de créditos.

### Tests for User Story 3 ⚠️

- [X] T018 [P] [US3] Teste em `tests/EvTrainer.Tests/Domain/DisplaySpeciesTests.cs`: `TrainingPokemon.DisplaySpecies` = espécie da última entrada de `IvLog` quando ela está na linha; senão `Species[0].Name`; senão `Name`
- [X] T019 [P] [US3] Ampliar `tests/EvTrainer.Tests/Components/Settings/SpeciesPickerTests.cs`: cada `button.species-option` contém `.sprite` com `--sprite-size: 48px`; cada `.species-chip` da linha Squirtle → Wartortle → Blastoise contém o sprite do respectivo número (7, 8, 9)
- [X] T020 [P] [US3] Ampliar `tests/EvTrainer.Tests/Components/Shell/PokemonMenuTests.cs`: cada `button.pokemon-item` contém `.sprite` da `DisplaySpecies` (Pokémon com anotação de nível em Wartortle ⇒ `sprites/8.png`); o rodapé `.menu-credits` mostra a chave `Menu.Credits` e um link `a[href='https://github.com/PokeAPI/sprites'][target=_blank][rel~=noopener]`

### Implementation for User Story 3

- [X] T021 [US3] Adicionar `DisplaySpecies` em `src/EvTrainer.Web/Domain/Model/TrainingPokemon.cs` — faz T018 passar
- [X] T022 [US3] Incluir `<PokemonSprite Size="48" />` nas opções de busca e nos chips da linha em `src/EvTrainer.Web/Components/Settings/SpeciesPicker.razor` (+ ajuste de layout em `app.css`: opção e chip em linha com o sprite à esquerda) — faz T019 passar
- [X] T023 [US3] Incluir `<PokemonSprite Name="@pokemon.DisplaySpecies" Size="40" />` nos itens e o rodapé de créditos (`<p class="menu-credits">` com `L["Menu.Credits"]` e o link) em `src/EvTrainer.Web/Components/Shell/PokemonMenu.razor`; adicionar `"Menu.Credits": "Sprites: PokeAPI/sprites · imagens © The Pokémon Company"` e `"Menu.Credits.Link": "Ver repositório"` em `strings.pt-BR.json` — faz T020 passar
- [X] T024 [US3] Rodar `dotnet format` e `dotnet test`; validar [quickstart.md §3](./quickstart.md) passos 2 e 3

**Checkpoint**: todas as histórias funcionando.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T025 Validar publish e offline ([quickstart.md §4](./quickstart.md)): `dotnet publish src/EvTrainer.Web -c Release -o artifacts/publish`, conferir que `service-worker-assets.js` lista os 386 `sprites/*.png`, que o aumento do download inicial é ≤ 400 KB (SC-004) e, no Chromium do Playwright, que após a primeira visita os sprites aparecem com a rede desligada e que não há requisições a outros domínios (SC-006)
- [X] T026 Verificar em 360 px, temas claro e escuro: sem rolagem horizontal, sprites nítidos (pixelados) e selos legíveis; nenhum card muda de tamanho ao carregar (SC-003)
- [X] T027 Rodar `dotnet format PokemonEvTrainer.slnx --verify-no-changes` e `dotnet test --solution PokemonEvTrainer.slnx` (tudo verde) como portão final

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências (precisa de internet para baixar os sprites).
- **Foundational (Phase 2)**: depende do Setup apenas para o teste manual; os testes bUnit não
  dependem dos arquivos existirem. Bloqueia todas as histórias.
- **US1 (Phase 3)**: depende da Fundação.
- **US2 (Phase 4)**: depende da Fundação; T013 usa os cards de US1 (T010).
- **US3 (Phase 5)**: depende da Fundação; independente de US1/US2.
- **Polish (Phase 6)**: depois das histórias.

### Within Each User Story

- Testes primeiro e falhando → domínio → componentes → CSS → format/test.
- Tarefas que editam `app.css` ou `strings.pt-BR.json` são sequenciais entre si.

### Parallel Opportunities

- T003 e T004 (testes da fundação); T007 junto de T005.
- US1: T008 e T009. US3: T018, T019 e T020.
- US3 pode andar em paralelo com US1/US2 depois da Fundação.

---

## Parallel Example: User Story 3

```bash
Task: "DisplaySpecies em tests/EvTrainer.Tests/Domain/DisplaySpeciesTests.cs"
Task: "Sprites no seletor em tests/EvTrainer.Tests/Components/Settings/SpeciesPickerTests.cs"
Task: "Sprites e créditos no menu em tests/EvTrainer.Tests/Components/Shell/PokemonMenuTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1 (baixar sprites) → Phase 2 (selo + componente).
2. Phase 3 (US1): cards com sprites.
3. **PARAR E VALIDAR** no app; push na `main` publica.

### Incremental Delivery

1. Setup + Fundação.
2. US1 → cards com sprites.
3. US2 → selo robusto em falhas.
4. US3 → seletor, menu e créditos.
5. Polish → offline, tamanho, 360 px.

---

## Notes

- Commits só quando o usuário pedir; mensagens sem menção a ferramentas de IA.
- Se os detentores dos direitos pedirem a remoção, apagar `wwwroot/sprites/*.png` mantém o app
  funcional com selos (US2).
