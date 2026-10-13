# Implementation Plan: Versão do jogo (FireRed / LeafGreen)

**Branch**: `014-game-version` | **Date**: 2026-10-13 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/014-game-version/spec.md`

## Summary

`scripts/BuildLocations.cs` passa a gravar os selvagens de cada local **por versão** (`"wild": {"fireRed": […],
"leafGreen": […]}`) a partir das tabelas `_FireRed` e `_LeafGreen` de `wild_encounters.json`. `ReferenceData` expõe
`WildPlaces(GameVersion)`. `OpponentSearch` monta um índice por versão e `Find(query, version)` usa o da versão
escolhida; para Pokémon (não vitaminas) a anotação curada `wildLocation` sai do índice, substituída pelos dados do jogo
por versão (as notas curadas eram de FireRed). Os treinadores continuam iguais nas duas versões. Nova preferência do
aparelho `UiPreferences.GameVersion` (`"fireRed"` padrão | `"leafGreen"`), trocada no menu (grupo "Versão do jogo",
como Tema/Idioma); a aba EVs passa a versão à busca.

## Technical Context

**Language/Version**: C# 15 / .NET 11 (SDK RC fixado no `global.json`)

**Primary Dependencies**: nenhuma nova.

**Storage**: `evtrainer:v1:prefs.gameVersion` (aparelho); backup/sync inalterados. `locations.json` regenerado.

**Testing**: xUnit v3 — índice por versão (Safari: Scyther FR × Pinsir LG; exclusivos de cada versão; treinadores
iguais), preferência (padrão, ida e volta, valor inválido → fireRed); bUnit — menu com as duas opções, troca atualiza a
busca aberta mantendo o texto; testes de texto fixo/inglês cobrindo o menu.

**Target Platform**: browsers de celular e desktop (320–2560 px).

**Project Type**: SPA estática PWA no GitHub Pages, 2 projetos (inalterado).

**Performance Goals**: dois índices de busca construídos na inicialização (≈ 2× o atual, centenas de entradas): sem
impacto perceptível.

**Constraints**: offline; textos via recursos; dados reproduzíveis por script.

**Scale/Scope**: gerador (selvagens por versão), `ReferenceData`, `OpponentSearch`, `UiPreferences`/`TrainerStore`,
`PokemonMenu`, `EvTab`, 2–3 chaves de texto por idioma.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.* Constituição v1.3.0.

| Princípio / Seção | Verificação | Pré | Pós |
|---|---|---|---|
| I. Estático no browser | Dados embutidos. | ✅ | ✅ |
| II. Dois projetos | Nada novo; regra no domínio. | ✅ | ✅ |
| III. Test-First | Testes de domínio e bUnit antes do código; testes que dependiam da anotação curada são ajustados à regra nova. | ✅ | ✅ |
| IV. Fidelidade Gen 3 | Tabelas de encontro oficiais de cada versão (decompilação). | ✅ | ✅ |
| V. Dados locais | Só uma preferência nova no aparelho. | ✅ | ✅ |
| VI. Mobile-first, acessível | Escolha no menu (2 toques), botões com `aria-pressed`. | ✅ | ✅ |
| VII. Outros idiomas | Rótulo do grupo via recursos; nomes das versões iguais nos dois idiomas. | ✅ | ✅ |
| Restrições / dotnet CLI primeiro | Gerador por `dotnet run scripts/BuildLocations.cs`. | ✅ | ✅ |

Resultado: **sem violações**.

## Project Structure

### Documentation (this feature)

```text
specs/014-game-version/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── game-version.md
├── checklists/
│   └── requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
scripts/BuildLocations.cs                       # wild por versão
locations.json                                  # regenerado
src/EvTrainer.Web/
├── Domain/GameVersion.cs                       # novo: enum FireRed | LeafGreen + parse/format da preferência
├── Domain/Reference/ReferenceData.cs           # WildPlaces(GameVersion)
├── Domain/OpponentSearch.cs                    # índice por versão; Find(query, version); sem wildLocation curado p/ Pokémon
├── Domain/Model/TrainingPokemon.cs             # UiPreferences.GameVersion
├── Domain/Json/PokemonJson.cs                  # normaliza valor inválido
├── State/TrainerStore.cs                       # SetGameVersion
├── Components/Shell/PokemonMenu.razor          # grupo "Versão do jogo"
├── Components/Evs/EvTab.razor                  # passa a versão
└── Localization/strings.*.json                 # Menu.GameVersion, Version.FireRed/LeafGreen
tests/EvTrainer.Tests/
├── Domain/SearchTests.cs                       # por versão
├── Domain/ReferenceDataTests.cs / LocationNamesTests.cs
├── Domain/ModelJsonTests.cs, State/TrainerStoreTests.cs
├── Components/Shell/PokemonMenuTests.cs, Components/Evs/OpponentSearchTests.cs
└── Localization/*
```

**Structure Decision**: mesma solução; dado por versão gerado pelo script; regra de filtragem no domínio.

## Complexity Tracking

Sem violações da constituição a justificar.
