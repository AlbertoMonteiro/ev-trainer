# Tasks: Versão do jogo (FireRed / LeafGreen)

**Input**: Design documents from `/specs/014-game-version/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/game-version.md, quickstart.md

**Tests**: obrigatórios (Princípio III — TDD): cada teste é escrito e visto falhar antes do código.

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Setup (dados)

- [X] T001 Em `scripts/BuildLocations.cs`, ler as tabelas `_FireRed` e `_LeafGreen` de `wild_encounters.json` e gravar por local `"wild": { "fireRed": [...], "leafGreen": [...] }` (listas ordenadas; local entra se tiver treinadores ou selvagens em alguma versão); atualizar o comentário de cabeçalho
- [X] T002 Rodar `dotnet run scripts/BuildLocations.cs` e versionar `locations.json`; conferir à mão: Safari Zone `fireRed` contém Scyther e não Pinsir, `leafGreen` o inverso; Rota 24 `fireRed` contém Oddish, `leafGreen` Bellsprout; `trainers` iguais ao arquivo anterior

## Phase 2: Foundational

- [X] T003 [P] Testes em `tests/EvTrainer.Tests/Domain/ReferenceDataTests.cs`: `Data.WildPlaces(GameVersion.FireRed)["Scyther"]` contém "Safari Zone" e `WildPlaces(GameVersion.LeafGreen)` não tem Scyther na Safari Zone; Pinsir o inverso; Tentacool na Rota 20 nas duas; Unown em "Monean Chamber" nas duas. Ajustar `tests/EvTrainer.Tests/Domain/LocationNamesTests.cs` para iterar os locais das duas versões
- [X] T004 Criar `src/EvTrainer.Web/Domain/GameVersion.cs`: `enum GameVersion { FireRed, LeafGreen }` e `static class GameVersions` com `Parse(string? value)` (`"leafGreen"` → LeafGreen; qualquer outro → FireRed) e `Key(GameVersion)` (`"fireRed"` | `"leafGreen"`); em `src/EvTrainer.Web/Domain/Reference/ReferenceData.cs` ler `wild.fireRed`/`wild.leafGreen` e trocar a propriedade por `WildPlaces(GameVersion version)`. T003 verde
- [X] T005 [P] Testes em `tests/EvTrainer.Tests/Domain/ModelJsonTests.cs` (`gameVersion`: ausente → "fireRed"; "leafGreen" → "leafGreen"; inválido → "fireRed") e `tests/EvTrainer.Tests/State/TrainerStoreTests.cs` (`SetGameVersion("leafGreen")` grava e notifica)
- [X] T006 `UiPreferences.GameVersion` (padrão "fireRed") em `src/EvTrainer.Web/Domain/Model/TrainingPokemon.cs`, normalização em `src/EvTrainer.Web/Domain/Json/PokemonJson.cs` (como `GuidePhase`), `SetGameVersion` em `src/EvTrainer.Web/State/TrainerStore.cs`. T005 verde

## Phase 3: User Story 1 — Escolher a versão do jogo (P1) 🎯 MVP

**Independent Test**: LeafGreen → "safari" traz Pinsir e não Scyther; FireRed → o inverso; treinadores iguais.

- [X] T007 [P] [US1] Em `tests/EvTrainer.Tests/Domain/SearchTests.cs`: `Find("safari zone", FireRed)` contém Scyther e não Pinsir; `LeafGreen` o inverso; `Find("rota 24", LeafGreen)` contém Bellsprout e não Oddish; nenhum dos 17 exclusivos do FireRed casa como selvagem em LeafGreen por local (para cada local de `WildPlaces(LeafGreen)`, os hits sem `TrainerLocations` estão em `WildPlaces(LeafGreen)` daquele local); `Find("rota 20", v)` tem os mesmos hits com `TrainerLocations` nas duas versões; vitaminas continuam casando por "celadon" nas duas; ajustar os testes existentes para chamar `Find(query, GameVersion.FireRed)` e corrigir os que dependiam de anotação curada de Pokémon
- [X] T008 [US1] Em `src/EvTrainer.Web/Domain/OpponentSearch.cs`: construir um índice por `GameVersion`; local selvagem de Pokémon = `data.WildPlaces(version)` (pt + en); anotação curada `WildLocation` só para vitaminas; `Find(string query, GameVersion version)`. T007 verde
- [X] T009 [US1] Em `src/EvTrainer.Web/Components/Evs/EvTab.razor`: `Search.Find(_query, GameVersions.Parse(Store.Preferences.GameVersion))`; ajustar chamadores nos testes (`tests/EvTrainer.Tests/Components/Evs/OpponentSearchTests.cs`) se necessário

## Phase 4: User Story 2 — Versão visível e fácil de trocar (P2)

**Independent Test**: com "safari" digitado, trocar a versão no menu: lista muda, texto continua.

- [X] T010 [P] [US2] Em `tests/EvTrainer.Tests/Components/Shell/PokemonMenuTests.cs`: grupo com `L["Menu.GameVersion"]`; `.choices.versions button[data-version]` = fireRed, leafGreen com textos `Version.FireRed`/`Version.LeafGreen`; `aria-pressed` na ativa; clicar leafGreen grava a preferência. Em `tests/EvTrainer.Tests/Components/Evs/OpponentSearchTests.cs` (ou `EvTabFlowTests.cs`): com "safari zone" digitado em FireRed (Scyther presente), trocar para LeafGreen pelo menu → Pinsir presente, Scyther ausente, `input.search` mantém o texto
- [X] T011 [US2] Adicionar `Menu.GameVersion`, `Version.FireRed`, `Version.LeafGreen` em `src/EvTrainer.Web/Localization/strings.pt-BR.json` e `strings.en.json`; grupo de versões em `src/EvTrainer.Web/Components/Shell/PokemonMenu.razor` abaixo de Tema (mesmo padrão `.choices`), chamando `Store.SetGameVersion`. T010 verde

## Phase 5: Polish

- [X] T012 [P] `tests/EvTrainer.Tests/Localization/NoHardcodedTextTests.cs` / `EnglishScreensTests.cs`: menu com o grupo de versões sem texto fixo e sem português em inglês ("FireRed"/"LeafGreen" como dados permitidos)
- [X] T013 `dotnet build PokemonEvTrainer.slnx --no-incremental` sem avisos, `dotnet test --solution PokemonEvTrainer.slnx`, `dotnet format PokemonEvTrainer.slnx --verify-no-changes`
- [X] T014 Validação no navegador conforme `quickstart.md` (320 px, pt/en)
- [X] T015 [P] `README.md`: mencionar FireRed e LeafGreen (escolha no menu; muda os selvagens da busca)

## Dependencies

- Setup (T001→T002) → Foundational (T003→T004; T005→T006) → US1 (T007→T008→T009) → US2 (T010→T011) → Polish.

## Parallel Example

```text
Foundational: T003 ReferenceDataTests  |  T005 ModelJsonTests/TrainerStoreTests
```

## Implementation Strategy

MVP = Setup + Foundational + US1 (busca correta por versão). US2 (escolha no menu) é necessária para o jogador trocar a
versão e entra no mesmo commit.
