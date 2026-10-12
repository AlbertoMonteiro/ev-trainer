# Tasks: Busca com úteis primeiro

**Input**: Design documents from `/specs/009-useful-first-search/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ev-lists.md, quickstart.md

**Tests**: obrigatórios (Princípio III — TDD): cada teste é escrito e visto falhar antes do código.

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Setup

Nenhuma tarefa: sem pacotes, projetos ou configuração novos.

## Phase 2: Foundational

- [X] T001 Escrever `tests/EvTrainer.Tests/Domain/UsefulFirstTests.cs`: partição estável (ordem mantida em cada grupo), cada item em exatamente um grupo, `TrainerLocations` preservados, metas zero/completas → `Useful` vazia, vitaminas seguem `IsUseful` (data-model.md)
- [X] T002 Implementar `Evs.UsefulFirst(IEnumerable<SearchHit>, int[] totals, int[] targets)` → `(IReadOnlyList<SearchHit> Useful, IReadOnlyList<SearchHit> Others)` em `src/EvTrainer.Web/Domain/Evs.cs` (T001 verde)

## Phase 3: User Story 1 — Resultados da busca com os úteis primeiro (P1) 🎯 MVP

**Independent Test**: busca de rota com úteis e não úteis → todos os úteis antes, cada grupo com título.

- [X] T003 [US1] Testes bUnit em `tests/EvTrainer.Tests/Components/Evs/OpponentSearchTests.cs`: busca "rota 4" com metas parciais → grades "Úteis para as metas" e "Outros resultados" na ordem do contrato, ordem da busca em cada grupo, local do treinador preservado; nenhum útil → só "Resultados"; todos úteis → só "Úteis para as metas"; sem resultado → "Resultados" + `Empty.NoResults`
- [X] T004 [US1] Teste em `tests/EvTrainer.Tests/Components/Evs/EvTabFlowTests.cs`: com a busca aberta, completar a meta move o adversário para "Outros resultados"; desfazer devolve (FR-006)
- [X] T005 [US1] Adicionar `Evs.List.OtherResults` = "Outros resultados" e `Evs.List.Others` = "Outros adversários" em `src/EvTrainer.Web/Localization/strings.pt-BR.json`
- [X] T006 [US1] `src/EvTrainer.Web/Components/Evs/EvTab.razor`: `.main-list` renderiza 1 ou 2 `OpponentGrid` conforme `contracts/ev-lists.md` (sem `@for`, por causa do AD0001)

## Phase 4: User Story 2 — O filtro não finge estar ativo durante a busca (P2)

**Independent Test**: digitar busca → controle desativado e nada marcado; apagar → escolha anterior.

- [X] T007 [US2] Testes bUnit em `tests/EvTrainer.Tests/Components/Evs/OpponentSearchTests.cs`: com busca, `button.useful` e `button.all` com `disabled` e `aria-pressed="false"`, preferência salva intacta; apagar a busca restaura "Úteis" ou "Todos"
- [X] T008 [US2] Parâmetro `Searching` em `src/EvTrainer.Web/Components/Evs/OpponentSearchBar.razor`, passado pela `EvTab`
- [X] T009 [P] [US2] CSS `.segmented button:disabled` (opacidade reduzida, cursor padrão) em `src/EvTrainer.Web/wwwroot/css/app.css`

## Phase 5: User Story 3 — "Todos" também traz os úteis primeiro (P3)

**Independent Test**: filtro Todos sem busca → úteis antes de qualquer não útil.

- [X] T010 [US3] Atualizar `Useful_is_the_default_and_the_choice_is_remembered` e `All_lists_the_386_species_in_dex_order_then_the_vitamins` em `tests/EvTrainer.Tests/Components/Evs/OpponentSearchTests.cs` para a nova regra (títulos `Evs.List.Useful` + `Evs.List.Others`; 392 cartões no total; cada grupo em ordem da Pokédex com vitaminas no fim do grupo); caso sem nenhum útil → título `Evs.List.All`
- [X] T011 [US3] `EvTab` aplica a partição também em "Todos" sem busca (mesmo código da T006)

## Phase 6: Polish

- [X] T012 [P] `tests/EvTrainer.Tests/Localization/NoHardcodedTextTests.cs`: busca agrupada e controle desativado sem texto fixo
- [X] T013 `dotnet build --no-incremental`, `dotnet test`, `dotnet format --verify-no-changes`
- [X] T014 Validação no navegador conforme `quickstart.md` (360 e 1366 px, claro/escuro) e README (bullet "Find the right opponent fast")

## Dependencies

- T001 → T002 → US1 (T003–T006) → US2 (T007–T009) e US3 (T010–T011) → Polish.
- US2 e US3 tocam arquivos diferentes do domínio, mas ambas editam `OpponentSearchTests.cs`/`EvTab.razor`: executar em sequência.

## Implementation Strategy

MVP = US1 (busca com úteis primeiro). US2 e US3 são incrementos pequenos sobre o mesmo render.
