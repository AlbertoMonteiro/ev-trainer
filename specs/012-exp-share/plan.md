# Implementation Plan: Exp. Share

**Branch**: `012-exp-share` | **Date**: 2026-10-12 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/012-exp-share/spec.md`

## Summary

Cada `TrainingPokemon` ganha a flag `ExpShare` (como `Pokerus`/`MachoBrace`; no JSON `"expShare"`), com no máximo
um portador garantido pelo `TrainerStore`. `BattleEvent` ganha `Shared` (`"x": true` no JSON) para identificar
batalhas vindas do Exp. Share. Ao registrar uma batalha (não vitamina) para o Pokémon selecionado, se houver outro
portador, uma função pura `BattleLog.RecordShared` adiciona ao portador a mesma batalha com multiplicador
`Evs.Multiplier(portador.Pokerus, machoBrace: false)`, agrupando só com o último evento compartilhado igual. O
`TrainerStore.RecordBattle` devolve os dois resultados; o aviso mostra os dois ganhos e o Desfazer desfaz os dois.
A tela de EVs ganha um seletor "Exp. Share" no cartão de resumo (abaixo da grade). O histórico marca as batalhas
compartilhadas. Backup e sincronização carregam os novos campos; se uma sincronização deixar dois portadores, fica o
mais recente.

## Technical Context

**Language/Version**: C# 15 / .NET 11 (SDK RC fixado no `global.json`)

**Primary Dependencies**: nenhuma nova.

**Storage**: backup/`localStorage` ganham `expShare` (Pokémon) e `x` (evento), ambos opcionais e gravados só quando
verdadeiros; leitura tolerante de dados antigos (ver [data-model.md](./data-model.md)).

**Testing**: xUnit v3 para as regras (tabela Pokérus × Macho Brace, limites, agrupamento, desfazer, portador único,
JSON ida e volta, merge da sincronização); bUnit para o seletor, o aviso com dois ganhos, Desfazer e histórico.

**Target Platform**: browsers de celular e desktop (320–2560 px).

**Project Type**: SPA estática PWA no GitHub Pages, 2 projetos (inalterado).

**Performance Goals**: registrar batalha continua instantâneo (2 recomputações de totais em vez de 1).

**Constraints**: regras da Gen 3 (FR-004/FR-005); 1 toque para registrar; textos pt/en; ida e volta do backup
(`Read(Write(x)) == x`).

**Scale/Scope**: 2 campos de modelo + DTO, 1 função de domínio, ajustes no `TrainerStore` (registrar, desfazer,
marcar portador, apagar, sync), `EvTab` (aviso), `EvSummary` (seletor), `RecentHistory` (marca), ~6 chaves de texto
por idioma.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.* Constituição v1.3.0.

| Princípio / Seção | Verificação | Pré | Pós |
|---|---|---|---|
| I. Estático no browser | Tudo local. | ✅ | ✅ |
| II. Dois projetos | Nenhum projeto novo; regra em `Domain/BattleLog.cs` (C# puro). | ✅ | ✅ |
| III. Test-First | Tabela de casos e testes bUnit escritos antes e vistos falhar. | ✅ | ✅ |
| IV. Fidelidade Gen 3 | EV inteiro para o portador; Pokérus por Pokémon; Macho Brace só de quem segura; limites 255/510 por Pokémon; vitaminas fora. | ✅ | ✅ |
| V. Dados locais | Campos novos no mesmo formato de backup/sync, opcionais, compatíveis com arquivos antigos; nada sai do aparelho além da sync opcional existente. | ✅ | ✅ |
| VI. Mobile-first, acessível | Registrar continua 1 toque; seletor é um `<select>` com rótulo; aviso legível por leitor de tela (já é `status`). | ✅ | ✅ |
| VII. Outros idiomas | Textos novos em `strings.pt-BR.json` e `strings.en.json`; paridade testada. | ✅ | ✅ |
| Restrições / dotnet CLI primeiro | Sem pacotes, sem JS. | ✅ | ✅ |

Resultado: **sem violações**.

## Project Structure

### Documentation (this feature)

```text
specs/012-exp-share/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── exp-share.md
├── checklists/
│   └── requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
src/EvTrainer.Web/
├── Domain/Model/TrainingPokemon.cs        # TrainingPokemon.ExpShare; BattleEvent.Shared (+ Clone)
├── Domain/BattleLog.cs                    # RecordShared, UndoLastShared
├── Domain/Json/PokemonJson.cs             # "expShare", "x" (gravados só quando true)
├── Domain/Sync/… (merge existente)        # nada muda no merge por Pokémon
├── State/TrainerStore.cs                  # ExpShareHolderId, SetExpShare, RecordBattle → SharedRecord, UndoLastBattle(ambos), Delete/Load/ApplySync normalizam portador único
├── Components/Evs/EvTab.razor             # aviso com os dois ganhos; Desfazer dos dois
├── Components/Evs/EvSummary.razor         # seletor "Exp. Share" (só com 2+ Pokémon)
├── Components/Evs/RecentHistory.razor     # marca "Exp. Share" nas batalhas compartilhadas
├── Localization/strings.pt-BR.json        # chaves novas
└── Localization/strings.en.json           # chaves novas
tests/EvTrainer.Tests/
├── Domain/ExpShareTests.cs                # novo: tabela de casos, limites, agrupamento, vitaminas, desfazer
├── Domain/ModelJsonTests.cs / BackupFormatTests.cs  # ida e volta, arquivo antigo
├── State/TrainerStoreTests.cs             # portador único, apagar/desfazer, sync com 2 portadores
├── Components/Evs/EvTabFlowTests.cs       # aviso com dois ganhos, Desfazer
├── Components/Evs/EvSummaryTests.cs       # seletor
├── Components/Evs/RecentHistoryTests.cs   # marca no histórico
└── Localization/*                         # paridade e texto fixo cobrem o seletor e o aviso
```

**Structure Decision**: mesma solução; regra de ganho compartilhado no domínio, orquestração no `TrainerStore`,
apresentação nos componentes da aba EVs.

## Complexity Tracking

Sem violações da constituição a justificar.
