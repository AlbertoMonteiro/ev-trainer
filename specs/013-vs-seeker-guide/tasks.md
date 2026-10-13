# Tasks: Guia de treino com VS Seeker

**Input**: Design documents from `/specs/013-vs-seeker-guide/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/vs-seeker-guide-json.md,
contracts/training-guide-ui.md, quickstart.md

**Tests**: obrigatórios (Princípio III — TDD): cada teste é escrito e visto falhar antes do código.

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Setup (dados)

**Purpose**: gerar e embutir `vs-seeker-guide.json` a partir da decompilação.

- [X] T001 Criar `scripts/BuildVsSeekerGuide.cs` (script de arquivo único, `#:property` como `scripts/BuildPokedex.cs`; rodar com `dotnet run scripts/BuildVsSeekerGuide.cs` na raiz): baixar de `https://raw.githubusercontent.com/pret/pokefirered/<commit fixo>/` os arquivos `src/vs_seeker.c`, `src/data/trainers.h`, `src/data/trainer_parties.h`; ler `sRematches` (`{ {ids…}, MAP(MAP_X) }`), para cada id não-`SKIP` e diferente do anterior criar um estágio com `index` = posição; ler classe, nome, `doubleBattle` e party de `trainers.h`; ler `lvl`/`species` de `trainer_parties.h` aceitando structs com e sem `heldItem`/`moves` (campos em qualquer ordem); só mapas de Kanto (`MAP_ROUTE\d+`; descartar ilhas/Sevault/etc.); `location` = "Rota N"; `access`: Rotas 19/20/21 → `surf`, Rota 17 → `cyclingRoad`, Rota 23 → `victoryRoad`; classe `TRAINER_CLASS_COOLTRAINER` → "Cooltrainer" (título por palavra, `_` → espaço; `RS_` removido se houver); nome capitalizado ("BERNIE" → "Bernie", "ELI & ANNE" → "Eli & Anne"); espécie do decomp → nome da Pokédex (`pokedex-gen3.json`: normalizar `NIDORAN_F`→`Nidoran♀`, `NIDORAN_M`→`Nidoran♂`, `MR_MIME`→`Mr. Mime`, `FARFETCHD`→`Farfetch'd`, demais por comparação sem pontuação/caixa) — falhar o script se alguma espécie não casar; descartar estágios com time idêntico ao anterior; gravar `vs-seeker-guide.json` no formato de `contracts/vs-seeker-guide-json.md` (com `_fonte` citando o commit)
- [X] T002 Rodar o gerador e versionar `vs-seeker-guide.json` na raiz; conferir à mão: Engineer Bernie (Rota 11) estágios 0 (Magnemite 18 ×2, Magneton 18) e 3 (Magneton 28 ×3); Lass Julia (Rota 8) só estágio 0; Twins Eli & Anne com `double: true`
- [X] T003 Embutir em `src/EvTrainer.Web/EvTrainer.Web.csproj`: `<EmbeddedResource Include="../../vs-seeker-guide.json" LogicalName="EvTrainer.vs-seeker-guide.json" />`; acrescentar em `reference-data.en.json` o nome em inglês de qualquer `location` nova que ainda não tenha tradução (ex.: "Rota 1"→"Route 1"…)

## Phase 2: Foundational

- [X] T004 [P] Criar `tests/EvTrainer.Tests/Domain/VsSeekerGuideDataTests.cs`: carrega o embutido; ≥ 150 treinadores; ids únicos; todo `location` é "Rota N" e tem tradução em `LocationNames`; toda espécie existe no `Pokedex`; `stages` em ordem crescente de `index`, 0 ≤ index ≤ 5, time com 1–6 Pokémon e níveis 1–100; `access` ⊆ {surf, cyclingRoad, victoryRoad}; casos-âncora do T002 (Bernie, Julia, Twins)
- [X] T005 Criar `src/EvTrainer.Web/Domain/Reference/VsSeekerGuide.cs`: records `GuideTrainer(Id, TrainerClass, Name, Location, Double, Access, Stages)` e `GuideStage(Index, Team: IReadOnlyList<(string Species, int Level)>)` com `RequiresEliteFour => Index >= 4`; `LoadEmbedded()`; registrar singleton em `src/EvTrainer.Web/Program.cs` e em `tests/EvTrainer.Tests/Support/AppTestContext.cs`. T004 verde
- [X] T006 [P] Em `tests/EvTrainer.Tests/Domain/ModelJsonTests.cs` e `tests/EvTrainer.Tests/State/TrainerStoreTests.cs`: `UiPreferences.GuidePhase` padrão `"before"`, ida e volta, valor desconhecido lido como `"before"`; `Store.SetGuidePhase("after")` grava e notifica
- [X] T007 `UiPreferences.GuidePhase` ("before" | "after", padrão "before") em `src/EvTrainer.Web/Domain/Model/TrainingPokemon.cs` e `Store.SetGuidePhase` em `src/EvTrainer.Web/State/TrainerStore.cs`. T006 verde

## Phase 3: User Story 1 — Ver onde treinar cada atributo (P1) 🎯 MVP

**Independent Test**: HP → Twins Eli & Anne e Lass Julia como entradas separadas com 4 HP; Sp. Atk → Engineer Bernie no topo.

- [X] T008 [P] [US1] Criar `tests/EvTrainer.Tests/Domain/TrainingGuideTests.cs`: `TrainingGuide.StageEvs(stage)` = soma dos `evYield` da Pokédex (ex.: Bernie estágio 3 = [0,0,0,6,0,0]); `Rank(stat, phase)` com dados reais: Sp. Atk/before → primeiro é `ENGINEER_BERNIE` com Score 6; HP/before → contém `LASS_JULIA` (4) e a entrada das Twins (4) separadas; só entra quem tem estágio visível com o atributo > 0; desempate por menor nível máximo do melhor estágio; Speed/before não vazio; nenhuma entrada agrega mais de um treinador
- [X] T009 [US1] Implementar `src/EvTrainer.Web/Domain/TrainingGuide.cs` (`StageEvs`, `Rank(int stat, string phase) → IReadOnlyList<GuideEntry>`, `GuideEntry(Trainer, VisibleStages, Best, Score)`), usando `VsSeekerGuide` e `Pokedex`. T008 verde
- [X] T010 [P] [US1] Adicionar as chaves de `contracts/training-guide-ui.md` (Menu.Guide, Guide.*) em `src/EvTrainer.Web/Localization/strings.pt-BR.json` e `strings.en.json`
- [X] T011 [P] [US1] Criar `tests/EvTrainer.Tests/Components/Guide/TrainingGuideViewTests.cs`: abre pelo menu (`button.guide`) e pelo `button.open-guide` da aba EVs; `.guide-stat button[data-stat]` HP/ATK/SPA/SPE; escolher SPA → primeiro `li.guide-entry[data-trainer=ENGINEER_BERNIE]`; cada entrada com `.who`, `.where` (local no idioma ativo), `.stage` com `.team` e `.evs .main`; outros atributos em `.evs .also`; marca `.double` nas Twins; `.access-surf` para Rotas 19–21; `.guide-tips` e `.guide-sources` presentes; `button.close-guide` volta às abas
- [X] T012 [US1] Criar `src/EvTrainer.Web/Components/Guide/TrainingGuideView.razor` conforme `contracts/training-guide-ui.md`; item `button.guide` em `src/EvTrainer.Web/Components/Shell/PokemonMenu.razor` (+ `OnGuide`), repasse em `src/EvTrainer.Web/Components/Shell/AppHeader.razor`, estado `_guiding` em `src/EvTrainer.Web/Pages/Home.razor`, `button.open-guide` em `src/EvTrainer.Web/Components/Evs/EvTab.razor` (callback até o `Home`); CSS `.guide-*` em `src/EvTrainer.Web/wwwroot/css/app.css` (cartões em 1 coluna no celular, 2 no desktop; 44 px nos botões). T011 verde

## Phase 4: User Story 2 — Estágios das revanches, antes/depois da Elite Four (P1)

**Independent Test**: Bernie mostra "Original" e "Após Fuchsia"; Gia & Jes sem nv 55 em "antes", com nv 55 marcado em "depois".

- [X] T013 [P] [US2] Em `TrainingGuideTests.cs`: phase "before" esconde estágios com `Index >= 4`; "after" mostra todos; `Best` = estágio visível com mais EV do atributo; Young Couple Gia & Jes HP/before → melhor estágio sem Nidoqueen nv 55; HP/after → melhor estágio Nidoqueen nv 55
- [X] T014 [US2] Ajustar `src/EvTrainer.Web/Domain/TrainingGuide.cs` para a fase (se ainda não coberto em T009). T013 verde
- [X] T015 [P] [US2] Em `TrainingGuideViewTests.cs`: `.guide-phase button[data-phase]` com `aria-pressed`; estágios em ordem com `.label` = `Guide.Stage.{index}`; `.stage.best` no melhor; `.requires-e4` nos estágios 4–5 em "after"; trocar a fase chama `SetGuidePhase` e reordena; a fase salva é usada ao abrir
- [X] T016 [US2] Implementar seletor de fase e marcação de estágios em `src/EvTrainer.Web/Components/Guide/TrainingGuideView.razor`. T015 verde

## Phase 5: User Story 3 — Começar pelo que o Pokémon precisa (P2)

**Independent Test**: Pokémon só com meta incompleta em Sp. Atk → página abre em SPA.

- [X] T017 [P] [US3] Em `TrainingGuideTests.cs`: `InitialStat(totals, targets)` → maior (meta − atual) > 0 entre HP/Atk/SpA/Spe; nenhuma → HP; `WastedStats(stage, totals, targets)` → atributos com EV no estágio e meta atingida (meta > 0 e atual ≥ meta, ou meta 0)
- [X] T018 [US3] Implementar `InitialStat` e `WastedStats` em `src/EvTrainer.Web/Domain/TrainingGuide.cs`. T017 verde
- [X] T019 [P] [US3] Em `TrainingGuideViewTests.cs`: abre com o atributo inicial do Pokémon selecionado; sem Pokémon → HP; aviso `.waste` com `Guide.Waste` quando o melhor estágio dá atributo com meta completa
- [X] T020 [US3] Aplicar atributo inicial e aviso em `src/EvTrainer.Web/Components/Guide/TrainingGuideView.razor`. T019 verde

## Phase 6: Polish

- [X] T021 [P] `tests/EvTrainer.Tests/Localization/NoHardcodedTextTests.cs` e `EnglishScreensTests.cs`: página do guia (nomes de treinadores/classes e locais em inglês entram como dados permitidos) sem texto fixo e sem português em inglês
- [X] T022 `dotnet build PokemonEvTrainer.slnx --no-incremental` sem avisos, `dotnet test --solution PokemonEvTrainer.slnx`, `dotnet format PokemonEvTrainer.slnx --verify-no-changes`
- [X] T023 Validação no navegador conforme `quickstart.md` (320 e 1366 px, pt/en, antes/depois)
- [X] T024 [P] `README.md`: bullet "Where to train" e o comando do gerador na seção de scripts

## Dependencies

- Setup (T001→T002→T003) → Foundational (T004→T005; T006→T007) → US1 (T008→T009; T010; T011→T012) → US2 (T013→T014; T015→T016) → US3 (T017→T018; T019→T020) → Polish.

## Parallel Example

```text
# Foundational: T004 VsSeekerGuideDataTests.cs  |  T006 ModelJsonTests/TrainerStoreTests
# US1:          T008 TrainingGuideTests.cs  |  T010 strings  |  T011 TrainingGuideViewTests.cs
```

## Implementation Strategy

MVP = Setup + Foundational + US1 + US2 (as duas P1: lista por atributo e estágios por fase). US3 (P2) entra no mesmo
commit da feature se nada bloquear, como nas anteriores.
