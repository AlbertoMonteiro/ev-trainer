---

description: "Task list for EVs Trainer — reconstrução no browser (.NET 11 Blazor WASM)"
---

# Tasks: EVs Trainer — reconstrução no browser

**Input**: Design documents from `/specs/001-ev-trainer-app/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: OBRIGATÓRIOS. A constituição (Princípio III, inegociável) exige TDD: cada tarefa de teste
é escrita, executada com `dotnet test --solution PokemonEvTrainer.slnx` e **precisa falhar** antes
da tarefa de implementação correspondente. Depois de implementar, rodar de novo e ficar verde.

**Organization**: tarefas agrupadas por história de usuário (US1–US6 da spec, em ordem de
prioridade), cada uma testável de forma independente.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência de tarefa incompleta)
- **[Story]**: história de usuário da tarefa (US1…US6)

## Convenções válidas para todas as tarefas

- **dotnet CLI primeiro** (constituição): criar projetos/arquivos de config com `dotnet new`,
  pacotes com `dotnet package add`, referências com `dotnet add reference`, solução com
  `dotnet sln`. Nunca escrever `.csproj`, `.slnx`, `global.json` ou `Directory.Build.props` do
  zero. Ao final de cada tarefa que altera código: `dotnet format PokemonEvTrainer.slnx`.
- **Sem texto fixo** (Princípio VII): todo texto visível (incluindo `aria-label`, `title`,
  placeholders) vem de `IStringLocalizer` com chave em
  `src/EvTrainer.Web/Localization/strings.pt-BR.json` (convenção em
  [contracts/localization.md](./contracts/localization.md)). Ao criar um componente, adicionar as
  chaves dele nesse arquivo (edição sequencial: não paralelizar duas tarefas que mexem nele).
- **Domínio puro**: nada em `src/EvTrainer.Web/Domain/` referencia Blazor, `IJSRuntime`,
  `HttpClient`, armazenamento ou textos de UI.
- Namespaces: `EvTrainer.Web.Domain`, `EvTrainer.Web.Localization`, `EvTrainer.Web.State`,
  `EvTrainer.Web.Components.*`; testes em `EvTrainer.Tests.*`.
- Vetores de stats sempre `int[6]` na ordem `[HP, Ataque, Defesa, SpAtk, SpDef, Speed]`.
- Testes de componentes usam bUnit 2.x (`BunitContext`) com `JSInterop.Mode = JSRuntimeMode.Strict`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: scaffolding inteiro pela CLI, solução compilando e testes rodando no CI.

- [X] T001 Criar `global.json` na raiz com `dotnet new globaljson --sdk-version 11.0.100-rc.1.26425.128 --roll-forward latestFeature --test-runner Microsoft.Testing.Platform`
- [X] T002 [P] Criar `Directory.Build.props` na raiz com `dotnet new buildprops` e depois (edição manual permitida, sem comando equivalente) adicionar `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` e `<LangVersion>latest</LangVersion>` num `PropertyGroup`
- [X] T003 [P] Criar `.editorconfig` na raiz com `dotnet new editorconfig`
- [X] T004 Criar o projeto web com `dotnet new blazorwasm --pwa --empty -f net11.0 -n EvTrainer.Web -o src/EvTrainer.Web` (gera `src/EvTrainer.Web/EvTrainer.Web.csproj`, `wwwroot/manifest.webmanifest`, `service-worker*.js`, pacote `Microsoft.AspNetCore.Components.Gateway`)
- [X] T005 Criar o projeto de testes com `dotnet new xunit --xunit-version v3 --test-runner Microsoft.Testing.Platform -f net11.0 -n EvTrainer.Tests -o tests/EvTrainer.Tests` e apagar o `UnitTest1.cs` gerado
- [X] T006 Adicionar os dois projetos à solução existente com `dotnet sln PokemonEvTrainer.slnx add src/EvTrainer.Web tests/EvTrainer.Tests` e a referência com `dotnet add tests/EvTrainer.Tests reference src/EvTrainer.Web`
- [X] T007 Adicionar pacotes: `dotnet package add Microsoft.Extensions.Localization.Abstractions --prerelease --project src/EvTrainer.Web` e `dotnet package add bunit --project tests/EvTrainer.Tests`
- [X] T008 Ajustar `src/EvTrainer.Web/EvTrainer.Web.csproj` (edição manual, sem comando equivalente): `<InvariantGlobalization>true</InvariantGlobalization>`; `<EmbeddedResource Include="../../reference-data.json" LogicalName="EvTrainer.reference-data.json" />`; `<EmbeddedResource Include="Localization/strings.*.json" LogicalName="EvTrainer.%(Filename)%(Extension)" />`; `<InternalsVisibleTo Include="EvTrainer.Tests" />`
- [X] T009 Ajustar `tests/EvTrainer.Tests/EvTrainer.Tests.csproj` (edição manual): `<None Include="../../user-data-export.json" Link="Fixtures/user-data-export.json" CopyToOutputDirectory="PreserveNewest" />`
- [X] T010 Instalar o workload com `dotnet workload install wasm-tools` e validar com `dotnet build PokemonEvTrainer.slnx` e `dotnet test --solution PokemonEvTrainer.slnx` (zero testes, build verde)
- [X] T011 Limpar o template em `src/EvTrainer.Web/wwwroot/index.html`: `<html lang="pt-BR">`, título `EVs Trainer`, manter `<base href="/" />`, `<link rel="preload" id="webassembly" />`, import map e o registro do service worker; remover referências a Bootstrap/arquivos de exemplo
- [X] T012 Criar o workflow `.github/workflows/deploy.yml`: gatilho `push` na `main` + `workflow_dispatch`; permissões `contents: read`, `pages: write`, `id-token: write`; job `build`: `actions/checkout`, `actions/setup-dotnet` com `global-json-file: global.json`, `dotnet workload install wasm-tools`, `dotnet format PokemonEvTrainer.slnx --verify-no-changes`, `dotnet test --solution PokemonEvTrainer.slnx`, `sed -i 's|<base href="/" />|<base href="/${{ github.event.repository.name }}/" />|' src/EvTrainer.Web/wwwroot/index.html` (ANTES do publish, ver research R2), `dotnet publish src/EvTrainer.Web -c Release -o artifacts/publish`, `cp artifacts/publish/wwwroot/index.html artifacts/publish/wwwroot/404.html`, `touch artifacts/publish/wwwroot/.nojekyll`, `actions/configure-pages`, `actions/upload-pages-artifact` com `path: artifacts/publish/wwwroot`; job `deploy` (`needs: build`, environment `github-pages`) com `actions/deploy-pages`
- [X] T013 Rodar `dotnet format PokemonEvTrainer.slnx` e confirmar `dotnet format PokemonEvTrainer.slnx --verify-no-changes` limpo

**Checkpoint**: solução com 2 projetos compila, testes rodam (vazios), CI pronto.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: modelo, dados de referência, JSON, localização, repositório, store e casca da UI —
pré-requisitos de todas as histórias.

**⚠️ CRITICAL**: nenhuma história começa antes desta fase terminar.

### Testes da fundação (escrever primeiro, ver falhar)

- [X] T014 [P] Teste `tests/EvTrainer.Tests/Domain/ReferenceDataTests.cs`: `ReferenceData.LoadEmbedded()` carrega 157 adversários (151 com `Dex` 1–151 + 6 vitaminas com `Dex == null` e `IsVitamin == true`: HP Up, Protein, Iron, Calcium, Zinc, Carbos), 25 natures (5 neutras: Hardy, Docile, Serious, Bashful, Quirky), 34 locais em `TrainersByLocation` na ordem do arquivo (primeiro "Laboratório do Prof. Oak"), `Canon("Bulbasaur (rival)") == "Bulbasaur"`, `Canon("Nidoran F") == "Nidoran♀"`, `Canon("Nidoran M") == "Nidoran♂"`, `Canon("Desconhecido") == "Desconhecido"`, `Lookup("Nidoran F")!.Name == "Nidoran♀"`, `FindNature("")` e `FindNature("Xyz")` retornam `null`
- [X] T015 [P] Teste `tests/EvTrainer.Tests/Domain/NatureTests.cs`: `Nature.Multiplier(statIndex)` devolve 11 para o stat aumentado, 9 para o diminuído, 10 para os demais e **sempre 10 para HP (índice 0)**; Timid ⇒ Speed 11, Ataque 9; natures neutras ⇒ 10 em tudo
- [X] T016 [P] Teste `tests/EvTrainer.Tests/Domain/ModelJsonTests.cs`: serializar/desserializar `TrainingPokemon` com `EvTrainerJsonContext` usa exatamente os nomes `name, nature, species[name, base], targets, events[o, n, m], ivLog[lv, sp, st, ev], pokerus, macho, order`; leitura tolerante: propriedade desconhecida ignorada, `n` ausente ou < 1 ⇒ 1, `m` ausente ou fora de {1, 2, 4} ⇒ 1, vetores com tamanho ≠ 6 completam com 0 / truncam em 6
- [X] T017 [P] Teste `tests/EvTrainer.Tests/Localization/JsonStringLocalizerTests.cs`: com dicionários em memória, `L["Chave"]` devolve o texto do idioma ativo; argumentos `{0}` formatados com `CultureInfo.InvariantCulture`; chave ausente no idioma ativo cai para `pt-BR`; ausente em todos ⇒ `[Chave]` com `ResourceNotFound == true`; `LanguageState.Available` contém só `pt-BR` quando só existe `strings.pt-BR.json` embutido; trocar `LanguageState.Current` dispara `Changed`
- [X] T018 [P] Teste `tests/EvTrainer.Tests/Localization/KeyParityTests.cs`: todo recurso embutido `EvTrainer.strings.*.json` tem exatamente o mesmo conjunto de chaves e os mesmos placeholders `{n}` do `strings.pt-BR.json`; e toda chave literal usada como `L["..."]` em arquivos `.razor`/`.cs` de `src/EvTrainer.Web` (varredura dos fontes a partir do diretório da solução) existe no `pt-BR`
- [X] T019 [P] Teste `tests/EvTrainer.Tests/State/TrainerStoreTests.cs` (com `tests/EvTrainer.Tests/Support/InMemoryRepository.cs`): store vazio carrega do repositório; `AddPokemon()` cria Pokémon com id `Guid.CreateVersion7().ToString("N")`, 1 espécie sem nome, metas `[0,0,0,0,0,0]`, `Order` = maior + 1, fica selecionado e chama `SaveAll` imediatamente; `SaveAll` retornando `Failed` põe `Status == SaveStatus.Failed` sem perder o estado em memória; `Changed` dispara a cada comando; preferências (`SelectedId`, `Theme`, `Filter`, `Language`) são lidas/gravadas via `LoadPreferences`/`SavePreferences`; `SelectedId` inválido ⇒ primeiro por `Order`
- [X] T020 [P] Teste `tests/EvTrainer.Tests/Components/Shell/AppShellTests.cs` (bUnit): sem Pokémon, renderiza o estado vazio com a chave `Empty.NoPokemon` ("Nenhum Pokémon ainda.") e botões `Empty.AddPokemon` e `Menu.ImportBackup`; header tem botão menu com `aria-label` da chave `Menu.Open`, título `App.Title`, indicador de status, botões Pokérus/Macho Brace com `aria-pressed`; abas EVs · IVs · Ajustes
- [X] T021 [P] Teste `tests/EvTrainer.Tests/Components/Shell/StatusIndicatorTests.cs` (bUnit): `SaveStatus.Saved` ⇒ "📱" com `aria-label`/`title` da chave `Status.Saved` ("Salvo neste aparelho"); `Saving` ⇒ "🔄" `Status.Saving`; `Failed` ⇒ "⚠️" `Status.Failed`; `role="status"`
- [X] T022 [P] Teste `tests/EvTrainer.Tests/Components/Shell/ToastTests.cs` (bUnit): `ToastService.Show(message, undo)` exibe a mensagem e o botão `Toast.Undo`; tocar "Desfazer" executa a ação e fecha; sem `undo` não há botão; some após 4 s (usar `TimeProvider` falso injetado no `ToastService`)

### Implementação da fundação

- [X] T023 [P] Criar o modelo em `src/EvTrainer.Web/Domain/Model/`: `TrainingPokemon.cs` (Name, Nature `""` = neutra, `List<SpeciesEntry>` "1 a 3 entradas", Targets int[6] "Cada valor 0–255", `List<BattleEvent>` "Ordem é significativa; nunca reordenar", `List<LevelEntry>` "Mantida ordenada por Level (estável)", Pokerus, MachoBrace, Order), `SpeciesEntry.cs` (Name, Base int[6] "Cada valor 1–255"), `BattleEvent.cs` (Opponent, Count "≥ 1", Multiplier "1, 2 ou 4"), `LevelEntry.cs` (Level "1–100", Species, Stats int[6], Evs int[6] "0–255 cada"), `UiPreferences.cs` (SelectedId, Theme `system|light|dark`, Filter `useful|all`, Language padrão `pt-BR`) e `StatVectorExtensions.cs` com extension members C# 14 (`Sum()`, cópia segura) — conforme [data-model.md](./data-model.md)
- [X] T024 [P] Criar `src/EvTrainer.Web/Domain/Reference/` (`ReferenceData.cs`, `Opponent.cs`, `Nature.cs`, `TrainerLocation.cs`) com `LoadEmbedded()` lendo o recurso `EvTrainer.reference-data.json` (ignorar `_fonte`), `Canon`, `Lookup`, `FindNature`, `Nature.Multiplier` (natures usam nomes completos `"Ataque"`, `"Defesa"`, `"SpAtk"`, `"SpDef"`, `"Speed"` mapeados por `stats[]`) — faz T014 e T015 passarem
- [X] T025 Criar `src/EvTrainer.Web/Domain/Json/EvTrainerJsonContext.cs` (source generator STJ) e os conversores tolerantes de leitura descritos em [contracts/backup-format.md](./contracts/backup-format.md) — faz T016 passar
- [X] T026 [P] Criar `src/EvTrainer.Web/Localization/JsonStringLocalizer.cs` (implementa `IStringLocalizer` e `IStringLocalizer<T>` via fábrica), `LanguageState.cs` (Current, Available derivado dos recursos `EvTrainer.strings.*.json`, `Changed`) e `strings.pt-BR.json` inicial com as chaves `App.Title`, `Tabs.Evs`, `Tabs.Ivs`, `Tabs.Settings`, `Menu.Open`, `Menu.AddPokemon`, `Menu.ImportBackup`, `Empty.NoPokemon`, `Empty.AddPokemon`, `Status.Saved`, `Status.Saving`, `Status.Failed`, `Toast.Undo`, `Stat.Short.0`…`Stat.Short.5` (`HP ATK DEF SPA SPD SPE`), `Stat.Long.0`…`Stat.Long.5` (`HP Ataque Defesa SpAtk SpDef Speed`) — faz T017 e T018 passarem
- [X] T027 [P] Criar `src/EvTrainer.Web/State/IPokemonRepository.cs` (contrato em [contracts/ui-shell.md](./contracts/ui-shell.md): `LoadAll`, `SaveAll` → `SaveResult`, `LoadPreferences`, `SavePreferences`) e `tests/EvTrainer.Tests/Support/InMemoryRepository.cs` (pode simular `Failed`)
- [X] T028 [P] Criar `src/EvTrainer.Web/wwwroot/js/interop.js` com `window.evt = { storageGet, storageSet (retorna false em exceção), downloadText(fileName, text), setTheme(theme), setLanguage(tag) }` e referenciar em `wwwroot/index.html`
- [X] T029 Criar `src/EvTrainer.Web/State/LocalStorageRepository.cs` usando `IJSInProcessRuntime` (chaves `evtrainer:v1:data` no formato de backup compacto e `evtrainer:v1:prefs`), serializando com `EvTrainerJsonContext` (depende de T025, T027, T028)
- [X] T030 Criar `src/EvTrainer.Web/State/TrainerStore.cs` (estado em memória, `Selected`, `Status` como hierarquia fechada C# 15 `closed record SaveStatus` com `Saved`/`Saving`/`Failed`, `Changed`, `AddPokemon`, seleção, gravação imediata a cada comando) e `src/EvTrainer.Web/State/ToastService.cs` (4 s, `TimeProvider` injetável, ação de desfazer opcional) — faz T019 passar
- [X] T031 Registrar serviços em `src/EvTrainer.Web/Program.cs`: `ReferenceData` (singleton via `LoadEmbedded`), `IPokemonRepository` → `LocalStorageRepository`, `TrainerStore`, `ToastService`, `TimeProvider.System`, `LanguageState`, `IStringLocalizerFactory`/`IStringLocalizer<>` → JSON; remover o `HttpClient` do template
- [X] T032 Criar a casca da UI: `src/EvTrainer.Web/App.razor` (rota única), `Pages/Home.razor` (abas como estado do componente), `Components/Shell/AppHeader.razor`, `StatusIndicator.razor`, `Tabs.razor`, `Toast.razor`, `EmptyState.razor` com `_Imports.razor` atualizado — faz T020, T021, T022 passarem
- [X] T033 [P] Criar `src/EvTrainer.Web/wwwroot/css/app.css` com os tokens claro/escuro do HANDOFF §5.7 (`--bg`, `--surface`, `--surface-2`, `--line`, `--fg`, `--muted`, `--accent`, `--accent-soft`, `--ok`, `--warn`, `--bad`, cores dos 6 stats), `prefers-color-scheme` + `:root[data-theme=light|dark]`, coluna única `max-width: 560px` com margem lateral de 16 px, header sticky, título que cai para 17 px em ≤ 400 px e 15 px em ≤ 370 px, toast fixo embaixo
- [X] T034 Script inline no `<head>` de `src/EvTrainer.Web/wwwroot/index.html` que lê `evtrainer:v1:prefs` (try/catch) e aplica `data-theme` e `lang` antes do Blazor iniciar
- [X] T035 Rodar `dotnet format PokemonEvTrainer.slnx` e `dotnet test --solution PokemonEvTrainer.slnx` (tudo verde)

**Checkpoint**: app abre com estado vazio, salva no `localStorage`, textos vêm do pt-BR.

---

## Phase 3: User Story 1 - Cadastrar um Pokémon e contar EVs com um toque (Priority: P1) 🎯 MVP

**Goal**: cadastrar Pokémon (nome, nature, linha evolutiva, metas) e registrar batalhas com um
toque, com limites da Gen 3, Pokérus/Macho Brace, aviso de ganho, desfazer e persistência local.

**Independent Test**: navegador limpo → cadastrar → tocar adversários e vitaminas → fechar/reabrir →
totais batem com as regras (spec US1, cenários 1–9).

### Tests for User Story 1 ⚠️ (escrever primeiro, ver falhar)

- [X] T036 [P] [US1] Teste `tests/EvTrainer.Tests/Domain/EvTotalsTests.cs`: eventos `Bulbasaur ×1`, `Calcium ×12`, `Caterpie ×300 (m=2)` ⇒ `[255, 0, 0, 100, 0, 0]`; `Bulbasaur (rival)`, `Geodude`, `Onix` ⇒ `[0, 0, 2, 1, 0, 0]`; vitamina com stat em 95 soma só +5; stat ≥ 100 não soma; vitamina ignora `m`; total nunca passa de 510; evento com nome desconhecido não soma e não quebra; reprocessa em ordem (vitamina depois de batalhas pode não funcionar)
- [X] T037 [P] [US1] Teste `tests/EvTrainer.Tests/Domain/BattleLogTests.cs`: `Evs.Multiplier(false,false)==1`, `(true,false)==2`, `(false,true)==2`, `(true,true)==4`; `BattleLog.Record` agrupa no último evento se mesmo nome canônico e mesmo `m` (`n++`), senão anexa `{o, 1, m}`; `RecordResult.Gain` = diferença dos totais (tudo 0 quando limite atingido); `UndoLast` decrementa `n` e remove o evento quando chega a 0; mudar Pokérus depois não altera eventos já gravados; `Evs.CountFor` soma `n` por nome canônico (aliases contam junto)
- [X] T038 [P] [US1] Teste `tests/EvTrainer.Tests/Components/Evs/EvSummaryTests.cs` (bUnit): mostra nome + chave `Evs.Summary.Title`, `total / 510 · metas N`, 6 mini cards `atual/meta`; classe de concluído quando `atual == meta > 0`, de excedido quando `atual > meta`
- [X] T039 [P] [US1] Teste `tests/EvTrainer.Tests/Components/Evs/OpponentCardTests.cs` (bUnit): card inteiro é um `<button>`; mostra nome, etiquetas `+1 SPA` (chaves `Stat.Short.*`) e `×N`; **não** mostra local nem número da Pokédex; clicar chama `TrainerStore.RecordBattle`
- [X] T040 [P] [US1] Teste `tests/EvTrainer.Tests/Components/Evs/EvTabFlowTests.cs` (bUnit, store real + `InMemoryRepository`): tocar Caterpie ⇒ HP 1, `×1`, toast "Caterpie: +1 HP" (chave `Toast.EvGain`); "Desfazer" volta a 0; Pokérus + Macho ligados ⇒ +4; stat no limite ⇒ toast `Toast.NoEvGain`; após o toque o repositório recebeu `SaveAll`; botões Pokérus/Macho alternam `aria-pressed` e exibem "×2" quando ativos
- [X] T041 [P] [US1] Teste `tests/EvTrainer.Tests/Components/Settings/SettingsFormTests.cs` (bUnit): campos nome, nature (25 opções + vazio), metas 0–255 com soma ao vivo e aviso `Settings.Targets.OverLimit` quando > 510 (salvar continua habilitado); linha evolutiva de 1 a 3 espécies com 6 stats base 1–255; valor fora da faixa desabilita "Salvar ajustes"; salvar atualiza o Pokémon no store; novo Pokémon abre direto na aba Ajustes

### Implementation for User Story 1

- [X] T042 [P] [US1] Implementar `src/EvTrainer.Web/Domain/Evs.cs` (`Totals`, `Multiplier`, `CountFor`) conforme HANDOFF §4.1 e [contracts/domain-api.md](./contracts/domain-api.md) — faz T036 passar
- [X] T043 [US1] Implementar `src/EvTrainer.Web/Domain/BattleLog.cs` (`Record` → `RecordResult(int[] Gain)`, `UndoLast`) — faz T037 passar (depende de T042)
- [X] T044 [US1] Adicionar ao `TrainerStore` os comandos `RecordBattle(opponent)` (aplica `BattleLog.Record`, grava, emite toast com ganho formatado pela UI e ação Desfazer → `UndoLast`), `TogglePokerus`, `ToggleMacho`, `SaveSettings(name, nature, targets, species)` em `src/EvTrainer.Web/State/TrainerStore.cs`
- [X] T045 [P] [US1] Criar `src/EvTrainer.Web/Components/Evs/EvSummary.razor` (+ chaves) — faz T038 passar
- [X] T046 [P] [US1] Criar `src/EvTrainer.Web/Components/Evs/OpponentCard.razor` e `OpponentGrid.razor` (grade de 2 colunas; nesta história lista todos os adversários na ordem da referência; animação "bump" no toque) (+ chaves) — faz T039 passar
- [X] T047 [P] [US1] Criar `src/EvTrainer.Web/Components/Settings/SettingsForm.razor` e `SpeciesEditor.razor` (`inputmode="numeric"` nos números; nature mostrada pelo nome) (+ chaves) — faz T041 passar
- [X] T048 [US1] Ligar aba EVs (`EvSummary` + `OpponentGrid`) e aba Ajustes (`SettingsForm`) em `src/EvTrainer.Web/Pages/Home.razor`; botões Pokérus/Macho no `AppHeader.razor`; toasts de batalha formatados com `Toast.EvGain`/`Toast.NoEvGain` e rótulos `Stat.Short.*` — faz T040 passar
- [X] T049 [US1] Estilos de cards, mini cards, barras por stat e "bump" (desligado em `prefers-reduced-motion: reduce`) em `src/EvTrainer.Web/wwwroot/css/app.css`
- [X] T050 [US1] Rodar `dotnet format PokemonEvTrainer.slnx`, `dotnet test --solution PokemonEvTrainer.slnx` e validar manualmente os passos 1–5 do [quickstart.md §2](./quickstart.md)

**Checkpoint**: MVP utilizável — cadastrar e contar EVs com persistência local.

### Correção da US1 — seletor de espécie (feedback do usuário, 2026-10-09)

Digitar linha evolutiva e stats base foi considerado inaceitável. A espécie passa a ser escolhida
numa lista, com linha evolutiva e stats base da Gen III preenchidos sozinhos (FR-001, FR-034).

- [X] T106 [US1] Gerar `pokedex-gen3.json` (#1–386, stats base da Gen III via `past_stats`, `evolvesFrom`) com o file-based app `scripts/BuildPokedex.cs` (`dotnet run scripts/BuildPokedex.cs`) a partir da PokéAPI; nomes de Kanto iguais aos do `reference-data.json`; pré-evoluções posteriores à Gen III descartadas
- [X] T107 [P] [US1] Teste `tests/EvTrainer.Tests/Domain/PokedexTests.cs`: 386 espécies; stats Gen III (Butterfree SpAtk 80, Pikachu `35 55 30 50 40 90`, Pidgeot Speed 91); `Search("squ")` encontra Squirtle, `Search("25")`/`"#25"` encontra Pikachu, acentos/♀ ignorados; `LineFor(Squirtle)` = Squirtle → Wartortle → Blastoise; `LineFor(Wartortle)` inclui o ancestral; `LineFor(Pichu)` = Pichu → Pikachu → Raichu; `LineFor(Eevee)` para em Eevee e `EvolutionsOf(Eevee)` lista as 5 eeveelutions; `LineFor(Mr. Mime)` = só Mr. Mime
- [X] T108 [P] [US1] Teste `tests/EvTrainer.Tests/Components/Settings/SpeciesPickerTests.cs` (bUnit): digitar "squir" mostra Squirtle; escolher preenche a linha Squirtle → Wartortle → Blastoise com stats base exibidos, sem campos de stats; nome vazio recebe "Squirtle"; Eevee mostra os botões de evolução e escolher Vaporeon completa a linha; "Trocar espécie" volta à busca; salvar grava `species` com nomes e bases da Pokédex
- [X] T109 [US1] Implementar `src/EvTrainer.Web/Domain/Reference/Pokedex.cs` (`LoadEmbedded`, `Find`, `Search`, `LineFor`, `EvolutionsOf`) e embutir `pokedex-gen3.json` no `.csproj` — faz T107 passar
- [X] T110 [US1] Criar `src/EvTrainer.Web/Components/Settings/SpeciesPicker.razor` e trocar o `SpeciesEditor` por ele no `SettingsForm.razor` (remover `SpeciesEditor.razor`); atualizar `SettingsFormTests` — faz T108 passar
- [X] T111 [US1] Atualizar `data-model.md`/`research.md` (entidade `PokedexSpecies`, R15) e validar no app rodando


---

## Phase 4: User Story 2 - Migrar os dados atuais e fazer backup (Priority: P2)

**Goal**: importar `user-data-export.json` e exportar/importar backups com prévia e confirmação.

**Independent Test**: importar o arquivo num navegador limpo e conferir EVs/IVs com o app atual;
exportar e importar em outro navegador (spec US2).

### Tests for User Story 2 ⚠️

- [X] T051 [P] [US2] Teste `tests/EvTrainer.Tests/Domain/BackupFormatTests.cs`: `Read` de JSON inválido ⇒ `BackupError(InvalidJson)`; `collection` ≠ `"pokemon"` ⇒ `WrongCollection`; sem `documents` ⇒ `MissingDocuments`; `Read(Write(x))` igual a `x` (SC-004); `Write` mantém `events[].o` exatamente como gravado (aliases não reescritos) e escreve todos os campos; `ivLog` fora de ordem é reordenado por `lv`; `order` ausente recebe próxima posição; `species` vazio ⇒ 1 espécie sem nome; `Preview(current, incoming)` conta `New` e `Replaced` por id. Tratar `BackupReadResult` (union C# 15) com `switch` exaustivo
- [X] T052 [P] [US2] Teste `tests/EvTrainer.Tests/Domain/LegacyExportGoldenTests.cs` (SC-003): ler `Fixtures/user-data-export.json`; documento `squirtle` ⇒ `Evs.Totals == [42, 0, 2, 33, 1, 4]` (soma 82); contagens: Bulbasaur 2, Caterpie 33, Pikachu 1, Weedle 1, Geodude 1, Onix 1, Nidoran♀ 3, Jigglypuff 2, Clefairy 1, Magnemite 1, Oddish 18, Abra 7, Pidgey 1, Ivysaur 1, Horsea 4 (valores obtidos executando o código do `app-atual.html`). As asserções de IV deste arquivo entram em T080
- [X] T053 [P] [US2] Teste `tests/EvTrainer.Tests/Components/Settings/BackupPanelTests.cs` (bUnit): "Exportar backup" chama `evt.downloadText` com nome `evs-trainer-backup-AAAA-MM-DD.json` (data de um `TimeProvider` falso) e o JSON indentado de `BackupFormat.Write`; importar via `InputFile` mostra a prévia `Backup.ImportPreview` ("N Pokémon: X novos, Y substituídos") e só grava após `Backup.ConfirmImport`; arquivo inválido mostra o erro da chave correspondente e não altera nada; exibe o texto `Backup.LocalOnlyNotice`

### Implementation for User Story 2

- [X] T054 [US2] Implementar `src/EvTrainer.Web/Domain/BackupFormat.cs` (`Read` → `union BackupReadResult(BackupDocuments, BackupError)`, `Write(all, indented)`, `Preview` → `BackupMergePreview(New, Replaced)`) — faz T051 e T052 passarem
- [X] T055 [US2] Adicionar ao `TrainerStore` `ExportBackup()` (texto indentado) e `ImportBackup(BackupDocuments)` (merge por id: substitui iguais, adiciona novos, mantém locais ausentes do arquivo; grava) em `src/EvTrainer.Web/State/TrainerStore.cs`
- [X] T056 [US2] Criar `src/EvTrainer.Web/Components/Settings/BackupPanel.razor` (exportar via `evt.downloadText`, importar via `InputFile` com `accept=".json"`, prévia, confirmar, erro) (+ chaves) e incluí-lo na aba Ajustes e no estado vazio (`Menu.ImportBackup`) — faz T053 passar
- [X] T057 [US2] Rodar `dotnet format`/`dotnet test` e validar [quickstart.md §3](./quickstart.md) passos 1–2 e 4

**Checkpoint**: dados atuais migrados; backup de ida e volta.

---

## Phase 5: User Story 3 - Encontrar o adversário certo rapidamente (Priority: P3)

**Goal**: filtro Úteis/Todos, recentes e busca por nome, número, rota, líder ou treinador.

**Independent Test**: com metas definidas, conferir filtro, recentes e as buscas do HANDOFF §6.

### Tests for User Story 3 ⚠️

- [X] T058 [P] [US3] Teste `tests/EvTrainer.Tests/Domain/TextFoldTests.cs`: `Fold("Laboratório")=="laboratorio"`, `Fold("Nidoran♀")=="nidoran f"`, `Fold("Nidoran♂")=="nidoran m"`, ç/ã/õ/ê/ü dobrados, minúsculas
- [X] T059 [P] [US3] Teste `tests/EvTrainer.Tests/Domain/RouteExpanderTests.cs`: `"rota 3, 4, 22"` ⇒ `"rota 3, rota 4, rota 22"`; `"rotas 12–15"` ⇒ `"rota 12, rota 13, rota 14, rota 15"`; hífen comum e travessão; texto sem rota inalterado
- [X] T060 [P] [US3] Teste `tests/EvTrainer.Tests/Domain/SearchTests.cs`: `"rota 4"` inclui Mankey (selvagem "Rota 3, 4, 22") e Paras/Parasect com `TrainerLocations` contendo "Rota 4"; `"rota 1"` retorna exatamente Pidgey e Rattata; `"brock"` retorna Sandshrew, Geodude e Onix; `"nidoran f"` encontra Nidoran♀; busca por número (`"#25"` ou `"25"`) encontra Pikachu; fronteira de dígito (`"rota 1"` não casa "rota 13"); hits por nome/selvagem têm `TrainerLocations` vazio
- [X] T061 [P] [US3] Teste `tests/EvTrainer.Tests/Domain/UsefulFilterTests.cs`: metas `[252,0,2,252,0,4]` com DEF = 2 ⇒ Geodude não útil, Ivysaur não útil (meta SpD 0), Caterpie e Oddish úteis; vitamina só é útil com o stat < 100 e < meta
- [X] T062 [P] [US3] Teste `tests/EvTrainer.Tests/Domain/RecentTests.cs`: `Evs.Recent(events, data, 6)` devolve até 6 nomes canônicos distintos do mais recente para trás (aliases colapsam)
- [X] T063 [P] [US3] Teste `tests/EvTrainer.Tests/Components/Evs/OpponentSearchTests.cs` (bUnit): seletor Úteis (padrão) | Todos persiste em `UiPreferences.Filter`; com texto na busca o filtro é ignorado, título vira `Evs.List.Results` e Recentes some; recentes aparecem sem busca e não se repetem na lista; card de match por treinador mostra `Evs.Card.Trainers` ("Treinadores: Rota 4"); estados vazios `Empty.NoUseful` e `Empty.NoResults` ocupam as 2 colunas; digitar não perde o foco nem o texto ao re-renderizar

### Implementation for User Story 3

- [X] T064 [P] [US3] Implementar `src/EvTrainer.Web/Domain/TextFold.cs` (tabela própria, sem `string.Normalize`, research R7) — faz T058 passar
- [X] T065 [P] [US3] Implementar `src/EvTrainer.Web/Domain/RouteExpander.cs` — faz T059 passar
- [X] T066 [US3] Implementar `src/EvTrainer.Web/Domain/OpponentSearch.cs` (`Search(query, data)` → `SearchHit(Opponent, TrainerLocations)`; índice pré-calculado de textos dobrados e expandidos; `HashSet` com `[with(StringComparer.Ordinal)]` de C# 15) — faz T060 passar (depende de T064, T065)
- [X] T067 [US3] Adicionar `Evs.IsUseful` e `Evs.Recent` (C# 15 collection expression com comparer) em `src/EvTrainer.Web/Domain/Evs.cs` — faz T061 e T062 passarem
- [X] T068 [US3] Criar `src/EvTrainer.Web/Components/Evs/OpponentSearchBar.razor` (input de busca + seletor Úteis | Todos) e evoluir `OpponentGrid.razor` para seções Recentes / lista principal com títulos `Evs.List.Useful`, `Evs.List.All`, `Evs.List.Results`, linha "Treinadores" com reticências e estados vazios (+ chaves) — faz T063 passar
- [X] T069 [US3] Rodar `dotnet format`/`dotnet test` e validar [quickstart.md §2](./quickstart.md) passo 6

**Checkpoint**: lista útil e busca funcionando.

---

## Phase 6: User Story 4 - Estimar IVs a partir dos stats de cada nível (Priority: P4)

**Goal**: anotar stats por nível e ver faixas de IV por linha e a linha Final.

**Independent Test**: anotar os 6 níveis do caso de interseção do HANDOFF §6 e conferir a Final.

### Tests for User Story 4 ⚠️

- [X] T070 [P] [US4] Teste `tests/EvTrainer.Tests/Domain/StatsTests.cs`: `Stats.Value` com aritmética inteira — HP = `floor((2B+IV+floor(EV/4))·L/100) + L + 10`; demais = `floor((inner + 5) · natMul / 10)` com natMul 9/10/11; casos de checagem com Squirtle (base `44 48 65 50 64 43`) nível 5 IV 31 EV 0 Timid
- [X] T071 [P] [US4] Teste `tests/EvTrainer.Tests/Domain/IvTests.cs`: Squirtle Timid nível 5, EVs 0, stats `[20, 9, 12, 11, 12, 11]` ⇒ `IvSpan` HP 12–31, ATK 4–31, DEF 10–29, SPA 20–31, SPD 12–31, SPE 14–31; interseção com as 6 linhas do HANDOFF §6 (lv 5/13/16/18/20/21, Squirtle → Wartortle; Wartortle `59 63 80 65 80 58`) ⇒ Final `FinalSpan(23,25)`, `FinalSpan(20,21)`, `FinalSpan(18,19)`, `FinalSpan(26,28)`, `FinalSingle(29)`, `FinalSingle(30)`; stat impossível ⇒ `IvInconsistent`; espécie da linha ausente da linha evolutiva ⇒ `IvInconsistent` em todos os stats; faixas que não se cruzam ⇒ `FinalConflict`; sem linhas ⇒ `FinalNoData`; nature `""` = neutra. Usar `switch` exaustivo sobre `IvCell`/`FinalCell` (C# 15 `closed`)
- [X] T072 [P] [US4] Teste `tests/EvTrainer.Tests/Components/Ivs/IvTableTests.cs` (bUnit): linha **Final** em destaque + uma linha por nível ("Nv 21", chave `Ivs.Row.Level`); células `min–max`, valor único, "·" sem dado, "?" (chave `Ivs.Cell.Inconsistent`) com classe de erro e nota `Ivs.InconsistentNote` quando houver; "conferir" (`Ivs.Final.Conflict`) e "—" (`Ivs.Final.NoData`); remover linha oferece Desfazer
- [X] T073 [P] [US4] Teste `tests/EvTrainer.Tests/Components/Ivs/LevelFormTests.cs` (bUnit): campos Nível (1–100), Espécie (select da linha evolutiva, padrão = última usada), 6 stats e 6 EVs pré-preenchidos com `Evs.Totals` atuais e editáveis, todos `inputmode="numeric"`; "Salvar nível" desabilitado sem espécie ou com stat vazio; salvar insere e mantém `IvLog` ordenado por nível

### Implementation for User Story 4

- [X] T074 [P] [US4] Implementar `src/EvTrainer.Web/Domain/Stats.cs` — faz T070 passar
- [X] T075 [US4] Implementar `src/EvTrainer.Web/Domain/Ivs.cs` com `closed record IvCell` (`IvSpan`, `IvInconsistent`) e `closed record FinalCell` (`FinalSpan`, `FinalSingle`, `FinalConflict`, `FinalNoData`) — faz T071 passar (depende de T074)
- [X] T076 [US4] Adicionar ao `TrainerStore` `AddLevel(entry)` (insere e reordena por nível) e `RemoveLevel(index)` com Desfazer (reinsere no mesmo índice) em `src/EvTrainer.Web/State/TrainerStore.cs`
- [X] T077 [P] [US4] Criar `src/EvTrainer.Web/Components/Ivs/IvTable.razor` (+ chaves) — faz T072 passar
- [X] T078 [P] [US4] Criar `src/EvTrainer.Web/Components/Ivs/LevelForm.razor` (+ chaves) — faz T073 passar
- [X] T079 [US4] Ligar a aba IVs (card com nome + `Ivs.Title` e a nature à direita, `IvTable`, `LevelForm`) em `src/EvTrainer.Web/Pages/Home.razor`
- [X] T080 [US4] Completar `tests/EvTrainer.Tests/Domain/LegacyExportGoldenTests.cs` (SC-003) com as faixas de IV do `squirtle` importado: Final = HP `FinalSingle(25)`, ATK `FinalSpan(20,21)`, DEF `FinalSingle(18)`, SPA `FinalSingle(27)`, SPD `FinalSingle(29)`, SPE `FinalSingle(30)`, e linha lv 23 (Wartortle) = HP 25–28, ATK 18–21, DEF 14–18, SPA 23–27, SPD 27–31, SPE 27–30
- [X] T081 [US4] Rodar `dotnet format`/`dotnet test` e validar [quickstart.md §2](./quickstart.md) passo 7 e [§3](./quickstart.md) passo 3

**Checkpoint**: calculadora de IV equivalente ao app atual.

---

## Phase 7: User Story 5 - Gerenciar vários Pokémon e o histórico (Priority: P5)

**Goal**: menu com vários Pokémon, troca, adição, exclusão com desfazer, histórico editável e
rótulos de nature.

**Independent Test**: cadastrar dois Pokémon, alternar registrando batalhas, remover um item do
histórico e apagar um Pokémon, desfazendo cada ação.

### Tests for User Story 5 ⚠️

- [X] T082 [P] [US5] Teste `tests/EvTrainer.Tests/Components/Shell/PokemonMenuTests.cs` (bUnit): lista por `Order` com nome e `total/510`, marcador no selecionado; escolher troca o ativo e fecha; fecha com clique fora e com Esc; itens `Menu.AddPokemon`, `Menu.ExportBackup`, `Menu.ImportBackup`, `Menu.Theme` (Sistema/Claro/Escuro → `evt.setTheme` + `UiPreferences.Theme`); **não** há item de tela cheia; item de idioma oculto com só `pt-BR` disponível e visível quando `LanguageState.Available` tem 2+ (troca chama `evt.setLanguage` e re-renderiza sem recarregar)
- [X] T083 [P] [US5] Teste `tests/EvTrainer.Tests/Components/Evs/RecentHistoryTests.cs` (bUnit): seção recolhível com os últimos 12 eventos, mais novo primeiro, com `×n` e multiplicador; remover recalcula totais e oferece Desfazer (reinsere no mesmo índice)
- [X] T084 [P] [US5] Teste `tests/EvTrainer.Tests/Components/Settings/DeletePanelTests.cs` (bUnit): seção recolhível com botão vermelho; apagar remove do menu e toast `Toast.PokemonDeleted` com Desfazer restaura objeto e ordem
- [X] T085 [P] [US5] Teste `tests/EvTrainer.Tests/Components/Settings/NatureOptionsTests.cs` (bUnit): cada opção mostra "(+X −Y)" com rótulos `Stat.Short.*` ou a chave `Settings.Nature.Neutral` ("neutra")

### Implementation for User Story 5

- [X] T086 [US5] Adicionar ao `TrainerStore` `Select(id)`, `RemoveEvent(index)` com Desfazer, `DeletePokemon(id)` com Desfazer (restaura objeto e `Order`) e `SetTheme`/`SetLanguage` gravando `UiPreferences` em `src/EvTrainer.Web/State/TrainerStore.cs`
- [X] T087 [P] [US5] Criar `src/EvTrainer.Web/Components/Shell/PokemonMenu.razor` (dropdown, foco e Esc, clique fora) (+ chaves) e ligar ao botão ☰ do `AppHeader.razor` — faz T082 passar
- [X] T088 [P] [US5] Criar `src/EvTrainer.Web/Components/Evs/RecentHistory.razor` (+ chaves) e incluí-lo na aba EVs — faz T083 passar
- [X] T089 [P] [US5] Criar `src/EvTrainer.Web/Components/Settings/DeletePanel.razor` (+ chaves) e incluí-lo na aba Ajustes — faz T084 passar
- [X] T090 [US5] Exibir rótulos de nature "(+X −Y)"/"neutra" no select de `SettingsForm.razor` — faz T085 passar
- [X] T091 [US5] Rodar `dotnet format`/`dotnet test` e validar manualmente a troca entre dois Pokémon

**Checkpoint**: time inteiro gerenciável, com correções e desfazer.

---

## Phase 8: User Story 6 - Importar registros colados de uma planilha (Priority: P6)

**Goal**: colar linhas (TAB ou `;`) como Batalhas ou Níveis, ver prévia e confirmar.

**Independent Test**: colar linhas de batalhas e níveis e conferir prévia e resultado.

### Tests for User Story 6 ⚠️

- [X] T092 [P] [US6] Teste `tests/EvTrainer.Tests/Domain/SpreadsheetImportTests.cs`: Batalhas — `Caterpie;3;S` ⇒ 1 reconhecida, `{Caterpie, 3, 2}`; quantidade ausente ou não inteira > 0 ⇒ 1; célula +2 diferente de "S" ⇒ `m=1`; primeira célula com nome de adversário ou alias é usada; linha sem nome ⇒ ignorada; separadores TAB e `;`; linhas vazias não contam. Níveis — `21\tWartortle\t62\t31\t42\t39\t44\t38\t38\t0\t2\t27\t0\t4` ⇒ `LevelEntry(21, "Wartortle", [62,31,42,39,44,38], [38,0,2,27,0,4])`; EVs faltando ⇒ 0; menos de 6 stats ⇒ ignorada; espécie fora da linha evolutiva ⇒ ignorada. `ImportPreview.Recognized`/`Ignored` corretos
- [X] T093 [P] [US6] Teste `tests/EvTrainer.Tests/Components/Settings/SpreadsheetImportPanelTests.cs` (bUnit): seção recolhível "Importar do Excel" com seletor Batalhas | Níveis (IVs), textarea, prévia `Import.Preview` ("N linha(s) reconhecida(s), M ignorada(s)") e botão `Import.Confirm`; confirmar anexa eventos ao fim ou insere níveis ordenados; toast de sucesso

### Implementation for User Story 6

- [X] T094 [US6] Implementar `src/EvTrainer.Web/Domain/SpreadsheetImport.cs` (`Battles`, `Levels` → `ImportPreview<T>`; laços linha × célula com `break`/`continue` rotulados do C# 15) — faz T092 passar
- [X] T095 [US6] Adicionar ao `TrainerStore` `ImportBattles(items)` e `ImportLevels(items)` em `src/EvTrainer.Web/State/TrainerStore.cs`
- [X] T096 [US6] Criar `src/EvTrainer.Web/Components/Settings/SpreadsheetImportPanel.razor` (+ chaves) e incluí-lo na aba Ajustes — faz T093 passar
- [X] T097 [US6] Rodar `dotnet format`/`dotnet test`

**Checkpoint**: todas as histórias funcionando de forma independente.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: PWA, fontes, idiomas, acessibilidade, deploy e validação final.

- [X] T098 [P] Teste `tests/EvTrainer.Tests/Localization/NoHardcodedTextTests.cs` (SC-010): gerar em memória um idioma sintético `xx` com todas as chaves do pt-BR mapeadas para `«chave»`, ativá-lo no `LanguageState` e renderizar com bUnit `Home` (estado vazio e com um Pokémon nas 3 abas), `PokemonMenu` aberto e `Toast`; todo nó de texto visível e todo `aria-label`/`title`/`placeholder` deve estar no formato `«...»` ou ser dado (nome de Pokémon/espécie/nature/local, números, emojis de status, símbolos `×`, `/`, `–`, `·`)
- [X] T099 Corrigir qualquer texto fixo apontado por T098 movendo-o para `src/EvTrainer.Web/Localization/strings.pt-BR.json`
- [X] T100 [P] Baixar Chakra Petch, IBM Plex Sans e IBM Plex Mono (OFL, subset latin, `.woff2`) para `src/EvTrainer.Web/wwwroot/fonts/` com os arquivos de licença, declarar `@font-face` em `wwwroot/css/app.css` (`font-display: swap`, números em `font-variant-numeric: tabular-nums`)
- [X] T101 [P] Ajustar `src/EvTrainer.Web/wwwroot/manifest.webmanifest`: `name` "EVs Trainer", `short_name` "EVs", `display` "standalone", `start_url` "./", `scope` "./", `lang` "pt-BR", `background_color`/`theme_color` de `--bg`/`--accent`; substituir `icon-192.png`/`icon-512.png` e `favicon.png`
- [X] T102 Revisar acessibilidade em todos os componentes: `aria-label` em controles só com ícone, `aria-pressed` nos toggles, foco visível, alvos de toque ≥ 44 px; e layout sem rolagem horizontal em 360 px nas 3 abas, temas claro e escuro (quickstart §2 passo 8)
- [X] T103 Validar publish e offline localmente: `dotnet publish src/EvTrainer.Web -c Release -o artifacts/publish` + `dotnet serve -d artifacts/publish/wwwroot -p 5080` ([quickstart.md §4](./quickstart.md)): service worker ativo, modo offline funcional, manifest instalável, nenhuma requisição externa
- [X] T104 Rodar `dotnet format PokemonEvTrainer.slnx --verify-no-changes` e `dotnet test --solution PokemonEvTrainer.slnx` (tudo verde), fazer push na `main` e validar o deploy em `https://albertomonteiro.github.io/ev-trainer/` ([quickstart.md §5](./quickstart.md)); habilitar antes Settings → Pages → Source: GitHub Actions
- [X] T105 Remover o comentário "Sync Impact Report" do topo de `.specify/memory/constitution.md` antes do commit final (é material temporário de revisão, conforme o comando de constituição)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências.
- **Foundational (Phase 2)**: depende do Setup; **bloqueia todas as histórias**.
- **US1 (Phase 3)**: depende da Fundação. É o MVP.
- **US2 (Phase 4)**: depende da Fundação; usa `Evs.Totals`/`CountFor` de US1 no teste golden
  (T052 → T042).
- **US3 (Phase 5)**: depende da Fundação e do `OpponentGrid` de US1 (T046).
- **US4 (Phase 6)**: depende da Fundação; T080 depende de T054 (US2, leitura do export).
- **US5 (Phase 7)**: depende da Fundação e da casca de US1 (aba EVs/Ajustes).
- **US6 (Phase 8)**: depende da Fundação; usa `ReferenceData.Canon` (T024) e o store.
- **Polish (Phase 9)**: depois das histórias desejadas.

### User Story Dependencies

- US1 → independente (só Fundação).
- US2 → independente para backup; o teste golden reaproveita `Evs` de US1.
- US3 → estende a grade de US1.
- US4 → independente no domínio; golden de IV usa o leitor de backup de US2.
- US5, US6 → independentes entre si; ambas se apoiam nas abas criadas em US1.

### Within Each User Story

- Testes primeiro e **falhando** → domínio → store → componentes → ligação na página → format/test.
- Tarefas que editam `strings.pt-BR.json`, `TrainerStore.cs`, `Home.razor` ou `app.css` são
  sequenciais entre si.

### Parallel Opportunities

- Setup: T002 e T003.
- Fundação: testes T014–T022 todos em paralelo; implementações T023, T024, T026, T027, T028, T033
  em paralelo.
- Cada história: todas as tarefas de teste [P] em paralelo; componentes [P] em paralelo depois do
  domínio.
- US5 e US6 podem andar em paralelo após US1.

---

## Parallel Example: User Story 1

```bash
# Testes de US1 (escrever e ver falhar juntos):
Task: "Teste EvTotalsTests em tests/EvTrainer.Tests/Domain/EvTotalsTests.cs"
Task: "Teste BattleLogTests em tests/EvTrainer.Tests/Domain/BattleLogTests.cs"
Task: "Teste EvSummaryTests em tests/EvTrainer.Tests/Components/Evs/EvSummaryTests.cs"
Task: "Teste OpponentCardTests em tests/EvTrainer.Tests/Components/Evs/OpponentCardTests.cs"
Task: "Teste SettingsFormTests em tests/EvTrainer.Tests/Components/Settings/SettingsFormTests.cs"

# Componentes de US1 depois do domínio (T042–T044):
Task: "EvSummary.razor em src/EvTrainer.Web/Components/Evs/"
Task: "OpponentCard.razor + OpponentGrid.razor em src/EvTrainer.Web/Components/Evs/"
Task: "SettingsForm.razor + SpeciesEditor.razor em src/EvTrainer.Web/Components/Settings/"
```

## Parallel Example: User Story 3

```bash
Task: "Teste TextFoldTests em tests/EvTrainer.Tests/Domain/TextFoldTests.cs"
Task: "Teste RouteExpanderTests em tests/EvTrainer.Tests/Domain/RouteExpanderTests.cs"
Task: "Teste SearchTests em tests/EvTrainer.Tests/Domain/SearchTests.cs"
Task: "Teste UsefulFilterTests em tests/EvTrainer.Tests/Domain/UsefulFilterTests.cs"
Task: "Teste RecentTests em tests/EvTrainer.Tests/Domain/RecentTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1 (Setup) → Phase 2 (Fundação).
2. Phase 3 (US1).
3. **PARAR E VALIDAR**: quickstart §2 passos 1–5; push na `main` publica o MVP no Pages.

### Incremental Delivery

1. Setup + Fundação → base pronta (CI já publica).
2. US1 → MVP publicado.
3. US2 → migração do Wartortle e backup (o usuário já pode largar o app antigo).
4. US3 → busca e filtro.
5. US4 → IVs.
6. US5 → vários Pokémon e histórico.
7. US6 → importação de planilha.
8. Polish → fontes, PWA final, verificação de texto fixo, validação de deploy.

---

## Notes

- [P] = arquivos diferentes, sem dependência de tarefa incompleta.
- Rótulos [USn] ligam cada tarefa à história da spec.
- Confirmar que cada teste falha antes de implementar (Princípio III).
- Commitar ao fim de cada tarefa ou grupo lógico (mensagens sem menção a ferramentas de IA).
- Quando o .NET 11 GA sair, seguir [quickstart.md §6](./quickstart.md) (pode virar uma tarefa
  avulsa fora desta feature).
