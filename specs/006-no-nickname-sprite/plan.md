# Implementation Plan: Pokémon identificado pela espécie, sem apelido

**Branch**: `006-no-nickname-sprite` | **Date**: 2026-10-11 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/006-no-nickname-sprite/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

O apelido sai do app. Cada Pokémon passa a ter uma **forma atual** (`CurrentSpecies`), uma espécie da sua
linha evolutiva, que define a imagem e o nome mostrados em todo lugar. A forma atual é escolhida nos Ajustes
(busca, "Evolui para…" ou tocando num chip da linha) e avança sozinha quando uma anotação de nível traz uma
forma posterior. No resumo da aba EVs, o título em texto vira a imagem da forma atual (64 px) com rótulo
acessível. No JSON nada muda de formato: `"name"` passa a guardar a forma atual, e dados antigos são
normalizados na leitura por uma regra pura do domínio (R1, R2).

## Technical Context

**Language/Version**: C# 15 / .NET 11 (SDK RC fixado no `global.json`)

**Primary Dependencies**: nenhuma nova.

**Storage**: `localStorage`/backup no formato atual; `"name"` muda de significado (apelido → forma atual),
sem propriedade nova.

**Testing**: xUnit v3 + bUnit (TDD), projeto `EvTrainer.Tests` existente.

**Target Platform / Project Type**: iguais às features anteriores (SPA estática PWA no GitHub Pages,
2 projetos).

**Performance Goals**: evolução registrada em ≤ 2 toques e 0 caracteres (SC-001); recálculo instantâneo.

**Constraints**: dados e backups antigos abrem sem perda (SC-004); textos via recursos; sem JS novo; sem
rolagem horizontal em 360 px.

**Scale/Scope**: 1 entidade alterada (`TrainingPokemon`), 1 regra nova de domínio (`CurrentForm`),
1 comando alterado no store (`SaveSettings`) e 1 indiretamente (`InsertLevel`), 7 componentes alterados
(`SettingsForm`, `SpeciesPicker`, `EvSummary`, `PokemonMenu`, `IvTable`, `LevelForm`, `DeletePanel`).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.* Constituição v1.1.0.

| Princípio / Seção | Verificação | Pré | Pós |
|---|---|---|---|
| I. Estático no browser | Nada de servidor; sprites já embutidos. | ✅ | ✅ |
| II. Dois projetos | Nenhum projeto novo; `CurrentForm.Resolve` e `AdvanceTo` no `Domain/`, componentes só exibem. | ✅ | ✅ |
| III. Test-First | Testes de `Resolve`/`AdvanceTo`, JSON (ida e volta, apelido antigo) e componentes (Ajustes sem nome, chips, resumo com imagem, menu) antes do código. | ✅ | ✅ |
| IV. Fidelidade Gen 3 | Nenhuma regra de EV/IV muda; a normalização é feita na leitura, como os `aliases` (R2). | ✅ | ✅ |
| V. Dados locais | Mesmo formato de backup; arquivos antigos aceitos e versões antigas leem os novos (R1); Desfazer de `ReplaceLevel` restaura a forma atual. | ✅ | ✅ |
| VI. Mobile-first e acessível | Imagem 64 px cabe em 360 px; `role="img"` + `aria-label` no título; chips com `aria-pressed`. | ✅ | ✅ |
| VII. Outros idiomas | Chaves novas/removidas listadas no contrato; `KeyParityTests` e `NoHardcodedTextTests` cobrem. | ✅ | ✅ |
| Restrições / dotnet CLI primeiro | Sem pacotes, projetos nem JS novos; nada a gerar via CLI. | ✅ | ✅ |

Resultado: **sem violações**.

## Project Structure

### Documentation (this feature)

```text
specs/006-no-nickname-sprite/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── species-identity-ui.md
├── checklists/
│   └── requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
src/EvTrainer.Web/
├── Domain/Model/TrainingPokemon.cs        # Name → CurrentSpecies; AdvanceTo; InsertLevel avança; sem DisplaySpecies
├── Domain/CurrentForm.cs                  # novo: Resolve(name, line, ivLog)
├── Domain/Json/PokemonJson.cs             # "name" ⇄ CurrentSpecies via Resolve
├── State/TrainerStore.cs                  # SaveSettings(current, …); ReplaceLevel desfaz a forma atual
├── Components/StoreAwareComponent.cs      # PokemonLabel(pokemon)
├── Components/Settings/SettingsForm.razor # sem campo Nome; forma atual
├── Components/Settings/SettingsFormModel.cs # Name → Current; IsValid
├── Components/Settings/SpeciesPicker.razor  # chips alternáveis (Current/OnCurrent)
├── Components/Evs/EvSummary.razor         # imagem 64 px no título
├── Components/Shell/PokemonMenu.razor     # sprite de CurrentSpecies + rótulo + nature
├── Components/Ivs/IvTable.razor, LevelForm.razor
├── Components/Settings/DeletePanel.razor
├── wwwroot/css/app.css                    # .summary-who, .species-current
└── Localization/strings.pt-BR.json        # chaves do contrato
tests/EvTrainer.Tests/
├── Domain/CurrentFormTests.cs             # novo (substitui DisplaySpeciesTests)
├── Domain/ModelJsonTests.cs, BackupFormatTests.cs, LegacyExportGoldenTests.cs
├── State/TrainerStoreTests.cs
├── Components/Settings/SettingsFormTests.cs, SpeciesPickerTests.cs, DeletePanelTests.cs
├── Components/Evs/EvSummaryTests.cs
├── Components/Shell/PokemonMenuTests.cs
├── Components/Ivs/LevelFormTests.cs, IvTableTests.cs
└── Support/Pokemons.cs                    # Name → CurrentSpecies
```

**Structure Decision**: mesma solução de 2 projetos; a regra de forma atual fica no domínio e os
componentes só a exibem e a editam.

## Complexity Tracking

Sem violações da constituição a justificar.
