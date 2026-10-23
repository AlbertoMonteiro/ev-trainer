# Implementation Plan: Planejador da Elite Four

**Branch**: `015-elite-four-planner` | **Date**: 2026-10-23 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/015-elite-four-planner/spec.md`

## Summary

Novo gerador `scripts/BuildLeague.cs` lê a decompilação (pret/pokefirered) e grava `league.json`: golpes (tipo,
poder, precisão, PP, tipo de dano), espécies #1–386 (tipos, habilidades, golpes por nível, TM/HM e tutor) e a Liga
(Lorelei, Bruno, Agatha, Lance e Campeão; primeira luta e revanche; Campeão por inicial do jogador), com nível, item,
habilidade (sempre a primeira da espécie, research R2) e os 4 golpes de cada Pokémon. O domínio ganha
`TypeChart` (tabela Gen 3), `League` (dados embutidos) e `LeaguePlanner` (efetividade com habilidades, cobertura,
ameaças, resumo e sugestão gulosa de golpes). A equipe planejada (`LeaguePlan`: luta, inicial, até 6 membros com
espécie, habilidade, golpes e fixados) vive no `TrainerStore`, é salva no aparelho (`evtrainer:v1:plan`) e entra na
**sincronização**: o documento do Gist ganha a seção opcional `plan` com `updatedAt`; no merge vence a versão mais
recente (empate: maior conteúdo), como os Pokémon. A página `LeaguePlannerView` abre pelo menu, como "Onde treinar".

## Technical Context

**Language/Version**: C# 15 / .NET 11 (SDK fixado no `global.json`)

**Primary Dependencies**: nenhuma nova.

**Storage**: `evtrainer:v1:plan` (aparelho); seção `plan` no documento sincronizado (Gist); backup inalterado.
`league.json` novo, embutido.

**Testing**: xUnit v3 — dados (`league.json`: Lorelei primeira/revanche, Campeão por inicial, aprendizado do
Vileplume, Levitate do Gengar), `TypeChart`, efetividade com habilidades, cobertura/resumo, ameaças, sugestão, JSON do
plano, merge do plano; bUnit — página (luta, inicial, montar equipe, marcas, sugestão, persistência), menu, textos.

**Target Platform**: browsers de celular e desktop (320–2560 px).

**Project Type**: SPA estática PWA no GitHub Pages, 2 projetos (inalterado).

**Performance Goals**: cálculo de cobertura de 6 membros × 4 golpes × ~31 adversários instantâneo; sugestão da equipe
(guloso, ≤ 24 rodadas × ~6×80 candidatos × 31 adversários) bem abaixo de 1 s no WebAssembly.

**Constraints**: offline; textos via recursos; dados reproduzíveis por script; sync nunca perde dados locais.

**Scale/Scope**: gerador + `league.json` (~250 KB), 3 classes de domínio, JSON/merge do plano, `TrainerStore`,
`SyncService`, repositório, 1 página com componentes, menu, ~50 chaves de texto por idioma.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.* Constituição v1.3.0.

| Princípio / Seção | Verificação | Pré | Pós |
|---|---|---|---|
| I. Estático no browser | Dados embutidos; sync continua direto do browser no Gist do usuário. | ✅ | ✅ |
| II. Dois projetos | Nada novo; regras no domínio puro (`TypeChart`, `LeaguePlanner`). | ✅ | ✅ |
| III. Test-First | Testes de domínio, JSON, merge e bUnit antes do código. | ✅ | ✅ |
| IV. Fidelidade Gen 3 | Times, golpes e aprendizado da decompilação; tabela de tipos e categoria física/especial por tipo da Gen 3. | ✅ | ✅ |
| V. Dados locais / sync | Plano salvo no aparelho primeiro; sync opcional, regra de conflito documentada (mais recente vence), falha não perde dados. | ✅ | ✅ |
| VI. Mobile-first, acessível | Página em coluna única no celular; controles segmentados com `aria-pressed`; `select` nativo para golpes. | ✅ | ✅ |
| VII. Outros idiomas | Textos via recursos (pt/en); nomes do jogo em inglês. | ✅ | ✅ |
| Restrições / dotnet CLI primeiro | Gerador por `dotnet run scripts/BuildLeague.cs`. | ✅ | ✅ |

Resultado: **sem violações**.

## Project Structure

### Documentation (this feature)

```text
specs/015-elite-four-planner/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── league-data.md
│   └── plan-sync.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
scripts/BuildLeague.cs                              # novo gerador
league.json                                         # novo, embutido
src/EvTrainer.Web/
├── Domain/
│   ├── TypeChart.cs                                # tabela de tipos Gen 3, categoria por tipo
│   ├── LeaguePlanner.cs                            # efetividade, cobertura, ameaças, resumo, sugestão
│   ├── SyncMerge.cs                                # + merge do plano
│   ├── Json/PokemonJson.cs                         # + seção plan no documento sincronizado
│   ├── Json/LeaguePlanJson.cs                      # plano ↔ JSON
│   ├── Model/LeaguePlan.cs                         # equipe planejada
│   └── Reference/League.cs                         # league.json
├── State/ (TrainerStore, SyncService, IPokemonRepository, LocalStorageRepository)
├── Components/League/ (LeaguePlannerView, PlanMemberCard, LeagueOpponentCard)
├── Components/Shell/ (PokemonMenu, AppHeader), Pages/Home.razor
├── Localization/strings.*.json
└── wwwroot/css/app.css
tests/EvTrainer.Tests/ (Domain, State, Components/League, Localization)
```

**Structure Decision**: mesma organização das features 013/014.

## Complexity Tracking

Sem violações.
