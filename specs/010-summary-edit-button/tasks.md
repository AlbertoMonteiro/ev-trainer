# Tasks: Botão Editar EVs no cabeçalho do resumo

**Input**: Design documents from `/specs/010-summary-edit-button/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ev-summary.md, quickstart.md

**Tests**: obrigatórios (Princípio III — TDD): cada teste é escrito e visto falhar antes do código.

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Setup

Nenhuma tarefa: sem pacotes, projetos ou configuração novos.

## Phase 2: Foundational

Nenhuma tarefa: nenhuma regra de domínio nem dado muda.

## Phase 3: User Story 1 — Cabeçalho mais limpo, com o botão no lugar das metas (P1) 🎯 MVP

**Independent Test**: Pokémon com EVs e metas → cabeçalho mostra só "atual / 510" e o botão "Editar
EVs"; nenhum botão abaixo da grade; o botão some com o editor aberto e volta ao fechar.

- [X] T001 [US1] Em `tests/EvTrainer.Tests/Components/Evs/EvSummaryTests.cs`, atualizar `Shows_the_sprite_as_title_with_totals_and_six_mini_cards`: Squirtle com eventos somando 12 e metas `[252, 0, 2, 252, 0, 4]` → `.card-head .totals` com texto exatamente "12 / 510" (literal, não via `T(...)`, para travar o formato sem metas) e `Assert.DoesNotContain("·", …)` (nenhum sufixo de metas)
- [X] T002 [US1] Em `tests/EvTrainer.Tests/Components/Evs/EvSummaryTests.cs`, novo teste `The_edit_button_sits_in_the_header_next_to_the_totals`: exatamente um `button.edit-evs` no cartão; ele é filho de `.card-head` e vem depois de `span.totals`; texto `T("Evs.Edit")`; nenhum `button.edit-evs` fora de `.card-head`
- [X] T003 [US1] Em `tests/EvTrainer.Tests/Components/Evs/EvSummaryTests.cs`, novo teste `The_edit_button_hides_while_the_editor_is_open_and_comes_back`: clicar `button.edit-evs` → `.ev-editor` presente depois de `.summary-grid` e nenhum `button.edit-evs` no cartão; cancelar o editor (botão de cancelar do `EvEditor`) → `.ev-editor` ausente e `button.edit-evs` de volta em `.card-head`
- [X] T004 [US1] Rodar `dotnet test --solution PokemonEvTrainer.slnx --filter-class "*EvSummaryTests"` (ou filtro equivalente) e ver T001–T003 falharem
- [X] T005 [US1] Em `src/EvTrainer.Web/Localization/strings.pt-BR.json`, mudar `Evs.Summary.Totals` de `"{0} / 510 · metas {1}"` para `"{0} / 510"` (chave inalterada; `Evs.Edit` inalterado)
- [X] T006 [US1] Em `src/EvTrainer.Web/Components/Evs/EvSummary.razor`: chamar `L["Evs.Summary.Totals", totals.Sum()]` (sem a soma das metas); mover `<button type="button" class="edit-evs" …>@L["Evs.Edit"]</button>` para dentro de `.card-head`, depois de `span.totals`, renderizado só quando `!_editing`; abaixo de `.summary-grid` fica só `@if (_editing) { <EvEditor OnClose="() => _editing = false" /> }` (contracts/ev-summary.md)
- [X] T007 [P] [US1] Em `src/EvTrainer.Web/wwwroot/css/app.css`, regra `.summary .edit-evs` (seção "Manual EV edit"): remover `margin-top: 10px` e `width: 100%`; manter `min-height: 44px`; adicionar `padding: 0 12px` e `white-space: nowrap`. Adicionar `.summary .totals { margin-left: auto; white-space: nowrap; }` junto a `.summary .card-head` (research.md R4)
- [X] T008 [US1] Rodar os testes de `EvSummaryTests`, `EvEditorTests` e `NoHardcodedTextTests` e ver tudo verde (os dois últimos usam `button.edit-evs` e devem passar sem mudança)

## Phase 4: Polish

- [X] T009 `dotnet build --no-incremental`, `dotnet test --solution PokemonEvTrainer.slnx`, `dotnet format --verify-no-changes`
- [X] T010 Validação no navegador conforme `quickstart.md` (320, 360 e 1366 px, claro/escuro): cabeçalho em uma linha, sem sobreposição nem rolagem horizontal; se não couber a 320 px, ajustar só o padding do botão em `src/EvTrainer.Web/wwwroot/css/app.css`
- [X] T011 [P] Conferir `README.md` (bullet "Fix it by hand when the game disagrees") e ajustar se descrever a posição do botão ou o texto "metas"

## Dependencies

- T001–T003 (mesmo arquivo, sequenciais) → T004 → T005 → T006 → T008; T007 pode ser feita junto com T005/T006 (arquivo diferente) → Polish (T009 → T010; T011 paralela).

## Parallel Example

```text
T006 EvSummary.razor   |  T007 app.css
```

## Implementation Strategy

MVP = US1, que é a feature inteira: um único incremento, entregue após o Polish.
