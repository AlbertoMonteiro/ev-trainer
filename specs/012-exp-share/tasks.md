# Tasks: Exp. Share

**Input**: Design documents from `/specs/012-exp-share/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/exp-share.md, quickstart.md

**Tests**: obrigatórios (Princípio III — TDD): cada teste é escrito e visto falhar antes do código.

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Setup

Nenhuma tarefa: sem pacotes nem projetos novos.

## Phase 2: Foundational

**Purpose**: os dois campos novos no modelo e no JSON; tudo depende deles.

- [X] T001 Em `tests/EvTrainer.Tests/Domain/ModelJsonTests.cs` (ou `BackupFormatTests.cs`): ida e volta de um Pokémon com `ExpShare = true` e um evento `Shared = true`; JSON gravado contém `"expShare": true` e `"x": true`; com `false` as chaves **não** aparecem; arquivo antigo sem as chaves lê `ExpShare = false` e `Shared = false`; `Clone()` copia os dois campos. Ver falhar.
- [X] T002 Em `src/EvTrainer.Web/Domain/Model/TrainingPokemon.cs`: `public bool ExpShare { get; set; }` em `TrainingPokemon` (doc: "Holds the Exp. Share (spec 012); at most one Pokémon in the set.") e `public bool Shared { get; set; }` em `BattleEvent` (doc: "Battle received through the Exp. Share."); incluir nos dois `Clone()`.
- [X] T003 Em `src/EvTrainer.Web/Domain/Json/PokemonJson.cs`: `PokemonDto.ExpShare` (`[JsonPropertyName("expShare")]`, `bool?`, `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]`, gravado como `true` ou `null`) e `EventDto.Shared` (`[JsonPropertyName("x")]`, mesmo padrão); mapear nos dois sentidos. T001 verde; `BackupFormatTests` e `LegacyExportGoldenTests` continuam verdes.

## Phase 3: User Story 1 — Indicar quem está com o Exp. Share (P1) 🎯 MVP

**Goal**: marcar o portador; cada batalha do principal também conta para ele, com as regras da Gen 3.

**Independent Test**: dois Pokémon, portador marcado no segundo, batalha no primeiro → EVs dos dois sobem conforme a tabela de `data-model.md`.

- [X] T004 [P] [US1] Criar `tests/EvTrainer.Tests/Domain/ExpShareTests.cs` para `BattleLog.RecordShared`: tabela de `data-model.md` (portador sem Pokérus → mult. 1; com Pokérus → 2; `MachoBrace` do portador ignorado); evento criado com `Shared = true`; segunda batalha igual agrupa (`Count = 2`); não agrupa com evento próprio (não compartilhado) do mesmo adversário nem com multiplicador diferente nem com ajuste manual; limites: portador com 510 no total → `RecordResult.GainedAny == false`; atributo em 255 → ganho 0 nesse atributo.
- [X] T005 [US1] Implementar `public static RecordResult RecordShared(TrainingPokemon holder, string opponent, ReferenceData data)` em `src/EvTrainer.Web/Domain/BattleLog.cs` ("Multiplier = holder.Pokerus ? 2 : 1"; agrupa só se o último evento for `Shared`, mesmo adversário canônico e mesmo multiplicador). T004 verde.
- [X] T006 [P] [US1] Em `tests/EvTrainer.Tests/State/TrainerStoreTests.cs`: `SetExpShare(id)` marca só esse Pokémon (FR-002: marcar outro desmarca o anterior); `SetExpShare(null)` desmarca; `ExpShareHolderId` reflete; `RecordBattle("Caterpie")` com portador ≠ selecionado → principal e portador ganham conforme a tabela (os 5 casos de `data-model.md`), resultado `SharedRecord` com `HolderId`; vitamina (`"HP Up"`) não compartilha; portador = selecionado → `HolderId` nulo e só um ganho; `DeletePokemon(portador)` → `ExpShareHolderId` nulo, desfazer devolve; `Load`/`ImportBackup`/`ApplySync` com 2 marcados → fica o de `UpdatedAt` mais recente (empate: menor `Order`).
- [X] T007 [US1] Em `src/EvTrainer.Web/State/TrainerStore.cs`: `public string? ExpShareHolderId`; `public void SetExpShare(string? id)` (desmarca todos, marca `id`, `Commit` dos alterados); `RecordBattle` passa a devolver `SharedRecord?` (novo `public sealed record SharedRecord(string MainId, RecordResult Main, string? HolderId, RecordResult? Holder)` em `src/EvTrainer.Web/Domain/BattleLog.cs`) chamando `BattleLog.RecordShared` quando há portador ≠ selecionado e `!data.Find(opponent).IsVitamin` (usar a API existente de `ReferenceData` para achar o adversário canônico); normalização do portador único após `Load`, `ImportBackup` e `ApplySync`. T006 verde (parte de registro/portador).
- [X] T008 [P] [US1] Em `tests/EvTrainer.Tests/Components/Evs/EvSummaryTests.cs`: com 1 Pokémon, sem `select#exp-share`; com 2, `label[for=exp-share]` = `T("ExpShare.Label")`, opções `""` (`T("ExpShare.None")`) + o outro Pokémon; escolher o outro → `Store.ExpShareHolderId` = id dele; com o selecionado sendo o portador, nota `.exp-share-self` = `T("ExpShare.Self")`.
- [X] T009 [US1] Adicionar chaves `ExpShare.Label`, `ExpShare.None`, `ExpShare.Self`, `Toast.ExpShareGain`, `Toast.ExpShareNoGain`, `History.Shared` com os textos de `contracts/exp-share.md` em `src/EvTrainer.Web/Localization/strings.pt-BR.json` e `src/EvTrainer.Web/Localization/strings.en.json`.
- [X] T010 [US1] Em `src/EvTrainer.Web/Components/Evs/EvSummary.razor`: com `Store.Ordered.Count > 1`, abaixo de `.summary-grid` (e acima do `EvEditor`), `div.exp-share-field` com `label[for=exp-share]` e `select#exp-share.exp-share` (opção vazia + outros Pokémon pelo `PokemonLabel`; se o selecionado for o portador, incluí-lo como opção selecionada e mostrar `p.exp-share-self.hint`); `@onchange` → `Store.SetExpShare(valor vazio ? null : valor)`. CSS mínimo em `src/EvTrainer.Web/wwwroot/css/app.css` (`.exp-share-field`: grid label/select em uma linha, `select` com `min-height: 44px`). T008 verde.
- [X] T011 [US1] Ajustar chamadores de `RecordBattle` (`src/EvTrainer.Web/Components/Evs/EvTab.razor` e testes que usam o retorno) para o novo `SharedRecord` sem mudar o comportamento do principal.

**Checkpoint**: US1 — a conta compartilhada funciona e o portador é escolhido na tela de EVs.

## Phase 4: User Story 2 — Ver e desfazer o que foi compartilhado (P1)

**Goal**: aviso com os dois ganhos, Desfazer dos dois, histórico marcado.

**Independent Test**: batalha com portador ativo → aviso com os dois ganhos; Desfazer → EVs dos dois voltam.

- [X] T012 [P] [US2] Em `tests/EvTrainer.Tests/Domain/ExpShareTests.cs`: `UndoLastShared(holder, "Caterpie", data)` remove 1 unidade do último evento compartilhado do mesmo adversário; não faz nada se o último evento não for compartilhado ou for de outro adversário.
- [X] T013 [US2] Implementar `public static void UndoLastShared(TrainingPokemon holder, string opponent, ReferenceData data)` em `src/EvTrainer.Web/Domain/BattleLog.cs`. T012 verde.
- [X] T014 [P] [US2] Em `tests/EvTrainer.Tests/State/TrainerStoreTests.cs`: `UndoBattle(record)` desfaz principal e portador; continua desfazendo o portador correto mesmo se `SetExpShare` mudou depois do registro.
- [X] T015 [US2] Em `src/EvTrainer.Web/State/TrainerStore.cs`: `public void UndoBattle(SharedRecord record)` (`BattleLog.UndoLast` no principal, `BattleLog.UndoLastShared` no `HolderId` se houver; `Commit` dos dois); manter `UndoLastBattle(id)` só se ainda houver chamadores. T014 verde.
- [X] T016 [P] [US2] Em `tests/EvTrainer.Tests/Components/Evs/EvTabFlowTests.cs`: com portador "Abra", tocar Caterpie → texto do aviso contém o ganho do principal e `T("Toast.ExpShareGain", <rótulo do Abra>, <ganho>)`; portador em 510 → contém `T("Toast.ExpShareNoGain", <rótulo>)`; tocar Desfazer → totais dos dois voltam.
- [X] T017 [US2] Em `src/EvTrainer.Web/Components/Evs/EvTab.razor`: montar a mensagem do principal como hoje e, se `record.HolderId` existir, acrescentar " · " + `Toast.ExpShareGain` (ganhos formatados com `Evs.Gain.Part`) ou `Toast.ExpShareNoGain`; Desfazer → `Store.UndoBattle(record)`. T016 verde.
- [X] T018 [P] [US2] Em `tests/EvTrainer.Tests/Components/Evs/RecentHistoryTests.cs`: evento `Shared` mostra `span.meta.shared` com `T("History.Shared")`; remover pelo histórico do portador remove só esse evento (principal intacto).
- [X] T019 [US2] Em `src/EvTrainer.Web/Components/Evs/RecentHistory.razor`: para `e.Shared`, `<span class="meta shared">@(" · " + L["History.Shared"])</span>`. T018 verde.

**Checkpoint**: US1 + US2 — feature completa.

## Phase 5: Polish

- [X] T020 [P] `tests/EvTrainer.Tests/Localization/NoHardcodedTextTests.cs` e `EnglishScreensTests.cs`: com 2 Pokémon e portador marcado, a aba EVs (seletor, aviso compartilhado) não tem texto fixo nem português em inglês
- [X] T021 `dotnet build PokemonEvTrainer.slnx --no-incremental` sem avisos, `dotnet test --solution PokemonEvTrainer.slnx`, `dotnet format PokemonEvTrainer.slnx --verify-no-changes`
- [X] T022 Validação no navegador conforme `quickstart.md` (320 e 1366 px, claro/escuro, pt e en): seletor cabe numa linha, aviso legível
- [X] T023 [P] `README.md`: bullet sobre o Exp. Share (EVs inteiros para quem segura; Pokérus dele; Macho Brace não conta)

## Dependencies

- Phase 2 (T001→T002→T003) bloqueia tudo.
- US1: T004→T005; T006→T007 (depende de T005); T008/T009→T010; T011 depois de T007.
- US2 depende de US1 (T007 `SharedRecord`): T012→T013; T014→T015; T016→T017; T018→T019.
- Polish depois de US2.

## Parallel Example

```text
# Depois da Phase 2:
T004 ExpShareTests.cs  |  T006 TrainerStoreTests.cs  |  T008 EvSummaryTests.cs
# Na US2:
T012 ExpShareTests.cs  |  T016 EvTabFlowTests.cs  |  T018 RecentHistoryTests.cs
```

## Implementation Strategy

MVP = Phase 2 + US1 (conta compartilhada correta). US2 (aviso, Desfazer, histórico) é necessária para a confiança
do jogador e entra no mesmo commit da feature, como nas anteriores.
