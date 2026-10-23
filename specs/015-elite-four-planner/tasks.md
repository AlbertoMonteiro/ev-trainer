# Tasks: Planejador da Elite Four

**Input**: Design documents from `/specs/015-elite-four-planner/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: obrigatórios (Princípio III — TDD): cada teste é escrito e visto falhar antes do código.

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Setup (dados)

- [X] T001 Criar `scripts/BuildLeague.cs` (contracts/league-data.md, research R1–R4): golpes, espécies (tipos,
  habilidades, aprendizado por nível/TM/HM/tutor), times da Liga (primeira luta, revanche, Campeão por inicial do
  jogador) com habilidade = primeira da espécie
- [X] T002 Rodar o gerador, versionar `league.json`, embutir no `src/EvTrainer.Web/EvTrainer.Web.csproj`; conferir à
  mão Lorelei (primeira/revanche), Campeão Bulbasaur → Charizard, Gengar Levitate, aprendizado do Vileplume

## Phase 2: Foundational

- [X] T003 [P] Testes `tests/EvTrainer.Tests/Domain/TypeChartTests.cs`: Electric × Water/Flying = 4; Ground × Flying =
  0; Ghost × Normal = 0; Fire × Water/Rock = ¼; Ice × Dragon/Flying = 4; Steel × Ice = 2; categoria física/especial
- [X] T004 `src/EvTrainer.Web/Domain/TypeChart.cs`. T003 verde
- [X] T005 [P] Testes `tests/EvTrainer.Tests/Domain/LeagueDataTests.cs` (contra `League.LoadEmbedded()`): Lorelei
  primeira (5 Pokémon, Dewgong nv 52 Thick Fat, golpes), revanche (Piloswine nv 63), Campeão por inicial, Lapras com
  Sitrus Berry, todos os golpes dos times existem em `Moves`, toda espécie do Pokédex existe com ≥ 1 tipo e habilidade,
  Vileplume aprende Sludge Bomb (TM36) e Solar Beam, Venusaur aprende Frenzy Plant (tutor)
- [X] T006 `src/EvTrainer.Web/Domain/Reference/League.cs` (`LoadEmbedded`, `Team(trainer, round, starter)`,
  `Opponents(round, starter)`, `Species(name)`, `Move(name)`); registrar no `Program.cs` e no `AppTestContext`. T005
  verde
- [X] T007 [P] Testes `tests/EvTrainer.Tests/Domain/LeaguePlanJsonTests.cs`: ida e volta; padrões; espécie
  desconhecida descartada; golpe não aprendível descartado; habilidade inválida → primeira; `locked` ⊆ `moves`;
  máximo 6 membros e 4 golpes
- [X] T008 `src/EvTrainer.Web/Domain/Model/LeaguePlan.cs` e `src/EvTrainer.Web/Domain/Json/LeaguePlanJson.cs`. T007
  verde

## Phase 3: User Story 1 — Times da Liga (P1) 🎯 MVP

- [X] T009 [P] [US1] Testes `tests/EvTrainer.Tests/Components/League/LeaguePlannerViewTests.cs`: menu abre a página;
  primeira luta mostra Lorelei→Campeão em ordem com nível, tipos, habilidade, item, golpes; revanche troca os times;
  inicial troca o Campeão; escolhas persistem
- [X] T010 [US1] `TrainerStore`: `Plan`, `SetPlanRound`, `SetPlanStarter` (carimbam, salvam, `LocalDataChanged`);
  repositório `LoadPlan`/`SavePlan` (`evtrainer:v1:plan`) em `IPokemonRepository`, `LocalStorageRepository`,
  `InMemoryRepository`
- [X] T011 [US1] `Components/League/LeaguePlannerView.razor` + `LeagueOpponentCard.razor`, item "Planejar a Elite Four"
  no `PokemonMenu`/`AppHeader`/`Home`, textos pt/en, CSS. T009 verde

## Phase 4: User Story 2 — Equipe e cobertura (P1)

- [X] T012 [P] [US2] Testes `tests/EvTrainer.Tests/Domain/LeaguePlannerTests.cs`: efetividade com habilidades
  (Levitate, Water Absorb, Volt Absorb, Flash Fire, Thick Fat, Wonder Guard), dano fixo, melhor golpe por adversário,
  classes e resumo
- [X] T013 [US2] `src/EvTrainer.Web/Domain/LeaguePlanner.cs`: `Effectiveness`, `Coverage`, `Summary`. T012 verde
- [X] T014 [P] [US2] Testes bUnit: adicionar membro (busca de espécie), escolher habilidade e golpes (só aprendíveis,
  com origem), remover; marcas "sem super efetivo"/"sem resposta"; resumo; trocar espécie descarta golpes; persistência
- [X] T015 [US2] `TrainerStore`: `AddPlanMember`, `RemovePlanMember`, `SetPlanSpecies`, `SetPlanAbility`,
  `SetPlanMove`, `TogglePlanLock`; `Components/League/PlanMemberCard.razor`; cobertura nos cards. T014 verde

## Phase 5: User Story 3 — Ameaças (P2)

- [X] T016 [P] [US3] Testes de domínio (`Threats`, `Walls`) e bUnit (ameaças por adversário e por membro)
- [X] T017 [US3] `LeaguePlanner.Threats/Walls` e exibição. T016 verde

## Phase 6: User Story 4 — Sugestão (P2)

- [X] T018 [P] [US4] Testes de domínio da sugestão (só aprendíveis, respeita fixados, cobre a Liga quando possível,
  determinística) e bUnit (botões por membro e da equipe)
- [X] T019 [US4] `LeaguePlanner.Suggest` e botões. T018 verde

## Phase 7: Sincronização

- [X] T020 [P] Testes `SyncMergeTests` (plano: mais recente vence, empate, ausente × presente, sem mudança),
  `ModelJsonTests`/`PokemonJson` (seção `plan` lida e escrita), `SyncServiceTests` (plano vai e volta; mudança no plano
  agenda sync), `TrainerStore` (`ApplySync` aplica o plano sem carimbar)
- [X] T021 `SyncSnapshot`/`MergeResult` com plano, `SyncMerge`, `PokemonJson`, `SyncService`, `TrainerStore`. T020
  verde

## Phase 8: Polish

- [X] T022 [P] `NoHardcodedTextTests`/`EnglishScreensTests`/`KeyParityTests` cobrindo a página
- [X] T023 README (feature e gerador), `dotnet format`, testes, build; conferência no navegador (quickstart)
