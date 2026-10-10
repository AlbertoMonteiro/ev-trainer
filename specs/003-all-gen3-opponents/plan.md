# Implementation Plan: Adversários de todas as espécies do FireRed

**Branch**: `003-all-gen3-opponents` | **Date**: 2026-10-09 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-all-gen3-opponents/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Hoje a aba EVs só conhece os 151 de Kanto (`reference-data.json`); buscar "Milo" não acha Milotic.
O gerador da Pokédex passa a gravar também o **EV yield** de cada espécie (valor atual de `effort` da
PokéAPI — conferido igual à curadoria nos 151 de Kanto), e `ReferenceData` monta os adversários com
#1–151 curados + #152–386 da Pokédex + vitaminas. Todas as regras existentes já usam essa lista, então
busca, Úteis, Recentes, histórico, importação e selo do sprite passam a cobrir as 386 espécies. As
contagens `×N` passam a ser calculadas uma vez por renderização para a lista de 392 cards continuar
fluida.

## Technical Context

**Language/Version**: C# 15 / .NET 11 (SDK RC fixado no `global.json`)

**Primary Dependencies**: nenhuma nova.

**Storage**: nada novo persistido; `pokedex-gen3.json` ganha `evYield`.

**Testing**: xUnit v3 + bUnit (TDD), projeto existente.

**Target Platform**: igual às features 001/002.

**Project Type**: SPA estática (PWA), 2 projetos.

**Performance Goals**: 1 toque e ganho em < 1 s com "Todos" aberto (392 cards) — SC-003.

**Constraints**: `reference-data.json` intacto (curadoria do HANDOFF); sem requisição a terceiros em
uso; textos via recursos (nenhum texto novo previsto).

**Scale/Scope**: +235 adversários; 1 gerador alterado, 2 classes de domínio alteradas, 1 componente
otimizado.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.* Constituição v1.1.0.

| Princípio / Seção | Verificação | Pré | Pós |
|---|---|---|---|
| I. Estático no browser | Dados embutidos no build, sem servidor. | ✅ | ✅ |
| II. Dois projetos | Nenhum projeto novo; gerador é o file-based app existente. | ✅ | ✅ |
| III. Test-First | Testes de composição dos 392 adversários, consistência #1–151, Milotic/Zigzagoon e `CountsByOpponent` antes do código. | ✅ | ✅ |
| IV. Fidelidade Gen 3 | Kanto continua com os yields curados; #152–386 com yields validados contra a curadoria (R1, R3); stats base Gen III já existentes. | ✅ | ✅ |
| V. Dados locais | Nada muda no armazenamento; dados de referência embutidos. | ✅ | ✅ |
| VI. Mobile-first | Lista maior continua fluida (R4); carregamento preguiçoso de sprites mantido. | ✅ | ✅ |
| VII. Outros idiomas | Nenhum texto novo; nomes de espécies são dados. | ✅ | ✅ |
| Restrições / dotnet CLI primeiro | Sem pacotes; regeneração por `dotnet run scripts/BuildPokedex.cs`. | ✅ | ✅ |

Resultado: **sem violações**.

## Project Structure

### Documentation (this feature)

```text
specs/003-all-gen3-opponents/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── reference-data.md
├── checklists/
│   └── requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
scripts/BuildPokedex.cs                       # + evYield (effort atual) por espécie
pokedex-gen3.json                             # regenerado com evYield
src/EvTrainer.Web/
├── Domain/Reference/Pokedex.cs               # PokedexSpecies + EvYield
├── Domain/Reference/ReferenceData.cs         # LoadEmbedded lê os dois recursos; 386 + 6 adversários
├── Domain/Evs.cs                             # + CountsByOpponent
└── Components/Evs/OpponentGrid.razor         # usa CountsByOpponent (uma passada por render)
tests/EvTrainer.Tests/
├── Domain/ReferenceDataTests.cs              # 392 adversários, ordem, consistência #1–151, Milotic
├── Domain/PokedexTests.cs                    # EvYield nas 386; valores fixos das 8 espécies de risco (R1)
├── Domain/UsefulFilterTests.cs               # Zigzagoon útil, Machop não
├── Domain/SearchTests.cs                     # "milo", "350"
├── Domain/EvTotalsTests.cs                   # registrar Milotic soma +2 SpDef
└── Components/Evs/OpponentSearchTests.cs     # "Todos" com 392 cards; busca "milo" na tela
```

**Structure Decision**: mesma solução de 2 projetos; mudanças concentradas no domínio de referência e
no gerador, sem mudança visual.

## Complexity Tracking

Sem violações da constituição a justificar.
