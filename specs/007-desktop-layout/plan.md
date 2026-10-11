# Implementation Plan: Layout para desktop

**Branch**: `007-desktop-layout` | **Date**: 2026-10-11 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/007-desktop-layout/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Fazer o app aproveitar a tela no desktop sem mudar nada no celular. Abaixo de 1024 px o layout atual
fica intacto; a partir de 1024 px, cada tela organiza suas regiões lado a lado (EVs: resumo fixo à
esquerda e grade de adversários com ≥ 4 colunas à direita; IVs, Ajustes e Avaliar captura em duas
colunas) num conteúdo de até 1440 px; a partir de 1280 px, um painel lateral sempre visível lista os
Pokémon em treino para trocar com um clique. Tudo por CSS sobre regiões marcadas no markup, sem
JavaScript e sem re-renderizar por largura — redimensionar nunca perde estado.

## Technical Context

**Language/Version**: C# 15 / .NET 11 (SDK RC fixado no `global.json`)

**Primary Dependencies**: nenhuma nova.

**Storage**: nenhuma mudança (ver [data-model.md](./data-model.md)).

**Testing**: xUnit v3 + bUnit para estrutura (regiões, ordem do DOM, painel lateral); medições no
navegador para o que é visual ([quickstart.md](./quickstart.md)), conforme research R8.

**Target Platform**: browsers de celular e desktop; larguras de 360 a 2560 px.

**Project Type**: SPA estática PWA no GitHub Pages, 2 projetos (inalterado).

**Performance Goals**: nenhuma regressão; o layout muda só por CSS (sem custo de render).

**Constraints**: celular idêntico (SC-004); sem rolagem horizontal entre 360 e 2560 px (SC-005); sem
JS novo; textos via recursos; compilação completa sem AD0001 ao mexer em `CaptureView`.

**Scale/Scope**: 2 componentes novos (`Shell/PokemonList`, `Shell/PokemonSidebar`), wrappers de região
em `EvTab`, `IvTab`, `Home` (Ajustes) e `CaptureView`, `PokemonMenu` passa a usar `PokemonList`, ~1
chave de idioma nova e um bloco de CSS por ponto de quebra.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.* Constituição v1.2.0.

| Princípio / Seção | Verificação | Pré | Pós |
|---|---|---|---|
| I. Estático no browser | Só CSS e componentes; nada de servidor. | ✅ | ✅ |
| II. Dois projetos | Nenhum projeto novo; nenhuma regra de domínio muda. | ✅ | ✅ |
| III. Test-First | Testes bUnit antes do código para regiões/ordem e para o painel lateral; aspectos só visuais verificados por medições objetivas no navegador (R8), registradas na validação. | ✅ | ✅ |
| IV. Fidelidade Gen 3 | Não afeta regras. | ✅ | ✅ |
| V. Dados locais | Nenhuma mudança de dados. | ✅ | ✅ |
| VI. Mobile-first, bom no desktop | < 1024 px inalterado; ≥ 1024 px aproveita a largura até 1440 px; sem rolagem horizontal 360–2560; adaptação só por largura (CSS); redimensionar não perde estado (R2); hover e foco visíveis; registrar batalha continua 1 clique. | ✅ | ✅ |
| VII. Outros idiomas | `Sidebar.Title` via recursos; teste de texto fixo cobre o painel. | ✅ | ✅ |
| Restrições / dotnet CLI primeiro | Sem pacotes, sem JS, sem frameworks CSS. | ✅ | ✅ |

Resultado: **sem violações**.

## Project Structure

### Documentation (this feature)

```text
specs/007-desktop-layout/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── layout.md
├── checklists/
│   └── requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
src/EvTrainer.Web/
├── Components/Shell/PokemonList.razor        # novo: itens de Pokémon (menu e painel)
├── Components/Shell/PokemonSidebar.razor     # novo: <aside> com título, lista e adicionar
├── Components/Shell/PokemonMenu.razor        # usa PokemonList
├── Components/Evs/EvTab.razor                # .ev-layout + .ev-summary-area
├── Components/Ivs/IvTab.razor                # .iv-layout
├── Components/Capture/CaptureView.razor      # .capture-columns / .capture-inputs / .capture-output
├── Pages/Home.razor                          # .app-body com o painel; .settings-layout; seleção fecha a captura
├── Localization/strings.pt-BR.json           # Sidebar.Title
└── wwwroot/css/app.css                       # @media (min-width: 1024px) e (min-width: 1280px)
tests/EvTrainer.Tests/
├── Components/Shell/PokemonSidebarTests.cs   # novo
├── Components/Shell/PokemonMenuTests.cs      # lista compartilhada continua igual
├── Components/LayoutRegionsTests.cs          # novo: regiões e ordem do DOM por tela
└── Localization/NoHardcodedTextTests.cs      # painel lateral
```

**Structure Decision**: mesma solução de 2 projetos; mudança concentrada em CSS e em wrappers neutros
no markup.

## Complexity Tracking

Sem violações da constituição a justificar.
