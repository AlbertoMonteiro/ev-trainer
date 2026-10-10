# Implementation Plan: Edição manual de EVs, IVs e anotações

**Branch**: `004-manual-edit` | **Date**: 2026-10-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/004-manual-edit/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Permitir corrigir manualmente o que o app calcula. **EVs**: um "Editar EVs" grava um **evento de
ajuste** no histórico que fixa os totais naquele ponto (o histórico é mantido) ou, por escolha
explícita e depois de uma confirmação com aviso, **apaga o histórico** e começa dos valores informados;
ambos com Desfazer. **IVs**: campo "IVs conhecidos" que prevalece na linha Final, com aviso quando
contradiz as anotações. **Anotações de nível** passam a poder ser editadas no lugar. Tudo entra no
armazenamento e no backup com campos opcionais, mantendo arquivos antigos válidos.

## Technical Context

**Language/Version**: C# 15 / .NET 11 (SDK RC fixado no `global.json`)

**Primary Dependencies**: nenhuma nova.

**Storage**: `localStorage`/backup ganham `events[].set` e `knownIvs` (opcionais).

**Testing**: xUnit v3 + bUnit (TDD), projeto existente.

**Target Platform / Project Type**: iguais às features anteriores (SPA estática PWA, 2 projetos).

**Performance Goals**: edição completa em < 30 s / ≤ 8 toques (SC-001); recálculo instantâneo.

**Constraints**: nunca apagar histórico sem confirmação (SC-002); textos via recursos; sem JS novo.

**Scale/Scope**: 2 entidades alteradas, 1 caso novo de `FinalCell`, 4 comandos novos no store,
2 componentes novos (`EvEditor`, `KnownIvsPanel`) e 3 alterados (`EvSummary`, `RecentHistory`,
`IvTable`/`LevelForm`).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.* Constituição v1.1.0.

| Princípio / Seção | Verificação | Pré | Pós |
|---|---|---|---|
| I. Estático no browser | Nada de servidor. | ✅ | ✅ |
| II. Dois projetos | Nenhum projeto novo; regras no `Domain/`. | ✅ | ✅ |
| III. Test-First | Testes de domínio (ajuste em `Totals`, ignorado em contagens/recentes/agrupamento, `FinalKnown`), JSON (ida e volta, arquivos antigos) e componentes (editor, confirmação, IVs conhecidos, edição de anotação) antes do código. | ✅ | ✅ |
| IV. Fidelidade Gen 3 | Totais continuam derivados do histórico em ordem — o ajuste é um item da sequência; regras de limite/vitamina valem depois dele (R1). | ✅ | ✅ |
| V. Dados locais | Campos novos no mesmo documento local e no backup; arquivos antigos aceitos (R5); apagar exige confirmação e tem Desfazer. | ✅ | ✅ |
| VI. Mobile-first e acessível | Campos `inputmode="numeric"`, confirmação `role="alertdialog"`, botões ≥ 44 px. | ✅ | ✅ |
| VII. Outros idiomas | Todos os textos novos via chaves; teste de texto fixo cobre as telas novas. | ✅ | ✅ |
| Restrições / dotnet CLI primeiro | Sem pacotes nem JS novos. | ✅ | ✅ |

Resultado: **sem violações**.

## Project Structure

### Documentation (this feature)

```text
specs/004-manual-edit/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── manual-edit.md
├── checklists/
│   └── requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
src/EvTrainer.Web/
├── Domain/Model/TrainingPokemon.cs        # BattleEvent.SetTotals; TrainingPokemon.KnownIvs
├── Domain/Json/PokemonJson.cs             # "set" e "knownIvs" (leitura tolerante, escrita)
├── Domain/Evs.cs, BattleLog.cs            # ajuste em Totals; ignorado em contagens/recentes/agrupamento
├── Domain/Ivs.cs                          # FinalKnown(Value, MatchesNotes)
├── State/TrainerStore.cs                  # AdjustEvs, ResetHistory, SetKnownIvs, ReplaceLevel (+ Desfazer)
├── Components/Evs/EvEditor.razor          # novo: editar EVs, opção de histórico, confirmação
├── Components/Evs/EvSummary.razor         # botão "Editar EVs"
├── Components/Evs/RecentHistory.razor     # item "Ajuste manual"
├── Components/Ivs/KnownIvsPanel.razor     # novo: IVs conhecidos
├── Components/Ivs/IvTable.razor           # FinalKnown + botão Editar por linha
├── Components/Ivs/LevelForm.razor         # modo edição
└── Localization/strings.pt-BR.json        # chaves novas
tests/EvTrainer.Tests/
├── Domain/ManualAdjustmentTests.cs        # novo
├── Domain/KnownIvsTests.cs                # novo
├── Domain/ModelJsonTests.cs, BackupFormatTests.cs
├── Components/Evs/EvEditorTests.cs        # novo
├── Components/Ivs/KnownIvsPanelTests.cs   # novo
└── Components/Ivs/IvTableTests.cs, LevelFormTests.cs, Evs/RecentHistoryTests.cs
```

**Structure Decision**: mesma solução de 2 projetos; regras no domínio, componentes finos.

## Complexity Tracking

Sem violações da constituição a justificar.
