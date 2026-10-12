# Implementation Plan: Botão Editar EVs no cabeçalho do resumo

**Branch**: `010-summary-edit-button` | **Date**: 2026-10-12 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/010-summary-edit-button/spec.md`

## Summary

No cartão de resumo da aba EVs (`EvSummary`), o texto do total passa de "{atual} / 510 · metas {soma}"
para "{atual} / 510" (só o recurso `Evs.Summary.Totals` muda). O botão `button.edit-evs` sai do fim do
cartão e passa a ser o último item do `.card-head`, à direita do total; continua oculto enquanto o
`EvEditor` está aberto, e o editor continua abrindo abaixo da grade. CSS: o botão deixa de ocupar a
largura toda e ganha um visual compacto que mantém 44 px de altura; o total não quebra linha. Nada é
salvo de novo.

## Technical Context

**Language/Version**: C# 15 / .NET 11 (SDK RC fixado no `global.json`)

**Primary Dependencies**: nenhuma nova.

**Storage**: nenhuma mudança (ver [data-model.md](./data-model.md)).

**Testing**: bUnit para `EvSummary` (texto do total, posição do botão, oculto com editor aberto,
volta ao fechar); testes existentes de `EvEditor` e `NoHardcodedText` continuam achando
`button.edit-evs`.

**Target Platform**: browsers de celular e desktop (320–2560 px).

**Project Type**: SPA estática PWA no GitHub Pages, 2 projetos (inalterado).

**Performance Goals**: n/a (mudança só de marcação e estilo).

**Constraints**: cabeçalho em uma linha a 320 px (sprite 64 px + total + botão cabem em ~260 px úteis
do cartão); área de toque ≥ 44 px; textos via recursos.

**Scale/Scope**: 1 componente (`EvSummary.razor`), 1 recurso de texto alterado, 2 regras de CSS,
1 arquivo de teste atualizado.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.* Constituição v1.3.0.

| Princípio / Seção | Verificação | Pré | Pós |
|---|---|---|---|
| I. Estático no browser | Só componente e CSS. | ✅ | ✅ |
| II. Dois projetos | Nenhum projeto ou camada nova; nenhuma regra de domínio muda. | ✅ | ✅ |
| III. Test-First | Testes bUnit novos/atualizados de `EvSummary` escritos antes e vistos falhar; o teste que codifica o texto com metas é atualizado para a regra da spec, não removido. | ✅ | ✅ |
| IV. Fidelidade Gen 3 | Limite de 510 mostrado como hoje; nenhuma regra de EV muda. | ✅ | ✅ |
| V. Dados locais | Nenhum dado novo; backup e sincronização inalterados. | ✅ | ✅ |
| VI. Mobile-first, bom no desktop, acessível | Cabeçalho cabe em 320 px; botão continua `<button>` com rótulo e 44 px de altura; editor abre com 1 toque; mesma regra no desktop. | ✅ | ✅ |
| VII. Outros idiomas | `Evs.Summary.Totals` e `Evs.Edit` continuam recursos; teste de texto fixo da edição manual continua cobrindo. | ✅ | ✅ |
| Restrições / dotnet CLI primeiro | Sem pacotes, sem JS. | ✅ | ✅ |

Resultado: **sem violações**.

## Project Structure

### Documentation (this feature)

```text
specs/010-summary-edit-button/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── ev-summary.md
├── checklists/
│   └── requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
src/EvTrainer.Web/
├── Components/Evs/EvSummary.razor             # botão dentro do .card-head; total sem metas
├── Localization/strings.pt-BR.json            # Evs.Summary.Totals = "{0} / 510"
└── wwwroot/css/app.css                        # .summary .edit-evs compacto; .totals sem quebra
tests/EvTrainer.Tests/
└── Components/Evs/EvSummaryTests.cs           # total sem metas, botão no cabeçalho, oculto/volta
```

**Structure Decision**: mesma solução de 2 projetos; mudança restrita à apresentação do resumo.

## Complexity Tracking

Sem violações da constituição a justificar.
