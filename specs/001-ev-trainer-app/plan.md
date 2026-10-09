# Implementation Plan: EVs Trainer — reconstrução no browser

**Branch**: `001-ev-trainer-app` | **Date**: 2026-10-09 (rev. .NET 11 + idiomas) | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-ev-trainer-app/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Reconstruir o EVs Trainer (HANDOFF.md) como app Blazor WebAssembly standalone em **.NET 11 /
C# 15**, instalável e offline, publicado no GitHub Pages
(`https://albertomonteiro.github.io/ev-trainer/`). O jogador cadastra Pokémon, registra batalhas com
um toque (EVs pelas regras da Gen 3), busca adversários, estima IVs e leva os dados entre aparelhos
por backup JSON. As regras ficam num domínio C# puro (`Domain/`) dentro do projeto Web, feito
test-first contra o HANDOFF §6. A interface sai em pt-BR, com todos os textos em recursos de idioma
para o inglês entrar depois sem retrabalho. Todo scaffolding, pacote e formatação passa pela CLI do
.NET.

## Technical Context

**Language/Version**: C# 15 / .NET 11 (SDK `11.0.100-rc.1.26425.128`, go-live; GA prevista para
10/11/2026), fixado em `global.json` com `rollForward: latestFeature`.

**Primary Dependencies**: Blazor WebAssembly standalone (template `--pwa --empty` do .NET 11, com
o servidor de dev `Microsoft.AspNetCore.Components.Gateway`), `System.Text.Json` source generator,
`Microsoft.Extensions.Localization.Abstractions` (interface `IStringLocalizer`). Nenhum pacote de
UI ou JS de terceiros.

**Storage**: `localStorage` (`evtrainer:v1:data`, `evtrainer:v1:prefs`) atrás de
`IPokemonRepository`; backup em arquivo JSON exportado/importado pelo usuário.

**Testing**: xUnit v3 na Microsoft Testing Platform + bUnit 2.x, no único projeto `EvTrainer.Tests`.

**Target Platform**: navegadores modernos com WebAssembly (foco em celular Android/iOS);
hospedagem estática no GitHub Pages em `/ev-trainer/`.

**Project Type**: SPA web estática (PWA), sem backend.

**Performance Goals**: ganho visível < 1 s após o toque (SC-001); busca/filtro a cada tecla sem
atraso perceptível (157 adversários, 34 locais).

**Constraints**: 100% offline após o primeiro acesso; sem requisições a terceiros; trimming e
`InvariantGlobalization` ligados; 360 px sem rolagem horizontal; exatamente 2 projetos; nenhum
texto de UI fixo em componentes; scaffolding só pela CLI.

**Scale/Scope**: 1 usuário por aparelho, dezenas de Pokémon, milhares de eventos (< 1 MB); 1 rota,
3 abas, ~15 componentes; 1 idioma (pt-BR) com estrutura para N.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.* Constituição **v1.1.0**
(emendada nesta revisão: .NET 11, Princípio VII de idiomas, "dotnet CLI primeiro", ordem do
deploy).

| Princípio / Seção | Verificação | Pré | Pós |
|---|---|---|---|
| I. Estático no browser | Blazor WASM standalone; sem servidor; base href do repo antes do publish, `404.html`, `.nojekyll`; offline via service worker (R1, R2). | ✅ | ✅ |
| II. Dois projetos | `src/EvTrainer.Web` + `tests/EvTrainer.Tests`; domínio em `Domain/` sem Blazor/JS/IO ([domain-api](./contracts/domain-api.md)); Gateway é pacote de dev, não projeto. | ✅ | ✅ |
| III. Test-First | HANDOFF §6 + SC-003/SC-004/SC-010 viram testes antes do código; CI bloqueia deploy (R8). | ✅ | ✅ |
| IV. Fidelidade Gen 3 | Fórmula inteira, limites, vitaminas, `m` por evento, totais derivados, aliases só na leitura, dados embutidos ([data-model](./data-model.md)). | ✅ | ✅ |
| V. Dados locais | `localStorage` + backup no formato do `user-data-export.json`; sem conta/telemetria; fontes locais ([backup-format](./contracts/backup-format.md)). | ✅ | ✅ |
| VI. Mobile-first | Coluna ≤ 560 px, toque único, tokens claro/escuro, movimento reduzido, `inputmode`, `aria-label`, PWA ([ui-shell](./contracts/ui-shell.md)). | ✅ | ✅ |
| VII. Outros idiomas | `IStringLocalizer` + `strings.pt-BR.json`; domínio sem texto; testes de paridade e de texto fixo; `lang` e escolha persistidos ([localization](./contracts/localization.md), R14). | ✅ | ✅ |
| Restrições técnicas | .NET 11/C# 15 RC fixado; novidades adotadas só quando simplificam e sem flag de preview (R13); JS só em `interop.js`; STJ source gen; pacotes justificados: bUnit, `Localization.Abstractions` (R8, R14). | ✅ | ✅ |
| Fluxo e deploy | `dotnet format --verify-no-changes` + `dotnet test` → base href → publish → 404/.nojekyll → Pages. | ✅ | ✅ |
| dotnet CLI primeiro | Seção "Scaffolding pela CLI" abaixo lista o comando de cada passo; CPM descartado por exigir edição manual (R1). | ✅ | ✅ |

Resultado: **sem violações**; Complexity Tracking vazio.

Desvios conscientes do HANDOFF (não da constituição): sem nuvem; gravação imediata em vez de
debounce de 450 ms (R4); "Tela cheia" → PWA; indicador de status só com estados locais.

## Scaffolding pela CLI

Ordem a ser seguida pelas tarefas de setup (constituição, "dotnet CLI primeiro"). Edição manual só
onde não há comando, indicada como tal.

```bash
dotnet new globaljson --sdk-version 11.0.100-rc.1.26425.128 --roll-forward latestFeature --test-runner Microsoft.Testing.Platform
dotnet new buildprops
dotnet new editorconfig
dotnet new blazorwasm --pwa --empty -f net11.0 -n EvTrainer.Web -o src/EvTrainer.Web
dotnet new xunit --xunit-version v3 --test-runner Microsoft.Testing.Platform -f net11.0 -n EvTrainer.Tests -o tests/EvTrainer.Tests
dotnet sln PokemonEvTrainer.slnx add src/EvTrainer.Web tests/EvTrainer.Tests
dotnet add tests/EvTrainer.Tests reference src/EvTrainer.Web
dotnet package add Microsoft.Extensions.Localization.Abstractions --prerelease --project src/EvTrainer.Web
dotnet package add bunit --project tests/EvTrainer.Tests
dotnet workload install wasm-tools
dotnet format PokemonEvTrainer.slnx
```

O `.gitignore` já existe (gerado por `dotnet new gitignore`). Edições manuais previstas, por não
terem comando: `TreatWarningsAsErrors` no `Directory.Build.props`; `InvariantGlobalization`,
`EmbeddedResource` (link para `reference-data.json` e `Localization/*.json`) no `.csproj` do Web;
`None`/`CopyToOutputDirectory` do `user-data-export.json` no `.csproj` de testes; conteúdo de
`wwwroot`; workflow do GitHub Actions.

## Project Structure

### Documentation (this feature)

```text
specs/001-ev-trainer-app/
├── plan.md              # Este arquivo
├── research.md          # Fase 0 (R1–R14)
├── data-model.md        # Fase 1
├── quickstart.md        # Fase 1
├── contracts/
│   ├── backup-format.md # Formato de backup e de armazenamento
│   ├── domain-api.md    # API pública do domínio (C# 15: closed/union)
│   ├── localization.md  # Recursos de idioma, chaves e testes
│   └── ui-shell.md      # UI, repositório e interop
├── checklists/
│   └── requirements.md
└── tasks.md             # Fase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
PokemonEvTrainer.slnx
global.json                           # SDK 11 RC, latestFeature, MTP
Directory.Build.props                 # TreatWarningsAsErrors
.editorconfig                         # base do dotnet format
.github/workflows/deploy.yml          # format + test → base href → publish → Pages
reference-data.json                   # embutido por link
user-data-export.json                 # fixture de testes por link
HANDOFF.md, app-atual.html            # referência

src/EvTrainer.Web/                    # projeto 1: Blazor WebAssembly (PWA)
├── EvTrainer.Web.csproj              # net11.0, InvariantGlobalization, EmbeddedResource
├── Program.cs                        # DI: ReferenceData, IPokemonRepository, TrainerStore, ToastService, localização
├── App.razor                         # rota única
├── Domain/                           # C# puro — sem Blazor/JS/IO/texto de UI
│   ├── Model/                        # TrainingPokemon, SpeciesEntry, BattleEvent, LevelEntry, UiPreferences
│   ├── Reference/                    # ReferenceData, Opponent, Nature, TrainerLocation
│   ├── Evs.cs, BattleLog.cs          # §4.1–4.4, 4.6
│   ├── Stats.cs, Ivs.cs              # §2.2, 4.7, 4.8 (IvCell/FinalCell fechados)
│   ├── TextFold.cs, RouteExpander.cs, OpponentSearch.cs   # §4.5
│   ├── SpreadsheetImport.cs          # §4.9
│   ├── BackupFormat.cs               # union BackupReadResult
│   └── Json/EvTrainerJsonContext.cs
├── Localization/
│   ├── strings.pt-BR.json            # único idioma nesta versão
│   ├── JsonStringLocalizer.cs        # IStringLocalizer sobre JSON embutido
│   └── LanguageState.cs              # idioma ativo, disponíveis, persistência
├── State/
│   ├── IPokemonRepository.cs, LocalStorageRepository.cs
│   ├── TrainerStore.cs, ToastService.cs
├── Components/
│   ├── Shell/                        # AppHeader, PokemonMenu, StatusIndicator, Tabs, Toast, EmptyState
│   ├── Evs/                          # EvSummary, OpponentSearchBar, OpponentGrid, OpponentCard, RecentHistory
│   ├── Ivs/                          # IvTable, LevelForm
│   └── Settings/                     # SettingsForm, SpeciesEditor, SpreadsheetImportPanel, BackupPanel, DeletePanel
├── Pages/Home.razor
└── wwwroot/                          # index.html (base href, tema/lang), manifest, service workers,
                                      # css/app.css, fonts/*.woff2, js/interop.js, ícones

tests/EvTrainer.Tests/                # projeto 2: xUnit v3 (MTP) + bUnit
├── Domain/                           # EvTotals, Ivs, Search, UsefulFilter, BattleLog,
│                                     # SpreadsheetImport, BackupFormat
├── Components/                       # Home, OpponentCard, Menu, IvTable, Settings, BackupPanel, Status
├── Localization/                     # paridade de chaves, chaves usadas, sem texto fixo, troca ao vivo
└── Support/                          # InMemoryRepository, builders, idioma de teste sintético
```

**Structure Decision**: dois projetos (Princípio II). Domínio isolado em `Domain/`, textos em
`Localization/`, UI em `Components/` + `State/`; testes espelham essas pastas. Arquivos do HANDOFF
permanecem na raiz e entram no build por link.

## Complexity Tracking

Sem violações da constituição a justificar.
