---

description: "Task list for Avaliar IVs na captura"
---

# Tasks: Avaliar IVs na captura

**Input**: Design documents from `/specs/005-capture-iv-check/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/capture.md](./contracts/capture.md),
[quickstart.md](./quickstart.md)

**Tests**: OBRIGATÓRIOS (constituição, Princípio III). Escrever, rodar com
`dotnet test --solution PokemonEvTrainer.slnx`, **ver falhar**, então implementar.

**Organization**: US1 avaliar a captura (P1), US2 adicionar à lista (P2), US3 melhor até agora (P3).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)
- **[Story]**: US1, US2, US3

## Convenções

- Mesmas das features anteriores: dotnet CLI primeiro, `dotnet format PokemonEvTrainer.slnx` após
  cada tarefa com código, domínio sem Blazor/IO, **todo texto novo em**
  `src/EvTrainer.Web/Localization/strings.pt-BR.json` (Princípio VII).
- Vetores `[HP, Ataque, Defesa, SpAtk, SpDef, Speed]`.
- Testes de componentes herdam `tests/EvTrainer.Tests/Support/AppTestContext.cs`.
- Edições em `TrainerStore.cs`, `CaptureStore.cs`, `CaptureView.razor`, `strings.pt-BR.json` e
  `app.css` são sequenciais entre si.
- Caso de referência (contracts/capture.md): Squirtle Nv 5 Timid `20/9/12/11/12/11` ⇒ faixas
  `12–31, 4–31, 10–29, 20–31, 12–31, 14–31`; contam HP, DEF, SPA, SPD, SPE; veredito Incerto
  (média dos mínimos 13 = Médio, dos máximos 30 = Excelente); nature Favorece.

---

## Phase 1: Setup

- [X] T001 Confirmar base verde: `dotnet format PokemonEvTrainer.slnx --verify-no-changes` e `dotnet test --solution PokemonEvTrainer.slnx` (328 testes) antes de qualquer mudança

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: cálculo de IV reaproveitável, domínio da avaliação e armazenamento das notas de captura — usados por todas as histórias.

**⚠️ CRITICAL**: nenhuma história começa antes desta fase.

- [X] T002 [P] Ampliar `tests/EvTrainer.Tests/Domain/IvTests.cs`: a sobrecarga `Ivs.RangeFor(SpeciesEntry? species, Nature? nature, int level, int[] stats, int[] evs)` dá as mesmas células que a versão por `LevelEntry` para as 6 anotações do HANDOFF; espécie `null` ⇒ `IvInconsistent` nos stats preenchidos; espécie com base de HP 1 (Shedinja, `[1, 90, 45, 30, 30, 40]`) e stat de HP 1 ⇒ `Cells[0]` é `IvUnknowable`; `Final` ignora `IvUnknowable` (fica `FinalNoData` quando é a única linha); atualizar o helper `Show(IvCell)` do teste com o caso novo
- [X] T003 Em `src/EvTrainer.Web/Domain/Ivs.cs`: novo caso `public sealed record IvUnknowable : IvCell;` (HP com base de HP 1), sobrecarga pura de `RangeFor` e a versão antiga delegando para ela (research R2/R3); em `src/EvTrainer.Web/Components/Ivs/IvTable.razor` exibir `IvUnknowable` com a chave nova `Ivs.Cell.Unknowable` ("—" curto, `title` com `Ivs.Cell.UnknowableHint` "HP de Shedinja é sempre 1") — faz T002 passar; o compilador aponta todo `switch` sobre `IvCell`
- [X] T004 [P] Teste `tests/EvTrainer.Tests/Domain/CaptureTests.cs` (domínio, sem `Compare`): `Capture.Grade` nos limites `10→Poor, 11→Fair, 19→Fair, 20→Good, 25→Good, 26→Great`; `DefaultCounted` Abra `[25,20,15,105,55,90]` ⇒ `[T,F,T,T,T,T]`, Machop `[70,80,50,35,35,35]` ⇒ `[T,T,T,F,T,T]`, Ataque = SpAtk ⇒ todos `true`; `Evaluate` do caso de referência (faixas, `Grades[1]` = `GradeSpan(Poor, Great)` para `4–31`, `VerdictUncertain(Fair, Excellent)`, `NatureEffect.Helps`, `Score` = soma de `Min + Max` dos stats que contam); stat impossível que conta ⇒ `VerdictUnavailable(Inconsistent)` e `Score == null`; stat impossível que não conta ⇒ veredito normal; nenhum stat contando ⇒ `VerdictUnavailable(NothingCounted)`; faixas estreitas ⇒ `VerdictKnown`; Shedinja ⇒ HP não conta mesmo marcado; `NatureEffect`: Hardy ⇒ `Neutral`, Modest (+SPA −ATK) com ATK contando ⇒ `Harms`, Timid com ATK não contando ⇒ `Helps`, nature que sobe e desce stats que não contam ⇒ `Neutral`
- [X] T005 Criar `src/EvTrainer.Web/Domain/Capture.cs` conforme contracts/capture.md: `CaptureInput(string Species, int Level, string Nature, int[] Stats)` com `IsValid` ("Level 1–100", espécie no Pokédex, nature conhecida, "cada stat 1–999"), `IvGrade`, `GradeSpan`, `VerdictGrade`, `VerdictProblem`, `closed record Verdict` (+3 casos), `NatureEffect`, `CaptureResult`, `Capture.Grade`, `Capture.DefaultCounted`, `Capture.Evaluate` (EVs zero; médias com divisão inteira; limites de veredito Fraco < 11 ≤ Médio < 20 ≤ Bom < 26 ≤ Excelente) — faz T004 passar
- [X] T006 [P] Teste `tests/EvTrainer.Tests/Domain/CaptureNotesJsonTests.cs`: ida e volta de `CaptureNotes` (`counted` e `best` com `lv`, `nature`, `st`); leitura tolerante — documento ausente/JSON inválido ⇒ vazio; `counted` com tamanho ≠ 6 ⇒ ignorado; `best` com `lv` fora de "1–100", nature desconhecida, `st` sem 6 valores "1–999" ou espécie fora do Pokédex ⇒ entrada descartada; chaves gravadas com o nome canônico da espécie
- [X] T007 Criar `src/EvTrainer.Web/Domain/Json/CaptureNotesJson.cs` (DTOs + registro no `EvTrainerJsonContext` de `PokemonJson.cs`) e a classe `CaptureNotes` (`Counted`, `Best`) em `src/EvTrainer.Web/Domain/Capture.cs` — faz T006 passar
- [X] T008 Em `src/EvTrainer.Web/State/IPokemonRepository.cs` adicionar `CaptureNotes LoadCaptureNotes()` e `void SaveCaptureNotes(CaptureNotes notes)`; implementar em `src/EvTrainer.Web/State/LocalStorageRepository.cs` (chave `evtrainer:v1:capture`) e em `tests/EvTrainer.Tests/Support/InMemoryRepository.cs` (cópia em memória + contador de gravações)
- [X] T009 [P] Teste `tests/EvTrainer.Tests/State/CaptureStoreTests.cs` (parte de stats que contam): `CountedFor` sem marcação ⇒ `DefaultCounted` da espécie; `SetCounted` grava no repositório e dispara `Changed`; recarregar o store mantém a marcação; `ResetCounted` volta ao padrão e grava
- [X] T010 Criar `src/EvTrainer.Web/State/CaptureStore.cs` com `CountedFor`, `SetCounted`, `ResetCounted` e `Changed`; registrar em `src/EvTrainer.Web/Program.cs` (`AddSingleton<CaptureStore>()`) e em `tests/EvTrainer.Tests/Support/AppTestContext.cs` (propriedade `Capture`) — faz T009 passar
- [X] T011 Rodar `dotnet format` e `dotnet test` (tudo verde)

**Checkpoint**: domínio e armazenamento prontos.

---

## Phase 3: User Story 1 - Avaliar um Pokémon recém-capturado (Priority: P1) 🎯 MVP

**Goal**: tela **Avaliar captura** com espécie, nível, nature e 6 stats ⇒ faixa e grau por stat, stats que contam (automáticos, ajustáveis, lembrados), veredito e efeito da nature; nada gravado na lista.

**Independent Test**: caso de referência ⇒ faixas iguais à tabela de IVs, ATK esmaecido, veredito "Incerto" com dica de nível baixo, nature "Favorece"; `Repository.Stored` inalterado.

### Tests for User Story 1 ⚠️

- [X] T012 [P] [US1] Teste `tests/EvTrainer.Tests/Components/Shared/SpeciesSearchTests.cs` (bUnit): busca por nome com acento/caixa ignorados e por número (`#63`) lista opções com sprite e `Settings.Species.Dex`; tocar uma opção dispara `OnPick` com a `PokedexSpecies`; `tests/EvTrainer.Tests/Components/Settings/SpeciesPickerTests.cs` continua verde sem mudanças
- [X] T013 [P] [US1] Teste `tests/EvTrainer.Tests/Components/Capture/CaptureViewTests.cs` (bUnit, renderizando `Home`): item `Menu.Capture` do menu e botão `Empty.Capture` da tela vazia abrem a tela (título `Capture.Title`); campos: busca de espécie, nível e 6 stats com `inputmode="numeric"`, nature com as 25 opções (rótulo com +/−); sem espécie ou com campo inválido ⇒ sem resultados e campo com `aria-invalid="true"`; caso de referência ⇒ 6 linhas com faixa e grau (`Capture.Grade.*`, faixa larga como `Capture.GradeSpan`), linha de ATK com classe `not-counted` e botão "conta" `aria-pressed="false"`; veredito `Capture.Verdict.Uncertain` e dica `Capture.LowLevelHint`; nature `Capture.Nature.Helps`; tocar o botão de ATK ⇒ `aria-pressed="true"`, veredito recalculado e marcação gravada (`Capture.CountedFor("Squirtle")[1] == true`); `Capture.ResetCounted` via botão `Capture.Counted.Reset`; stat impossível ⇒ "?" e explicação `Capture.Inconsistent`; Shedinja com HP 1 ⇒ `Capture.Unknowable`; **Nova avaliação** limpa só os stats (espécie, nível e nature ficam); **Fechar** volta às abas (ou à tela vazia); `Repository.Stored` não muda em nenhum desses passos

### Implementation for User Story 1

- [X] T014 [US1] Extrair a busca de `src/EvTrainer.Web/Components/Settings/SpeciesPicker.razor` para `src/EvTrainer.Web/Components/Shared/SpeciesSearch.razor` (`EventCallback<PokedexSpecies> OnPick`) e usá-la no `SpeciesPicker` — faz T012 passar
- [X] T015 [US1] Criar `src/EvTrainer.Web/Components/Capture/CaptureStatsTable.razor` (por stat: rótulo, faixa, grau, botão "conta" com `aria-pressed` e `aria-label` `Capture.Counted.Toggle`; linhas que não contam com classe `not-counted`) e `src/EvTrainer.Web/Components/Capture/CaptureView.razor` (espécie via `SpeciesSearch` → chip com sprite e **Trocar**; nível; nature reaproveitando o rótulo +/− de `SettingsForm`; 6 stats; resultado ao vivo via `Capture.Evaluate` e `CaptureStore.CountedFor`; veredito; efeito da nature; dica de nível baixo quando `VerdictUncertain`; explicações de "?" e de Shedinja; **Restaurar padrão**; **Nova avaliação**; parâmetro `EventCallback OnClose`)
- [X] T016 [US1] Em `src/EvTrainer.Web/Pages/Home.razor` adicionar a visão de captura (estado `_capture`, renderiza `CaptureView` no lugar das abas/tela vazia); `src/EvTrainer.Web/Components/Shell/PokemonMenu.razor` ganha o item **Avaliar captura** (`Menu.Capture`) e `AppHeader` repassa o pedido; `src/EvTrainer.Web/Components/Shell/EmptyState.razor` ganha o botão `Empty.Capture` (`EventCallback OnCapture`)
- [X] T017 [US1] Chaves novas em `src/EvTrainer.Web/Localization/strings.pt-BR.json`: `Menu.Capture`, `Empty.Capture`, `Capture.Title`, `Capture.Close`, `Capture.Species`, `Capture.ChangeSpecies`, `Capture.Level`, `Capture.Nature`, `Capture.Stats`, `Capture.Hint` (tela de resumo, EVs zero), `Capture.Grade.Poor|Fair|Good|Great`, `Capture.GradeSpan` ("{0} a {1}"), `Capture.Verdict.Title`, `Capture.Verdict.Weak|Fair|Good|Excellent`, `Capture.Verdict.Uncertain` ("Incerto: entre {0} e {1}"), `Capture.Verdict.NothingCounted`, `Capture.Verdict.Inconsistent`, `Capture.LowLevelHint`, `Capture.Nature.Helps|Neutral|Harms`, `Capture.Counted.Toggle`, `Capture.Counted.Reset`, `Capture.Inconsistent`, `Capture.Unknowable`, `Capture.New`, `Ivs.Cell.Unknowable`, `Ivs.Cell.UnknowableHint` — faz T013 passar
- [X] T018 [US1] Estilos em `src/EvTrainer.Web/wwwroot/css/app.css`: tela de captura em coluna única, chip de espécie, tabela de stats sem rolagem horizontal em 360 px, graus com cores (`--ok`, `--warn`, `--bad`), linhas `not-counted` esmaecidas, botões ≥ 44 px
- [X] T019 [US1] Rodar `dotnet format` e `dotnet test`; validar [quickstart.md](./quickstart.md) passos 1, 2, 5 e 6

**Checkpoint**: MVP — o jogador avalia capturas e decide se fica ou solta.

---

## Phase 4: User Story 2 - Ficar com o Pokémon e adicioná-lo à lista (Priority: P2)

**Goal**: **Adicionar à minha lista** cria o Pokémon com linha evolutiva, nature e a anotação do nível avaliado.

**Independent Test**: adicionar o caso de referência ⇒ Squirtle selecionado, linha Squirtle → Wartortle → Blastoise, Timid, metas zero, anotação Nv 5 com EVs zero; aba IVs com as mesmas faixas; Desfazer remove.

### Tests for User Story 2 ⚠️

- [X] T020 [P] [US2] Ampliar `tests/EvTrainer.Tests/State/TrainerStoreTests.cs`: `AddFromCapture(input, dex)` cria Pokémon com `CurrentSpecies` = espécie capturada (feature 006), `Species` = `dex.LineFor` (Squirtle → Wartortle → Blastoise; Gloom só até Gloom), `Nature`, `Targets` zero, `IvLog` = uma anotação (`Level`, `Species` capturada, `Stats`, `Evs` zero), seleciona e grava; espécie repetida cria outro id; o Desfazer remove o novo Pokémon e restaura a seleção anterior
- [X] T021 [P] [US2] Ampliar `tests/EvTrainer.Tests/Components/Capture/CaptureViewTests.cs`: botão `Capture.Add` desabilitado sem avaliação válida; com o caso de referência ⇒ cria e seleciona o Pokémon, fecha a tela e abre a aba IVs, cuja linha do Nv 5 e linha Final mostram as mesmas faixas da avaliação (SC-002); toast `Capture.Added` com Desfazer

### Implementation for User Story 2

- [X] T022 [US2] Em `src/EvTrainer.Web/State/TrainerStore.cs`: `(string Id, Action Undo) AddFromCapture(CaptureInput input, Pokedex dex)` (research R9) — faz T020 passar
- [X] T023 [US2] Em `src/EvTrainer.Web/Components/Capture/CaptureView.razor`: botão **Adicionar à minha lista** (`Capture.Add`) e parâmetro `EventCallback OnAdded`; em `src/EvTrainer.Web/Pages/Home.razor` fechar a captura e ir para `Tab.Ivs` ao adicionar; chaves `Capture.Add`, `Capture.Added` — faz T021 passar
- [X] T024 [US2] Rodar `dotnet format` e `dotnet test`; validar [quickstart.md](./quickstart.md) passo 4

**Checkpoint**: capturar → avaliar → treinar sem redigitar.

---

## Phase 5: User Story 3 - Comparar com o melhor até agora (Priority: P3)

**Goal**: melhor até agora por espécie, comparação ao vivo, substituição ao concluir, Esquecer e Adicionar a partir do melhor.

**Independent Test**: concluir uma avaliação ⇒ vira o melhor; uma melhor ⇒ cartão "Melhor" e substitui ao concluir; uma pior ⇒ "Pior" e não substitui.

### Tests for User Story 3 ⚠️

- [X] T025 [P] [US3] Ampliar `tests/EvTrainer.Tests/Domain/CaptureTests.cs` com `Capture.Compare`: scores 100 × 104 com a mesma nature ⇒ `Equivalent`; 100 × 105 ⇒ `Worse` (e `Better` invertido); empate com `Helps` × `Neutral` ⇒ `Better` para quem tem `Helps`; `Neutral` × `Harms` idem
- [X] T026 [P] [US3] Ampliar `tests/EvTrainer.Tests/State/CaptureStoreTests.cs`: `Conclude` com avaliação inválida ou com "?" num stat que conta ⇒ `null`, nada gravado; primeira válida ⇒ vira o melhor (`BestFor`), grava e o Desfazer volta a "sem melhor"; melhor que a guardada ⇒ substitui e o Desfazer restaura a anterior; equivalente ou pior ⇒ `null`; a comparação usa a marcação atual de stats que contam para os dois lados; `Forget` remove com Desfazer
- [X] T027 [P] [US3] Ampliar `tests/EvTrainer.Tests/Components/Capture/CaptureViewTests.cs`: sem melhor ⇒ cartão `Capture.Best.None`; com melhor guardado, digitar uma avaliação melhor ⇒ cartão `Capture.Best.Title` com resumo (nível, nature, veredito), selo `Capture.Compare.Better` e aviso `Capture.Best.WillSave`, sem gravar enquanto digita; **Nova avaliação**, **Fechar** e **Adicionar à minha lista** concluem (toast `Capture.Best.Saved` com Desfazer quando substitui); avaliação pior ⇒ selo `Capture.Compare.Worse` e nada muda ao concluir; **Esquecer** (`Capture.Best.Forget`) com Desfazer; **Adicionar à minha lista** no cartão cria o Pokémon a partir do melhor

### Implementation for User Story 3

- [X] T028 [US3] Em `src/EvTrainer.Web/Domain/Capture.cs`: `Capture.Compare(CaptureResult current, CaptureResult best)` (research R6: margem ≤ 4 em `Score` ⇒ empate; desempate Helps > Neutral > Harms) — faz T025 passar
- [X] T029 [US3] Em `src/EvTrainer.Web/State/CaptureStore.cs`: `BestFor`, `Action? Conclude(CaptureInput input)` e `Action? Forget(string species)` — faz T026 passar
- [X] T030 [US3] Criar `src/EvTrainer.Web/Components/Capture/BestCaptureCard.razor` (resumo do melhor recalculado com `Capture.Evaluate`, selo da comparação, aviso "será guardado ao concluir", **Esquecer**, **Adicionar à minha lista**) e, em `src/EvTrainer.Web/Components/Capture/CaptureView.razor`, chamar `CaptureStore.Conclude` em **Nova avaliação**, **Fechar** e **Adicionar à minha lista** (toast com Desfazer quando substitui); chaves `Capture.Best.Title`, `Capture.Best.None`, `Capture.Best.Summary`, `Capture.Best.WillSave`, `Capture.Best.Saved`, `Capture.Best.Forget`, `Capture.Best.Forgotten`, `Capture.Compare.Better|Equivalent|Worse`; estilos do cartão em `app.css` — faz T027 passar
- [X] T031 [US3] Rodar `dotnet format` e `dotnet test`; validar [quickstart.md](./quickstart.md) passo 3

**Checkpoint**: todas as histórias funcionando.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T032 Ampliar `tests/EvTrainer.Tests/Localization/NoHardcodedTextTests.cs`: abrir a tela de captura a partir do menu e da tela vazia, com resultado, stat "?", Shedinja e cartão de melhor até agora, e verificar ausência de texto fixo (SC-005)
- [X] T033 [P] Ampliar `tests/EvTrainer.Tests/State/TrainerStoreTests.cs` (ou `BackupPanelTests.cs`): com notas de captura gravadas, `ExportBackup()` não contém `counted` nem `best` (quickstart passo 8)
- [X] T034 Validar no app em 360 px, temas claro e escuro, offline, [quickstart.md](./quickstart.md) passos 1–8; confirmar que nenhuma tecla digitada grava o melhor até agora (FR-015)
- [X] T035 [P] Atualizar `README.md` (Features): avaliação de IVs na captura com notas por stat, stats que contam, melhor até agora por espécie e "Adicionar à minha lista"
- [X] T036 Rodar `dotnet format PokemonEvTrainer.slnx --verify-no-changes` e `dotnet test --solution PokemonEvTrainer.slnx` (tudo verde) como portão final

---

## Dependencies & Execution Order

- **Setup (T001)** → **Fundação (T002–T011)** → US1 → US2 e US3 → Polish.
- Fundação: T002 → T003 (IvCell novo, base do `Evaluate`); T004 → T005 (depende de T003); T006 → T007
  → T008 → T009 → T010.
- US1 depende da Fundação. US2 e US3 dependem da US1 (`CaptureView`); US2 e US3 são independentes
  entre si no domínio/store, mas ambas editam `CaptureView.razor` e `Home.razor` — fazer US2 antes da
  US3 (US3 reaproveita o "Adicionar" no cartão).
- Dentro de cada história: testes falhando → domínio → store → componentes → format/test.
- T003 obriga a atualizar `switch`es de `IvCell` (hierarquia `closed`): o build aponta onde.

### Parallel Opportunities

- Fundação: T002, T004 e T006 (testes em arquivos diferentes) em paralelo; T009 após T008.
- US1: T012 e T013 em paralelo.
- US2: T020 e T021 em paralelo.
- US3: T025, T026 e T027 em paralelo.
- Polish: T033 e T035 em paralelo.

## Parallel Example: Foundational

```text
Task: "T002 Ampliar tests/EvTrainer.Tests/Domain/IvTests.cs (sobrecarga de RangeFor, Shedinja)"
Task: "T004 Teste tests/EvTrainer.Tests/Domain/CaptureTests.cs (graus, padrão, veredito, nature)"
Task: "T006 Teste tests/EvTrainer.Tests/Domain/CaptureNotesJsonTests.cs (ida e volta, tolerância)"
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Fase 1 e Fase 2.
2. US1 (T012–T019) → validar quickstart 1, 2, 5 e 6 → pode publicar: o jogador já decide "fico ou
   solto".

### Incremental Delivery

1. + US2 → capturar e ir direto para o treino.
2. + US3 → caça repetida com melhor até agora.
3. Polish → texto fixo, backup, 360 px, README.
