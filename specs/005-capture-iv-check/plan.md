# Implementation Plan: Avaliar IVs na captura

**Branch**: `005-capture-iv-check` | **Date**: 2026-10-11 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/005-capture-iv-check/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Nova tela **Avaliar captura**: o jogador escolhe a espécie no Pokédex, informa nível, nature e os 6
stats de um Pokémon recém-capturado (EVs zero) e vê, por stat, a faixa de IV (mesmo cálculo da tabela
de IVs), uma classificação Ótimo/Bom/Médio/Ruim, um veredito geral sobre os **stats que contam**
(automáticos pela espécie, ajustáveis e lembrados) e o efeito da nature. O app guarda, por espécie, o
**melhor até agora** e compara cada nova captura com ele; ao concluir, uma avaliação melhor o
substitui (com Desfazer). **Adicionar à minha lista** cria o Pokémon com a linha evolutiva, a nature e
a primeira anotação de nível já preenchida.

## Technical Context

**Language/Version**: C# 15 / .NET 11 (SDK RC fixado no `global.json`)

**Primary Dependencies**: nenhuma nova (Blazor WASM, xUnit v3, bUnit já existentes).

**Storage**: `localStorage` ganha a chave `evtrainer:v1:capture` (melhor até agora + stats que
contam), fora do backup (R7). Formato do backup inalterado.

**Testing**: xUnit v3 + bUnit (TDD), projeto existente.

**Target Platform / Project Type**: SPA estática PWA no GitHub Pages, 2 projetos (inalterado).

**Performance Goals**: resultado instantâneo ao digitar (≤ 6 × 32 avaliações da fórmula); avaliação
completa em < 45 s no celular (SC-001).

**Constraints**: offline; 360 px sem rolagem horizontal; textos via recursos; sem JS novo; mesmas
regras de IV da tabela existente (SC-002).

**Scale/Scope**: 1 arquivo de domínio novo (`Capture.cs`), 1 caso novo de `IvCell`, 1 store novo
(`CaptureStore`), 1 comando novo no `TrainerStore`, 2 métodos no repositório, ~4 componentes novos
(`CaptureView`, `CaptureStatsTable`, `BestCaptureCard`, `Shared/SpeciesSearch`) e alterações em
`Home`, `PokemonMenu`, `EmptyState`, `SpeciesPicker`, `IvTable`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.* Constituição v1.1.0.

| Princípio / Seção | Verificação | Pré | Pós |
|---|---|---|---|
| I. Estático no browser | Tudo no cliente; nenhuma chamada de rede nova. | ✅ | ✅ |
| II. Dois projetos | Nenhum projeto novo; regras em `Domain/Capture.cs` (C# puro, sem Blazor/JS). | ✅ | ✅ |
| III. Test-First | Testes de domínio (graus, padrão de stats, veredito, nature, comparação, Shedinja, sobrecarga de `RangeFor` igual à tabela), JSON das notas de captura, `CaptureStore`, `AddFromCapture` e componentes antes do código. | ✅ | ✅ |
| IV. Fidelidade Gen 3 | Uma única implementação de faixa de IV (R2), aritmética inteira, nature 11/9/10, HP de Shedinja = 1 (R3). | ✅ | ✅ |
| V. Dados locais | Notas de captura no aparelho atrás de `IPokemonRepository`; fora do backup como as preferências (conveniência de caça, não dado de treino — R7); adicionar e substituir têm Desfazer. | ✅ | ✅ |
| VI. Mobile-first e acessível | Coluna única, `inputmode="numeric"`, alternância de stat com `aria-pressed`, botões ≥ 44 px, validação em 360 px. | ✅ | ✅ |
| VII. Outros idiomas | Chaves `Capture.*`/`Menu.Capture`; teste de texto fixo cobre a tela nova. | ✅ | ✅ |
| Restrições / dotnet CLI primeiro | Sem pacotes nem JS novos. | ✅ | ✅ |

Resultado: **sem violações**. Observação sobre o Princípio V ("export/import é o único meio de levar
dados entre dispositivos"): o melhor até agora e a marcação de stats não viajam no backup, assim como
tema, filtro e idioma; é uma escolha consciente (R7) e não perde dado de treino.

## Project Structure

### Documentation (this feature)

```text
specs/005-capture-iv-check/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── capture.md
├── checklists/
│   └── requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
src/EvTrainer.Web/
├── Domain/Ivs.cs                              # RangeFor(species, nature, level, stats, evs); IvUnknowable
├── Domain/Capture.cs                          # novo: CaptureInput/Result, graus, veredito, nature, comparação, padrão de stats
├── Domain/Json/CaptureNotesJson.cs            # novo: leitura tolerante/escrita de evtrainer:v1:capture
├── State/IPokemonRepository.cs                # LoadCaptureNotes / SaveCaptureNotes
├── State/LocalStorageRepository.cs            # chave evtrainer:v1:capture
├── State/CaptureStore.cs                      # novo: stats que contam, melhor até agora, Conclude/Forget
├── State/TrainerStore.cs                      # AddFromCapture
├── Components/Shared/SpeciesSearch.razor      # novo: busca do Pokédex extraída do SpeciesPicker
├── Components/Settings/SpeciesPicker.razor    # usa SpeciesSearch
├── Components/Capture/CaptureView.razor       # novo: formulário, resultados, ações
├── Components/Capture/CaptureStatsTable.razor # novo: faixa, grau e "conta" por stat
├── Components/Capture/BestCaptureCard.razor   # novo: melhor até agora e comparação
├── Components/Ivs/IvTable.razor               # exibe IvUnknowable
├── Components/Shell/PokemonMenu.razor         # item Avaliar captura
├── Components/Shell/EmptyState.razor          # botão Avaliar captura
├── Pages/Home.razor                           # visão Capture
├── Program.cs                                 # registra CaptureStore
└── Localization/strings.pt-BR.json            # chaves novas
tests/EvTrainer.Tests/
├── Domain/CaptureTests.cs                     # novo
├── Domain/IvTests.cs                          # sobrecarga + Shedinja
├── Domain/CaptureNotesJsonTests.cs            # novo
├── State/CaptureStoreTests.cs                 # novo
├── State/TrainerStoreTests.cs                 # AddFromCapture
├── Components/Capture/CaptureViewTests.cs     # novo
├── Components/Shell/*                         # menu e tela vazia
├── Localization/NoHardcodedTextTests.cs       # tela nova
└── Support/InMemoryRepository.cs              # notas de captura em memória
```

**Structure Decision**: mesma solução de 2 projetos; regras no domínio, store fino, componentes finos.

## Complexity Tracking

Sem violações da constituição a justificar.
