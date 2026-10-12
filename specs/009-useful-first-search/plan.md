# Implementation Plan: Busca com úteis primeiro

**Branch**: `009-useful-first-search` | **Date**: 2026-10-12 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/009-useful-first-search/spec.md`

## Summary

Na aba EVs, os resultados de uma busca (e a lista "Todos" sem busca) passam a ser divididos em dois
grupos com título — **"Úteis para as metas"** primeiro, depois **"Outros resultados"** / **"Outros
adversários"** — usando a mesma regra de utilidade do filtro atual (`Evs.IsUseful`), com partição
estável (cada grupo mantém a ordem de hoje). Quando um grupo ficaria vazio, só o outro aparece com o
título de hoje ("Resultados" / "Todos"). Durante a busca, o controle Úteis/Todos fica desativado e sem
opção marcada; ao apagar a busca, a escolha salva volta a valer. Nada é salvo de novo.

## Technical Context

**Language/Version**: C# 15 / .NET 11 (SDK RC fixado no `global.json`)

**Primary Dependencies**: nenhuma nova.

**Storage**: nenhuma mudança (ver [data-model.md](./data-model.md)).

**Testing**: xUnit v3 para a partição no domínio; bUnit para a aba EVs (grupos, títulos, ordem,
controle desativado, reagrupamento após batalha).

**Target Platform**: browsers de celular e desktop (360–2560 px).

**Project Type**: SPA estática PWA no GitHub Pages, 2 projetos (inalterado).

**Performance Goals**: sem regressão perceptível; a partição é uma passada linear sobre ≤ 392
adversários por render, a mesma ordem de grandeza do filtro atual.

**Constraints**: regra de utilidade inalterada (FR-002); filtro "Úteis" sem busca idêntico (SC-004);
textos via recursos; compilação completa sem AD0001 (evitar `@for` seguido de lambdas na `EvTab`).

**Scale/Scope**: 1 função de domínio (`Evs.UsefulFirst`), `EvTab` renderiza 1 ou 2 grades,
`OpponentSearchBar` ganha o parâmetro `Searching`, 2 chaves de idioma novas, 1 regra de CSS para o
controle desativado.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.* Constituição v1.3.0.

| Princípio / Seção | Verificação | Pré | Pós |
|---|---|---|---|
| I. Estático no browser | Só componentes e domínio; nada de servidor. | ✅ | ✅ |
| II. Dois projetos | Nenhum projeto novo; a partição fica em `Domain/` (C# puro, sem Blazor). | ✅ | ✅ |
| III. Test-First | Testes de domínio da partição e testes bUnit da aba escritos antes e vistos falhar; testes existentes que codificam o comportamento antigo de "Todos" (título único, ordem da Pokédex) são atualizados para a nova regra da spec, não removidos. | ✅ | ✅ |
| IV. Fidelidade Gen 3 | Reusa `Evs.IsUseful` sem mudança (vitaminas e limite de 100 incluídos). | ✅ | ✅ |
| V. Dados locais | Nenhum dado novo; backup e sincronização inalterados. | ✅ | ✅ |
| VI. Mobile-first, bom no desktop, acessível | Mesmo comportamento em qualquer largura; grupos com título (`h3`); controle desativado com `disabled` e `aria-pressed="false"`; registrar batalha continua 1 toque. | ✅ | ✅ |
| VII. Outros idiomas | `Evs.List.OtherResults` e `Evs.List.Others` via recursos; teste de texto fixo cobre a busca agrupada. | ✅ | ✅ |
| Restrições / dotnet CLI primeiro | Sem pacotes, sem JS. | ✅ | ✅ |

Resultado: **sem violações**.

## Project Structure

### Documentation (this feature)

```text
specs/009-useful-first-search/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── ev-lists.md
├── checklists/
│   └── requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
src/EvTrainer.Web/
├── Domain/Evs.cs                              # UsefulFirst: partição estável úteis / outros
├── Components/Evs/EvTab.razor                 # 1 ou 2 grades em .main-list, títulos por contrato
├── Components/Evs/OpponentSearchBar.razor     # parâmetro Searching: controle desativado, nada marcado
├── Localization/strings.pt-BR.json            # Evs.List.OtherResults, Evs.List.Others
└── wwwroot/css/app.css                        # .segmented button:disabled
tests/EvTrainer.Tests/
├── Domain/UsefulFirstTests.cs                 # novo
├── Components/Evs/OpponentSearchTests.cs      # grupos, títulos, controle desativado, "Todos" agrupado
├── Components/Evs/EvTabFlowTests.cs           # reagrupa após batalha / desfazer
└── Localization/NoHardcodedTextTests.cs       # busca agrupada
```

**Structure Decision**: mesma solução de 2 projetos; regra de ordenação pura no domínio, apresentação
na `EvTab`.

## Complexity Tracking

Sem violações da constituição a justificar.
