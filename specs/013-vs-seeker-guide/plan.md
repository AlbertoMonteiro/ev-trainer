# Implementation Plan: Guia de treino com VS Seeker

**Branch**: `013-vs-seeker-guide` | **Date**: 2026-10-13 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/013-vs-seeker-guide/spec.md`

## Summary

Os dados vêm da **decompilação do FireRed** (`pret/pokefirered`): `src/vs_seeker.c` (`sRematches`: até 6 estágios por
treinador, com o mapa, e a regra de progresso de cada estágio), `src/data/trainers.h` (classe, nome, batalha dupla,
party) e `src/data/trainer_parties.h` (espécie e nível). Um script `scripts/BuildVsSeekerGuide.cs` (mesmo padrão do
`BuildPokedex.cs`) gera `vs-seeker-guide.json` na raiz — só treinadores de Kanto, com estágios e a condição de cada um —
e o app o embute. Os EVs **não** são gravados no arquivo: o domínio os calcula com o rendimento do `pokedex-gen3.json`
(SC-005). Uma classe pura `TrainingGuide` filtra estágios pela fase (antes/depois da Elite Four), ordena por EVs do
atributo, escolhe o atributo inicial pelo Pokémon selecionado e gera os avisos. A página `TrainingGuideView` abre pelo
menu (como Captura e Sincronização) e pela aba EVs; a fase fica nas preferências do aparelho.

## Technical Context

**Language/Version**: C# 15 / .NET 11 (SDK RC fixado no `global.json`)

**Primary Dependencies**: nenhuma nova no app; o gerador usa só `HttpClient` e regex (roda à parte, como os outros
scripts).

**Storage**: `UiPreferences.GuidePhase` (`"before"` | `"after"`, padrão `"before"`) no `localStorage` de preferências;
nada no backup/sincronização. Dados de referência embutidos (`vs-seeker-guide.json`).

**Testing**: xUnit v3 — `TrainingGuide` (fases, ordenação, desempate, EVs = soma dos rendimentos, avisos, atributo
inicial) e leitura/validação do JSON embutido (todo Pokémon existe na Pokédex, só mapas de Kanto, nenhum treinador de
prédio/ginásio); bUnit — página (seletor de atributo e fase, entradas separadas, estágios em ordem, destaque, notas,
fontes, textos pt/en sem texto fixo).

**Target Platform**: browsers de celular e desktop (320–2560 px).

**Project Type**: SPA estática PWA no GitHub Pages, 2 projetos (inalterado).

**Performance Goals**: lista recalculada na hora (≈ 220 treinadores × ≤ 6 estágios).

**Constraints**: offline; textos via recursos; nomes de treinador/Pokémon como no jogo (inglês); locais pelo
`LocationNames` existente (pt/en); dados reproduzíveis por script (Princípio "dotnet CLI primeiro").

**Scale/Scope**: 1 script gerador, 1 JSON (~220 treinadores), 2 classes de domínio (`VsSeekerGuide` dados +
`TrainingGuide` regras), 1 página + item de menu + atalho na aba EVs, 1 preferência, ~20 chaves de texto por idioma.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.* Constituição v1.3.0.

| Princípio / Seção | Verificação | Pré | Pós |
|---|---|---|---|
| I. Estático no browser | Dados embutidos; nada de servidor; funciona offline. | ✅ | ✅ |
| II. Dois projetos | Gerador é um script de arquivo único em `scripts/` (como os existentes), não projeto; regras em `Domain/`. | ✅ | ✅ |
| III. Test-First | Testes de domínio e bUnit antes do código; teste de integridade dos dados gerados. | ✅ | ✅ |
| IV. Fidelidade Gen 3 | Times, níveis e condições de revanche tirados do código do próprio jogo; EVs da Pokédex Gen III já usada. | ✅ | ✅ |
| V. Dados locais | Só uma preferência nova no aparelho; backup/sync inalterados. | ✅ | ✅ |
| VI. Mobile-first, acessível | Lista em cartões de uma coluna no celular, 2+ colunas no desktop; seletores com rótulo; abre em ≤ 2 toques. | ✅ | ✅ |
| VII. Outros idiomas | Textos e condições via recursos pt/en; locais via `LocationNames`. | ✅ | ✅ |
| Restrições / dotnet CLI primeiro | Gerador roda com `dotnet run scripts/BuildVsSeekerGuide.cs`. | ✅ | ✅ |

Resultado: **sem violações**.

## Project Structure

### Documentation (this feature)

```text
specs/013-vs-seeker-guide/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── vs-seeker-guide-json.md
│   └── training-guide-ui.md
├── checklists/
│   └── requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
scripts/BuildVsSeekerGuide.cs                 # novo: gera vs-seeker-guide.json a partir do pret/pokefirered (commit fixo)
vs-seeker-guide.json                          # novo (gerado, versionado, embutido)
reference-data.en.json                        # + nomes em inglês de locais novos, se houver
src/EvTrainer.Web/
├── EvTrainer.Web.csproj                      # EmbeddedResource EvTrainer.vs-seeker-guide.json
├── Domain/Reference/VsSeekerGuide.cs         # novo: carrega o JSON (treinadores, estágios)
├── Domain/TrainingGuide.cs                   # novo: fases, EVs por estágio, ranking, avisos, atributo inicial
├── Domain/Model/TrainingPokemon.cs           # UiPreferences.GuidePhase
├── State/TrainerStore.cs                     # SetGuidePhase
├── Components/Guide/TrainingGuideView.razor  # nova página
├── Components/Shell/PokemonMenu.razor        # item "Onde treinar"
├── Components/Shell/AppHeader.razor          # repassa OnGuide
├── Pages/Home.razor                          # _guiding como _syncing/_capturing
├── Components/Evs/EvTab.razor                # atalho "Onde treinar" (link discreto acima da lista)
├── Localization/strings.pt-BR.json / strings.en.json
└── wwwroot/css/app.css                       # .guide-*
tests/EvTrainer.Tests/
├── Domain/VsSeekerGuideDataTests.cs          # integridade do JSON
├── Domain/TrainingGuideTests.cs              # regras
├── Components/Guide/TrainingGuideViewTests.cs
└── Localization/* (paridade, sem texto fixo, inglês sem português)
```

**Structure Decision**: mesma solução; dados gerados por script e embutidos, regras puras no domínio, página no padrão
das telas abertas pelo menu.

## Complexity Tracking

Sem violações da constituição a justificar.
