---

description: "Task list for Edição manual de EVs, IVs e anotações"
---

# Tasks: Edição manual de EVs, IVs e anotações

**Input**: Design documents from `/specs/004-manual-edit/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/manual-edit.md](./contracts/manual-edit.md),
[quickstart.md](./quickstart.md)

**Tests**: OBRIGATÓRIOS (constituição, Princípio III). Escrever, rodar com
`dotnet test --solution PokemonEvTrainer.slnx`, ver falhar, então implementar.

**Organization**: US1 editar EVs (P1), US2 IVs conhecidos (P2), US3 editar anotação de nível (P3).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)
- **[Story]**: US1, US2, US3

## Convenções

- Mesmas das features anteriores: dotnet CLI primeiro, `dotnet format PokemonEvTrainer.slnx` após
  cada tarefa com código, domínio sem Blazor/IO, **todo texto novo em**
  `src/EvTrainer.Web/Localization/strings.pt-BR.json` (Princípio VII).
- Vetores `[HP, Ataque, Defesa, SpAtk, SpDef, Speed]`.
- Testes de componentes herdam `tests/EvTrainer.Tests/Support/AppTestContext.cs`.
- Edições em `TrainerStore.cs`, `strings.pt-BR.json` e `app.css` são sequenciais entre si.

---

## Phase 1: Setup

- [X] T001 Confirmar a linha de base: `dotnet format PokemonEvTrainer.slnx --verify-no-changes` e `dotnet test --solution PokemonEvTrainer.slnx` verdes antes de começar (277 testes)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: campos novos no modelo e no formato salvo/backup, usados por US1 e US2.

### Testes (escrever primeiro, ver falhar)

- [X] T002 Ampliar `tests/EvTrainer.Tests/Domain/ModelJsonTests.cs`: evento `{ "o": "", "n": 1, "m": 1, "set": [12, 0, 2, 26, 0, 4] }` é lido com `SetTotals == [12,0,2,26,0,4]` e `IsManualAdjustment == true`; evento sem `set` ⇒ `SetTotals == null`; `set` com tamanho ≠ 6 completa/trunca para 6 e cada valor é limitado a 0–255; `knownIvs: [null, null, null, null, null, 30]` ⇒ `KnownIvs[5] == 30`, os demais `null`; `knownIvs` ausente ⇒ 6 `null`; valores fora de 0–31 ⇒ `null`; escrita sempre grava `knownIvs` (6 posições) e grava `set` apenas nos ajustes; ida e volta (`Serialize(Deserialize(x)) == x`) com ajustes e IVs informados

### Implementação

- [X] T003 Em `src/EvTrainer.Web/Domain/Model/TrainingPokemon.cs`: `BattleEvent.SetTotals` (`int[]?`, "presente ⇒ é um ajuste manual; cada valor 0–255, soma ≤ 510"), `IsManualAdjustment => SetTotals is not null`, `Clone` copiando o array; `TrainingPokemon.KnownIvs` (`int?[]`, 6 posições, "cada valor null ou 0–31"), incluído no `Clone`
- [X] T004 Em `src/EvTrainer.Web/Domain/Json/PokemonJson.cs`: `EventDto.SetTotals` (`[JsonPropertyName("set")]`, ignorado na escrita quando `null` via `[JsonIgnore(Condition = WhenWritingNull)]`), `PokemonDto.KnownIvs` (`int?[]?`, `knownIvs`); mapeamento tolerante descrito em [contracts/manual-edit.md](./contracts/manual-edit.md) — faz T002 passar

**Checkpoint**: dados novos salvos e lidos; nada muda na tela.

---

## Phase 3: User Story 1 - Ajustar os EVs para os valores reais (Priority: P1) 🎯 MVP

**Goal**: "Editar EVs" com "Manter histórico" (ajuste no histórico) ou "Apagar histórico" (só após confirmação), sempre com Desfazer.

**Independent Test**: EVs `[10,0,2,20,0,4]` → editar para `[12,0,2,26,0,4]` mantendo histórico ⇒ totais exatos, item "Ajuste manual", batalhas seguintes somam a partir dele.

### Tests for User Story 1 ⚠️

- [X] T005 [P] [US1] Teste `tests/EvTrainer.Tests/Domain/ManualAdjustmentTests.cs`: `Caterpie ×10` + ajuste `[12,0,2,26,0,4]` ⇒ `Totals == [12,0,2,26,0,4]`; ajuste seguido de `Caterpie ×1` ⇒ HP 13; ajuste `[0,0,0,95,0,0]` seguido de `Calcium` ⇒ SpAtk 100 (regra da vitamina vale depois do ajuste); ajuste com soma 510 seguido de batalha ⇒ não soma; dois ajustes ⇒ vale o último; remover eventos anteriores ao ajuste não muda os totais; `CountFor`, `CountsByOpponent` e `Recent` ignoram ajustes; `BattleLog.Record` com o último evento sendo ajuste cria evento novo (não agrupa)
- [X] T006 [P] [US1] Ampliar `tests/EvTrainer.Tests/State/TrainerStoreTests.cs`: `AdjustEvs(targets)` acrescenta um ajuste no fim e grava; com `targets` iguais aos totais atuais não grava nada e retorna `null`; o Desfazer retornado remove o ajuste; `ResetHistory(targets)` deixa `Events` com um único ajuste `targets` e grava; o Desfazer restaura a lista anterior inteira (mesma ordem e contagens); anotações de nível não são tocadas
- [X] T007 [P] [US1] Teste `tests/EvTrainer.Tests/Components/Evs/EvEditorTests.cs` (bUnit): botão `Evs.Edit` no `EvSummary` abre o editor com 6 campos `inputmode="numeric"` preenchidos com os totais; soma ao vivo; valor > 255, negativo, não numérico ou soma > 510 desabilita **Salvar** com a mensagem `EvEdit.Invalid`; "Manter histórico" vem marcado; salvar ⇒ totais exatos, toast `EvEdit.Adjusted` com Desfazer; valores iguais ⇒ toast `EvEdit.NoChange`; escolher "Apagar histórico" e salvar ⇒ aparece `[role=alertdialog]` com `EvEdit.Confirm.Message` contendo o número de batalhas, botão **Exportar backup** (chama `IBrowserApi.DownloadText`), **Apagar N batalhas** e **Cancelar**; Cancelar ⇒ nada muda; confirmar ⇒ histórico com um único ajuste e toast `EvEdit.HistoryErased` com Desfazer que restaura tudo
- [X] T008 [P] [US1] Ampliar `tests/EvTrainer.Tests/Components/Evs/RecentHistoryTests.cs`: ajuste aparece como `History.Adjustment` com os 6 valores (`12/0/2/26/0/4`); o × remove com Desfazer e os totais voltam ao cálculo pelas batalhas

### Implementation for User Story 1

- [X] T009 [US1] Em `src/EvTrainer.Web/Domain/Evs.cs`: `Totals` trata ajuste (`ev = copy(SetTotals)`, `sum = ev.Sum()`, segue o laço); `CountFor`, `CountsByOpponent` e `Recent` pulam ajustes — faz T005 (parte de domínio) passar
- [X] T010 [US1] Em `src/EvTrainer.Web/Domain/BattleLog.cs`: `Record` nunca agrupa no último evento quando ele é ajuste — completa T005
- [X] T011 [US1] Em `src/EvTrainer.Web/State/TrainerStore.cs`: `Action? AdjustEvs(int[] targets)` e `Action? ResetHistory(int[] targets)` (validando "0–255, soma ≤ 510"), gravação imediata e Desfazer — faz T006 passar
- [X] T012 [US1] Criar `src/EvTrainer.Web/Components/Evs/EvEditor.razor` (formulário, opção de histórico com rádio, etapa de confirmação `role="alertdialog"` com exportar backup via `IBrowserApi`) e o botão **Editar EVs** em `src/EvTrainer.Web/Components/Evs/EvSummary.razor`; chaves novas em `strings.pt-BR.json` (`Evs.Edit`, `EvEdit.Title`, `EvEdit.KeepHistory`, `EvEdit.EraseHistory`, `EvEdit.Save`, `EvEdit.Cancel`, `EvEdit.Invalid`, `EvEdit.Sum`, `EvEdit.Adjusted`, `EvEdit.NoChange`, `EvEdit.Confirm.Message`, `EvEdit.Confirm.Erase`, `EvEdit.Confirm.Backup`, `EvEdit.HistoryErased`) — faz T007 passar
- [X] T013 [US1] Em `src/EvTrainer.Web/Components/Evs/RecentHistory.razor`: exibir ajuste com `History.Adjustment` e os valores; remoção pelo mesmo `RemoveEvent` — faz T008 passar
- [X] T014 [US1] Estilos do editor e da confirmação em `src/EvTrainer.Web/wwwroot/css/app.css` (campos 6 colunas como as metas, caixa de aviso com `--bad`, botões ≥ 44 px)
- [X] T015 [US1] Rodar `dotnet format` e `dotnet test`; validar [quickstart.md](./quickstart.md) passos 1 e 2

**Checkpoint**: MVP — EVs corrigíveis sem perder histórico; apagar só com confirmação.

---

## Phase 4: User Story 2 - Informar IVs conhecidos (Priority: P2)

**Goal**: IVs informados prevalecem na linha Final, com aviso de conflito.

**Independent Test**: Speed = 30 ⇒ Final "30" informado; Speed = 10 com anotações 26–30 ⇒ aviso.

### Tests for User Story 2 ⚠️

- [X] T016 [P] [US2] Teste `tests/EvTrainer.Tests/Domain/KnownIvsTests.cs`: com as 6 anotações do caso de interseção (HANDOFF §6) e `KnownIvs[5] = 30` ⇒ `Final[5] == FinalKnown(30, MatchesNotes: true)`; `KnownIvs[5] = 10` ⇒ `FinalKnown(10, false)`; sem anotações e `KnownIvs[0] = 5` ⇒ `FinalKnown(5, true)`; stat com "conferir" e valor informado ⇒ `MatchesNotes == false`; stats sem valor informado continuam como antes
- [X] T017 [P] [US2] Teste `tests/EvTrainer.Tests/Components/Ivs/KnownIvsPanelTests.cs` (bUnit): seção recolhível `KnownIvs.Title` com 6 campos (vazio = não sei), 0–31; valor fora da faixa desabilita salvar; salvar grava `KnownIvs` e toast com Desfazer; a `IvTable` mostra na linha Final o valor com a marca `Ivs.Final.Known` e, quando não bate, a classe `bad` e o texto `Ivs.Final.KnownMismatch`; apagar o campo volta ao cálculo

### Implementation for User Story 2

- [X] T018 [US2] Em `src/EvTrainer.Web/Domain/Ivs.cs`: novo caso `public sealed record FinalKnown(int Value, bool MatchesNotes) : FinalCell;` e `Final` aplicando `KnownIvs` — faz T016 passar; atualizar todo `switch` sobre `FinalCell` apontado pelo compilador (`IvTable.razor`, `tests/EvTrainer.Tests/Domain/IvTests.cs`)
- [X] T019 [US2] Em `src/EvTrainer.Web/State/TrainerStore.cs`: `Action? SetKnownIvs(int?[] ivs)` ("null ou 0–31") com Desfazer
- [X] T020 [US2] Criar `src/EvTrainer.Web/Components/Ivs/KnownIvsPanel.razor`, incluí-lo na aba IVs em `src/EvTrainer.Web/Pages/Home.razor` e exibir `FinalKnown` em `src/EvTrainer.Web/Components/Ivs/IvTable.razor`; chaves `KnownIvs.Title`, `KnownIvs.Hint`, `KnownIvs.Save`, `KnownIvs.Saved`, `Ivs.Final.Known`, `Ivs.Final.KnownMismatch` — faz T017 passar
- [X] T021 [US2] Rodar `dotnet format` e `dotnet test`; validar [quickstart.md](./quickstart.md) passo 3

**Checkpoint**: IVs informados funcionando.

---

## Phase 5: User Story 3 - Editar uma anotação de nível (Priority: P3)

**Goal**: editar no lugar em vez de remover e anotar de novo.

**Independent Test**: editar HP da anotação do nível 21 ⇒ linha e Final recalculadas; Desfazer restaura.

### Tests for User Story 3 ⚠️

- [X] T022 [P] [US3] Ampliar `tests/EvTrainer.Tests/State/TrainerStoreTests.cs`: `ReplaceLevel(index, entry)` substitui, reordena por nível quando o nível muda, grava e o Desfazer restaura a anotação original na posição original
- [X] T023 [P] [US3] Criar `tests/EvTrainer.Tests/Components/Ivs/EditLevelTests.cs` (renderiza `IvTab`): o rótulo de nível de cada linha é o botão de editar (`aria-label` `Ivs.EditLabel` com o nível, dica `Ivs.EditHint`; sem coluna extra para caber em 360 px); tocar abre o formulário em modo edição preenchido com nível, espécie, stats e **EVs da anotação** (não os atuais), botão `Ivs.Form.SaveChanges` e `Ivs.Form.CancelEdit`; salvar substitui a anotação e mostra toast `Ivs.Updated` com Desfazer; cancelar volta ao modo de nova anotação sem mudar nada

### Implementation for User Story 3

- [X] T024 [US3] Em `src/EvTrainer.Web/State/TrainerStore.cs`: `Action? ReplaceLevel(int index, LevelEntry entry)` — faz T022 passar
- [X] T025 [US3] Criar `src/EvTrainer.Web/Components/Ivs/IvTab.razor` (contém `IvTable`, `KnownIvsPanel` e `LevelForm`, guarda o índice em edição), usar em `Pages/Home.razor` no lugar dos três componentes; `IvTable` ganha `EventCallback<int> OnEdit`; `LevelForm` ganha parâmetros `EditIndex` (`int?`) e `OnDone` com o modo edição; chaves `Ivs.EditLabel`, `Ivs.EditHint`, `Ivs.Form.EditTitle`, `Ivs.Form.SaveChanges`, `Ivs.Form.CancelEdit`, `Ivs.Updated` — faz T023 passar
- [X] T026 [US3] Rodar `dotnet format` e `dotnet test`; validar [quickstart.md](./quickstart.md) passo 4

**Checkpoint**: todas as histórias funcionando.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T027 Ampliar `tests/EvTrainer.Tests/Localization/NoHardcodedTextTests.cs` para abrir o editor de EVs (incluindo a etapa de confirmação), o painel de IVs conhecidos e o modo de edição de anotação, e verificar ausência de texto fixo
- [X] T028 [P] Ampliar `tests/EvTrainer.Tests/Domain/BackupFormatTests.cs`: backup com ajustes e IVs informados faz ida e volta idêntica (SC-004); `LegacyExportGoldenTests` continuam passando (arquivo antigo sem os campos novos)
- [X] T029 Validar no app em 360 px, temas claro e escuro, [quickstart.md](./quickstart.md) passos 1–5; confirmar que nenhum caminho apaga histórico sem o segundo toque (SC-002)
- [X] T030 [P] Atualizar `README.md` (Features): edição manual de EVs (mantendo ou apagando o histórico, com confirmação), IVs conhecidos e edição de anotação
- [X] T031 Rodar `dotnet format PokemonEvTrainer.slnx --verify-no-changes` e `dotnet test --solution PokemonEvTrainer.slnx` (tudo verde) como portão final

---

## Dependencies & Execution Order

- **Setup (T001)** → **Fundação (T002–T004)** → US1, US2 e US3 → Polish.
- US1 e US2 dependem da Fundação (campos novos); US3 não depende dos campos novos, mas edita
  `TrainerStore.cs` e `Home.razor` — fazer depois de US2 para evitar conflito.
- Dentro de cada história: testes falhando → domínio → store → componentes → format/test.
- T018 obriga a atualizar `switch`es de `FinalCell` (hierarquia `closed`): o build aponta onde.

### Parallel Opportunities

- US1: T005, T006, T007, T008 (arquivos diferentes). US2: T016 e T017. US3: T022 e T023.
- T028 e T030 a qualquer momento após as histórias.

---

## Parallel Example: User Story 1

```bash
Task: "Regras do ajuste em tests/EvTrainer.Tests/Domain/ManualAdjustmentTests.cs"
Task: "Comandos AdjustEvs/ResetHistory em tests/EvTrainer.Tests/State/TrainerStoreTests.cs"
Task: "Editor e confirmação em tests/EvTrainer.Tests/Components/Evs/EvEditorTests.cs"
Task: "Item de ajuste no histórico em tests/EvTrainer.Tests/Components/Evs/RecentHistoryTests.cs"
```

---

## Implementation Strategy

### MVP First

1. T001–T004 (base e dados) → T005–T015 (US1).
2. **Validar**: corrigir EVs mantendo histórico; apagar só com confirmação.

### Incremental Delivery

1. Fundação + US1 → correção de EVs.
2. US2 → IVs conhecidos.
3. US3 → edição de anotação.
4. Polish → texto fixo, backup, validação no app, README.

---

## Notes

- Commits só quando o usuário pedir; mensagens sem menção a ferramentas de IA.
- "Manter histórico" é sempre o padrão; nenhum caminho apaga sem o segundo toque.
