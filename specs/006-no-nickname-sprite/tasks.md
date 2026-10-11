---

description: "Task list for Pokémon identificado pela espécie, sem apelido"
---

# Tasks: Pokémon identificado pela espécie, sem apelido

**Input**: Design documents from `/specs/006-no-nickname-sprite/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/species-identity-ui.md](./contracts/species-identity-ui.md),
[quickstart.md](./quickstart.md)

**Tests**: OBRIGATÓRIOS (constituição, Princípio III). Escrever, rodar com
`dotnet test --solution PokemonEvTrainer.slnx`, ver falhar, então implementar.

**Organization**: US1 evoluir sem renomear (P1), US2 imagem no resumo de EVs (P1), US3 dados antigos (P2).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)
- **[Story]**: US1, US2, US3

## Convenções

- Mesmas das features anteriores: dotnet CLI primeiro, `dotnet format PokemonEvTrainer.slnx` após
  cada tarefa com código, domínio sem Blazor/IO, **todo texto novo em**
  `src/EvTrainer.Web/Localization/strings.pt-BR.json` (Princípio VII).
- Testes de componentes herdam `tests/EvTrainer.Tests/Support/AppTestContext.cs`.
- Edições em `TrainingPokemon.cs`, `TrainerStore.cs`, `strings.pt-BR.json` e `app.css` são sequenciais
  entre si.
- Invariante da forma atual (data-model.md): **`CurrentSpecies == ""` ou
  `Species.Any(s => s.Name == CurrentSpecies)`**. Testes que hoje põem um nome fora da linha (ex.:
  `second.Name = "Pidgey"` com a linha do Squirtle) DEVEM passar a usar uma linha própria (Pidgey/
  Pidgeotto/Pidgeot) em vez de só trocar o nome.

---

## Phase 1: Setup

- [X] T001 Confirmar a linha de base: `dotnet format PokemonEvTrainer.slnx --verify-no-changes` e `dotnet test --solution PokemonEvTrainer.slnx` verdes antes de começar (anotar o total de testes)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: trocar `Name` por `CurrentSpecies` no modelo e o texto exibido por um rótulo comum, sem mudar
nenhum comportamento visível ainda. Todas as histórias dependem desta fase.

### Testes (escrever primeiro, ver falhar)

- [X] T002 Criar `tests/EvTrainer.Tests/Domain/CurrentFormTests.cs` (substitui `tests/EvTrainer.Tests/Domain/DisplaySpeciesTests.cs`, que é apagado) cobrindo `TrainingPokemon.AdvanceTo` e `InsertLevel` sobre a linha Squirtle/Wartortle/Blastoise: com `CurrentSpecies = "Squirtle"`, `AdvanceTo("Wartortle")` ⇒ `"Wartortle"`; com `"Wartortle"`, `AdvanceTo("Squirtle")` ⇒ continua `"Wartortle"` ("nunca desevolui"); `AdvanceTo("Charmander")` (fora da linha) ⇒ não muda; com `CurrentSpecies = ""`, `AdvanceTo("Squirtle")` ⇒ `"Squirtle"`; `InsertLevel` de uma nota nível 16 `Wartortle` mantém o `IvLog` ordenado por nível e avança a forma atual; `InsertLevel` de uma nota nível 5 `Squirtle` com forma atual `Blastoise` ⇒ continua `Blastoise`; `Clone()` copia `CurrentSpecies`
- [X] T003 [P] Atualizar `tests/EvTrainer.Tests/Domain/ModelJsonTests.cs`: a escrita grava `"name"` com o valor de `CurrentSpecies` (lista de propriedades do documento inalterada: `events, ivLog, knownIvs, macho, name, nature, order, pokerus, species, targets`); leitura de `"name": "Wartortle"` com a linha do Squirtle ⇒ `CurrentSpecies == "Wartortle"`; ida e volta preserva `CurrentSpecies`

### Implementação

- [X] T004 Em `src/EvTrainer.Web/Domain/Model/TrainingPokemon.cs`: renomear `Name` para `CurrentSpecies` (doc: "Espécie em que o Pokémon está agora: `\"\"` ou um nome de `Species`"); remover `DisplaySpecies`; adicionar `AdvanceTo(string species)` com a regra do data-model ("se `species` está na linha e (`CurrentSpecies == \"\"` ou índice de `species` > índice de `CurrentSpecies`), `CurrentSpecies = species`; senão não muda"); `InsertLevel` passa a chamar `AdvanceTo(entry.Species)` depois de inserir; `Clone` copia `CurrentSpecies` — faz T002 passar
- [X] T005 Em `src/EvTrainer.Web/Domain/Json/PokemonJson.cs`: `ToDto` grava `Name = p.CurrentSpecies`; `FromDto` lê `CurrentSpecies = dto.Name?.Trim() ?? ""` (a normalização de dados antigos vem em US3) — faz T003 passar
- [X] T006 [P] Atualizar `tests/EvTrainer.Tests/Support/Pokemons.cs` (`CurrentSpecies = "Squirtle"`) e adicionar o builder `Pokemons.Pidgey()` com a linha Pidgey `[40, 45, 40, 35, 35, 56]` / Pidgeotto `[63, 60, 55, 50, 50, 71]` / Pidgeot `[83, 80, 75, 70, 70, 91]`; trocar os usos de `.Name = "Pidgey"` em `tests/EvTrainer.Tests/Components/Shell/PokemonMenuTests.cs` e `tests/EvTrainer.Tests/Components/Settings/DeletePanelTests.cs` por `Pokemons.Pidgey()`
- [X] T007 Adicionar a chave `"Pokemon.NoSpecies": "Pokémon sem espécie"` em `src/EvTrainer.Web/Localization/strings.pt-BR.json` e o helper `protected string PokemonLabel(TrainingPokemon pokemon)` em `src/EvTrainer.Web/Components/StoreAwareComponent.cs` (devolve `CurrentSpecies`, ou `L["Pokemon.NoSpecies"]` quando vazio)
- [X] T008 Substituir `pokemon.Name` por `PokemonLabel(pokemon)` em `src/EvTrainer.Web/Components/Evs/EvSummary.razor` (título, provisório até US2), `src/EvTrainer.Web/Components/Ivs/IvTable.razor` (`Ivs.Title`), `src/EvTrainer.Web/Components/Settings/DeletePanel.razor` (`Delete.Hint`, `Delete.Confirm` e o nome passado ao aviso `Toast.PokemonDeleted`) e `src/EvTrainer.Web/Components/Shell/PokemonMenu.razor` (texto do item; o sprite passa de `DisplaySpecies` para `CurrentSpecies`)
- [X] T009 Em `src/EvTrainer.Web/Components/Settings/SettingsFormModel.cs`, `SettingsForm.razor` e `src/EvTrainer.Web/State/TrainerStore.cs`, só o necessário para compilar com o modelo novo (`SaveSettings` grava `pokemon.CurrentSpecies = name.Trim()`); o campo Nome some em US1
- [X] T010 Rodar `dotnet build PokemonEvTrainer.slnx`, `dotnet format PokemonEvTrainer.slnx` e `dotnet test --solution PokemonEvTrainer.slnx`: tudo verde, com os testes que dependiam de `DisplaySpecies` ajustados para `CurrentSpecies`

**Checkpoint**: o modelo tem forma atual; a tela mostra o mesmo que antes para Pokémon cujo nome era a espécie.

---

## Phase 3: User Story 1 - Evoluir sem renomear (Priority: P1) 🎯 MVP

**Goal**: sem campo Nome; a forma atual é escolhida nos Ajustes (busca, "Evolui para…", chip da linha) e
avança sozinha por anotação de nível.

**Independent Test**: criar um Ralts, tocar em "Evolui para Kirlia" e salvar ⇒ menu, aba EVs e aba IVs
mostram Kirlia; nos Ajustes não existe campo de nome.

### Testes (escrever primeiro, ver falhar)

- [X] T011 [P] [US1] Reescrever `tests/EvTrainer.Tests/Components/Settings/SettingsFormTests.cs`: não existe `#settings-name` (substitui `Blank_name_disables_save` e `Custom_name_is_kept_when_changing_species`); `button.save` habilitado sem nada digitado quando metas e linha são válidas; escolher Charmeleon na busca de um Pokémon novo ⇒ salvo com `CurrentSpecies == "Charmeleon"` e linha Charmander/Charmeleon/Charizard; com Squirtle salvo, tocar no chip Wartortle e salvar ⇒ `CurrentSpecies == "Wartortle"`; tocar em `button.evolution-option` de uma linha de 1 espécie (Eevee → Vaporeon) e salvar ⇒ `CurrentSpecies == "Vaporeon"`; escolher na busca uma espécie fora da linha atual ⇒ forma atual = espécie escolhida
- [X] T012 [P] [US1] Ampliar `tests/EvTrainer.Tests/Components/Settings/SpeciesPickerTests.cs`: cada `li.species-chip` tem um `button.species-current` com `aria-pressed="true"` só na espécie igual a `Current` e `aria-label` = `T("Settings.Species.SetCurrent", nome)`; tocar no chip dispara `OnCurrent` com o nome; o texto `Settings.Species.CurrentHint` aparece abaixo da linha
- [X] T013 [P] [US1] Ampliar `tests/EvTrainer.Tests/State/TrainerStoreTests.cs`: `SaveSettings("Wartortle", …)` grava `CurrentSpecies`; `SaveSettings` com `current` fora da linha nova é ignorado (nada muda); `AddLevel` de nota Wartortle avança a forma atual; `ReplaceLevel` que troca uma nota para Blastoise avança a forma atual e o Desfazer devolve a nota **e** a forma atual anterior; `RemoveLevel` não altera a forma atual
- [X] T014 [P] [US1] Ampliar `tests/EvTrainer.Tests/Components/Shell/PokemonMenuTests.cs` (`Items_show_the_display_species_sprite_and_the_footer_credits` → sprite de `CurrentSpecies`): o item mostra o rótulo (espécie) e, quando a nature não é vazia, `span.nature` com o nome da nature; dois Squirtle com natures diferentes ficam distinguíveis; Pokémon com `CurrentSpecies == ""` mostra `T("Pokemon.NoSpecies")` e o selo "?"
- [X] T015 [P] [US1] Ampliar `tests/EvTrainer.Tests/Components/Ivs/LevelFormTests.cs`: a espécie pré-selecionada de uma anotação nova é `CurrentSpecies` (com forma atual Wartortle e última nota Squirtle ⇒ Wartortle)

### Implementação

- [X] T016 [US1] Em `src/EvTrainer.Web/Localization/strings.pt-BR.json`: remover `Settings.Name`; adicionar `"Settings.Species.SetCurrent": "{0} como forma atual"` e `"Settings.Species.CurrentHint": "Toque na forma em que o Pokémon está agora."`
- [X] T017 [US1] Em `src/EvTrainer.Web/Components/Settings/SpeciesPicker.razor`: parâmetros `Current` (`string`) e `OnCurrent` (`EventCallback<string>`); dentro de cada `li.species-chip`, o sprite e o texto ficam num `button.species-current` com `aria-pressed` e `aria-label` do contrato; `p.hint` com `Settings.Species.CurrentHint` depois da `ol.line` — faz T012 passar
- [X] T018 [US1] Em `src/EvTrainer.Web/Components/Settings/SettingsFormModel.cs`: trocar `Name` por `Current` (`From`: `Current = pokemon.CurrentSpecies`); `IsValid` = "metas 0–255, linha com 1–3 espécies e `Current` presente na linha" (sem exigência de nome)
- [X] T019 [US1] Em `src/EvTrainer.Web/Components/Settings/SettingsForm.razor`: remover o campo `#settings-name` (o `div.two` fica só com a nature); `PickSpecies` passa a fazer `_form.Current = pick.Picked` sempre (remove a regra de "manter apelido"); passar `Current="_form.Current"` e `OnCurrent` (que define `_form.Current`) ao `SpeciesPicker`; `Save` chama `Store.SaveSettings(_form.Current, …)` — faz T011 passar
- [X] T020 [US1] Em `src/EvTrainer.Web/State/TrainerStore.cs`: assinatura `SaveSettings(string current, string nature, int[] targets, IReadOnlyList<SpeciesEntry> species)`, ignorando a chamada quando `current` não está em `species`; o Desfazer de `ReplaceLevel` guarda e restaura também `CurrentSpecies` (o avanço já vem de `InsertLevel`) — faz T013 passar
- [X] T021 [US1] Em `src/EvTrainer.Web/Components/Ivs/LevelForm.razor`: a espécie inicial de uma anotação nova passa a ser `pokemon.CurrentSpecies` (fallback: primeira espécie da linha) — faz T015 passar
- [X] T022 [US1] Em `src/EvTrainer.Web/Components/Shell/PokemonMenu.razor`: acrescentar `span.nature.muted` com a nature quando não for vazia — faz T014 passar
- [X] T023 [US1] Em `src/EvTrainer.Web/wwwroot/css/app.css`: estilo de `button.species-current` (sem borda de botão, alvo ≥ 44 px, destaque com `--accent` quando `aria-pressed="true"`) e de `.pokemon-item .nature`

**Checkpoint**: US1 completa — evoluir é 1 toque no chip ou na evolução + salvar; nenhum nome digitado.

---

## Phase 4: User Story 2 - Ver a imagem do Pokémon na tela de EVs (Priority: P1)

**Goal**: o título em texto do resumo de EVs vira a imagem da forma atual (64 px) com rótulo acessível.

**Independent Test**: com um Kirlia selecionado, a aba EVs mostra a imagem de Kirlia, nenhum texto
"Kirlia" no `card-head`, e o título é anunciado como "EVs de Kirlia".

### Testes (escrever primeiro, ver falhar)

- [X] T024 [P] [US2] Atualizar `tests/EvTrainer.Tests/Components/Evs/EvSummaryTests.cs` (`Shows_title_totals_and_six_mini_cards`): `.card-head h2.summary-who` contém `span[role=img]` com `aria-label` = `T("Evs.Summary.Label", "Squirtle")` e dentro um `.sprite` com `--sprite-size: 64px` e `img[src="sprites/7.png"]`; o `TextContent` do `h2` não contém "Squirtle"; os totais `span.totals` continuam; com `CurrentSpecies == ""` o `aria-label` usa `Pokemon.NoSpecies` e o selo mostra "?"; trocar a forma atual para Wartortle e re-renderizar ⇒ `sprites/8.png`

### Implementação

- [X] T025 [US2] Em `src/EvTrainer.Web/Localization/strings.pt-BR.json`: remover `Evs.Summary.Title`; adicionar `"Evs.Summary.Label": "EVs de {0}"`
- [X] T026 [US2] Em `src/EvTrainer.Web/Components/Evs/EvSummary.razor`: o `h2` vira `<h2 class="summary-who"><span role="img" aria-label="@L["Evs.Summary.Label", PokemonLabel(pokemon)]"><PokemonSprite Name="@pokemon.CurrentSpecies" Size="64" /></span></h2>` — faz T024 passar
- [X] T027 [US2] Em `src/EvTrainer.Web/wwwroot/css/app.css`: `.summary-who` sem margem extra, alinhado ao centro vertical com `span.totals` no `card-head`; sprite em `image-rendering: pixelated`; conferir que em 360 px o `card-head` não quebra nem rola na horizontal

**Checkpoint**: US1 + US2 entregam o pedido completo para dados novos.

---

## Phase 5: User Story 3 - Dados antigos continuam funcionando (Priority: P2)

**Goal**: dados e backups com apelido abrem com a forma atual deduzida, sem perder nada.

**Independent Test**: importar um backup com `"name": "Gardi"`, linha Ralts/Kirlia/Gardevoir e nota nível
20 Kirlia ⇒ aparece como Kirlia; batalhas, metas, anotações e IVs idênticos.

### Testes (escrever primeiro, ver falhar)

- [X] T028 [P] [US3] Ampliar `tests/EvTrainer.Tests/Domain/CurrentFormTests.cs` com a tabela de `CurrentForm.Resolve(name, line, ivLog)` do data-model (`[Theory]`): `"Kirlia"` ⇒ Kirlia; `"kirlia"` ⇒ Kirlia (grafia da linha); `" Kirlia "` ⇒ Kirlia; `"Gardi"` com notas Ralts nv 5 e Kirlia nv 20 ⇒ Kirlia; `"Gardi"` com nota Ralts nv 30 depois de Kirlia nv 20 ⇒ Kirlia (vale a mais avançada na linha, não a de maior nível); `"Gardi"` sem notas ⇒ Ralts; `null` ⇒ primeira espécie; nota de espécie fora da linha é ignorada; linha com uma espécie sem nome ⇒ `""`
- [X] T029 [P] [US3] Ampliar `tests/EvTrainer.Tests/Domain/ModelJsonTests.cs` e `tests/EvTrainer.Tests/Domain/BackupFormatTests.cs`: documento com `"name": "Gardi"` é lido com `CurrentSpecies` deduzido e todos os outros campos iguais; `"name"` ausente é aceito; exportar e importar de novo dá o mesmo documento, agora com `"name": "Kirlia"`
- [X] T030 [P] [US3] Ampliar `tests/EvTrainer.Tests/Domain/LegacyExportGoldenTests.cs`: cada Pokémon do `user-data-export.json` de referência tem `CurrentSpecies` não vazio e presente na sua linha; os totais, faixas de IV e contagens existentes continuam iguais

### Implementação

- [X] T031 [US3] Criar `src/EvTrainer.Web/Domain/CurrentForm.cs` (`public static class CurrentForm`, C# puro) com `Resolve(string? name, IReadOnlyList<SpeciesEntry> line, IReadOnlyList<LevelEntry> ivLog)` na ordem do data-model: (1) `name` sem espaços nas pontas igual, sem diferenciar maiúsculas, a uma espécie da linha → nome da linha; (2) senão, entre as notas cuja espécie está na linha, a de maior índice na linha; (3) senão, a primeira espécie com nome não vazio; (4) senão `""` — faz T028 passar
- [X] T032 [US3] Em `src/EvTrainer.Web/Domain/Json/PokemonJson.cs`: `FromDto` monta a linha e o `IvLog` primeiro e então define `CurrentSpecies = CurrentForm.Resolve(dto.Name, species, ivLog)` (substitui o mapeamento direto de T005) — faz T029 e T030 passar

**Checkpoint**: todas as histórias funcionando; dados antigos abrem sem perda.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T033 Procurar em `src/EvTrainer.Web` e `tests/EvTrainer.Tests` sobras de `DisplaySpecies`, `Settings.Name`, `Evs.Summary.Title`, `nickname`/`apelido` e remover (inclusive o comentário "Keep a nickname" em `SettingsForm.razor`)
- [X] T034 [P] Rodar `tests/EvTrainer.Tests/Localization/KeyParityTests.cs` e `NoHardcodedTextTests.cs` e corrigir textos fixos ou chaves órfãs nas telas alteradas
- [X] T035 [P] Atualizar o `README.md` (se ele citar o campo Nome/apelido) e os screenshots em `docs/` que mostram o resumo de EVs, quando existirem
- [X] T036 `dotnet format PokemonEvTrainer.slnx --verify-no-changes` e `dotnet test --solution PokemonEvTrainer.slnx` verdes; rodar os cenários de [quickstart.md](./quickstart.md) no app (`dotnet run --project src/EvTrainer.Web`), inclusive 360 px e tema escuro

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** → **Foundational (Phase 2)** → bloqueia todas as histórias.
- **US1 (Phase 3)** e **US2 (Phase 4)** dependem só da Phase 2 e podem andar em paralelo, mas ambas
  editam `strings.pt-BR.json` e `app.css` (T016/T025, T023/T027): fazer essas tarefas uma de cada vez.
- **US3 (Phase 5)** depende só da Phase 2 (T005 é substituído por T032).
- **Polish (Phase 6)** depois das histórias desejadas.

### User Story Dependencies

- **US1 (P1)**: independente após a Phase 2.
- **US2 (P1)**: independente após a Phase 2; usa `PokemonLabel` (T007).
- **US3 (P2)**: independente após a Phase 2; dá sentido aos dados antigos que US1/US2 exibem.

### Within Each User Story

- Testes primeiro, falhando; depois chaves de idioma → componentes/store → CSS.
- `TrainerStore.cs` (T009, T020) e `PokemonJson.cs` (T005, T032) são editados em sequência.

### Parallel Opportunities

- Phase 2: T003 e T006 em paralelo com T002.
- US1: T011–T015 (arquivos de teste diferentes) em paralelo.
- US3: T028–T030 em paralelo.
- Com a Phase 2 pronta, US1, US2 e US3 podem ser feitas por pessoas diferentes, cuidando dos arquivos
  compartilhados acima.

---

## Parallel Example: User Story 1

```bash
# Testes de US1 juntos (arquivos diferentes):
Task: "T011 Reescrever SettingsFormTests.cs (sem campo Nome, forma atual)"
Task: "T012 Ampliar SpeciesPickerTests.cs (chips alternáveis)"
Task: "T013 Ampliar TrainerStoreTests.cs (SaveSettings, avanço por nota, Desfazer)"
Task: "T014 Ampliar PokemonMenuTests.cs (rótulo + nature)"
Task: "T015 Ampliar LevelFormTests.cs (espécie inicial = forma atual)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1 + Phase 2.
2. Phase 3 (US1) → **validar**: cenário 1 do quickstart.
3. Pode ser entregue sozinha: o nome some e a evolução é 1 toque; o resumo de EVs mostra o nome da
   espécie em texto até US2.

### Incremental Delivery

1. Setup + Foundational → modelo com forma atual.
2. US1 → evoluir sem renomear (MVP).
3. US2 → imagem no resumo de EVs (pedido completo).
4. US3 → dados antigos com apelido livre viram a forma certa.

---

## Notes

- [P] = arquivos diferentes, sem dependência pendente.
- Verificar que cada teste falha antes de implementar (Princípio III).
- Commit ao fim de cada fase ou grupo lógico.
