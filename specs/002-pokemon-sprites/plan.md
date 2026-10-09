# Implementation Plan: Sprites dos Pokémon

**Branch**: `002-pokemon-sprites` | **Date**: 2026-10-09 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-pokemon-sprites/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Mostrar os sprites FireRed/LeafGreen das 386 espécies nos cards de adversário, no seletor de
espécie e no menu, com um selo colorido (cor do stat de EV + número da Pokédex) quando a imagem não
puder ser exibida. Os PNG (≈250 KB) são baixados uma vez do PokeAPI/sprites, fixados num commit, por
um file-based app versionado, e vão dentro do `wwwroot` — o service worker existente já os guarda
para uso offline. Um componente `PokemonSprite` reserva o tamanho, faz o *fade-in* no `onload` e cai
para o selo no `onerror`; a cor do selo vem de uma regra de domínio pura.

## Technical Context

**Language/Version**: C# 15 / .NET 11 (mesmo SDK RC fixado no `global.json`)

**Primary Dependencies**: nenhuma nova. Blazor WebAssembly (eventos `onload`/`onerror` nativos),
file-based app para o download.

**Storage**: nenhum dado novo persistido; sprites são assets estáticos em `wwwroot/sprites/`.

**Testing**: xUnit v3 (regra do selo) + bUnit (componente e telas), no projeto existente.

**Target Platform**: igual à feature 001 (navegadores móveis, GitHub Pages em `/ev-trainer/`).

**Project Type**: SPA estática (PWA) — mesma solução, 2 projetos.

**Performance Goals**: +≤400 KB no primeiro download (SC-004); registrar batalha continua com 1
toque e ganho em < 1 s (SC-005); zero deslocamento de layout (SC-003).

**Constraints**: nenhuma requisição a terceiros em uso (Princípio V); offline após a instalação,
com o "buraco" aceito; textos via recursos de idioma; sem JS novo.

**Scale/Scope**: 386 arquivos PNG; 1 componente novo, 3 componentes alterados, 1 regra de domínio,
1 script.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.* Constituição v1.1.0.

| Princípio / Seção | Verificação | Pré | Pós |
|---|---|---|---|
| I. Estático no browser | Sprites são arquivos estáticos publicados com o app (R1). | ✅ | ✅ |
| II. Dois projetos | Nenhum projeto novo; o download é um file-based app (`dotnet run arquivo.cs`), como o `BuildPokedex.cs`. | ✅ | ✅ |
| III. Test-First | `SpriteBadgeTests` e `PokemonSpriteTests` antes do código; telas existentes ganham asserts de sprite. | ✅ | ✅ |
| IV. Fidelidade Gen 3 | Sprites da própria Gen III (FR/LG); regra do selo usa o EV yield de `reference-data.json`. | ✅ | ✅ |
| V. Dados locais / sem terceiros | Imagens servidas do mesmo endereço; link de crédito só abre se o usuário tocar (R6). | ✅ | ✅ |
| VI. Mobile-first e acessível | Tamanho fixo (sem *layout shift*), `alt=""`/`aria-hidden`, movimento reduzido respeitado, legível nos dois temas (R3, R4). | ✅ | ✅ |
| VII. Outros idiomas | Rótulo `#{0}` e crédito via chaves (`Sprite.Dex`, `Menu.Credits`); teste de texto fixo continua valendo. | ✅ | ✅ |
| Restrições técnicas | Sem pacote NuGet novo; sem JS novo; .NET 11/C# 15. | ✅ | ✅ |
| Fluxo e deploy | Workflow inalterado; sprites versionados no repo entram no publish. | ✅ | ✅ |
| dotnet CLI primeiro | Download e verificação por `dotnet run scripts/DownloadSprites.cs`. | ✅ | ✅ |

Resultado: **sem violações**.

Observação de direitos (não é regra da constituição): imagens © The Pokémon Company, uso de fã
gratuito com crédito (FR-011); o selo cobre a eventual remoção dos arquivos.

## Project Structure

### Documentation (this feature)

```text
specs/002-pokemon-sprites/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── pokemon-sprite.md
├── checklists/
│   └── requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
scripts/
└── DownloadSprites.cs                      # novo: baixa e valida os 386 PNG (commit fixo)

src/EvTrainer.Web/
├── Domain/
│   ├── SpriteBadge.cs                      # novo: Dex, Stats (0–2), Initial, HasSprite
│   └── Model/TrainingPokemon.cs            # + DisplaySpecies
├── Components/
│   ├── Shared/PokemonSprite.razor          # novo: caixa fixa, selo, img lazy, onload/onerror
│   ├── Evs/OpponentCard.razor              # + sprite à esquerda do nome
│   ├── Settings/SpeciesPicker.razor        # + sprite nas opções e na linha
│   └── Shell/PokemonMenu.razor             # + sprite por Pokémon e rodapé de créditos
├── Localization/strings.pt-BR.json         # + Sprite.Dex, Menu.Credits
└── wwwroot/
    ├── sprites/{1..386}.png, CREDITS.md    # novos (gerados)
    └── css/app.css                         # + .sprite, .badge, fade, pixelated

tests/EvTrainer.Tests/
├── Domain/SpriteBadgeTests.cs              # novo
├── Components/Shared/PokemonSpriteTests.cs # novo
└── (OpponentCard/SpeciesPicker/PokemonMenu tests ganham asserts de sprite)
```

**Structure Decision**: mesma solução de 2 projetos da feature 001; o componente compartilhado vai
para `Components/Shared/` e a regra do selo para `Domain/` (C# puro, sem Blazor).

## Complexity Tracking

Sem violações da constituição a justificar.
