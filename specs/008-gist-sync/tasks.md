---

description: "Task list for Sincronização com Gist privado"
---

# Tasks: Sincronização com Gist privado

**Input**: Design documents from `/specs/008-gist-sync/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/sync.md](./contracts/sync.md),
[quickstart.md](./quickstart.md)

**Tests**: OBRIGATÓRIOS (constituição, Princípio III). Escrever, rodar com
`dotnet test --solution PokemonEvTrainer.slnx`, **ver falhar**, então implementar. **Nenhum teste usa
a rede**: o GitHub é simulado por `FakeHttpHandler` e o armazenamento remoto por `FakeSyncProvider`.

**Organization**: US1 conectar ao Gist (P1), US2 mesmos dados no celular e no PC (P1), US3 estado da
sincronização (P2).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)
- **[Story]**: US1, US2, US3

## Convenções

- dotnet CLI primeiro; `dotnet format PokemonEvTrainer.slnx` após cada tarefa com código; nenhum
  pacote NuGet novo (`HttpClient` e `System.Net.Http.Json` do runtime).
- Domínio sem I/O: `SyncMerge` e o carimbo de data não conhecem HTTP nem armazenamento.
- **Token**: nunca em tela após salvo, backup, conteúdo do Gist, URL, mensagem ou exceção
  (SC-005); mensagens de erro saem de `SyncErrorKind` + chaves `Sync.Error.*`.
- Textos novos só em `src/EvTrainer.Web/Localization/strings.pt-BR.json` (Princípio VII).
- Edições em `TrainerStore.cs`, `Home.razor`, `strings.pt-BR.json`, `app.css` e `Program.cs` são
  sequenciais entre si.
- Ao mexer em `.razor` com `@for`, validar com `dotnet build PokemonEvTrainer.slnx --no-incremental`
  (AD0001 no RC1).

---

## Phase 1: Setup

- [X] T001 Confirmar base verde: `dotnet format PokemonEvTrainer.slnx --verify-no-changes` e `dotnet test --solution PokemonEvTrainer.slnx` (447 testes)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: "alterado em" por Pokémon, registro de exclusões, regra de combinação, guarda da conexão e a interface de provedor — usados por todas as histórias.

**⚠️ CRITICAL**: nenhuma história começa antes desta fase.

### Tests ⚠️

- [X] T002 [P] Ampliar `tests/EvTrainer.Tests/Domain/ModelJsonTests.cs` e `tests/EvTrainer.Tests/Domain/BackupFormatTests.cs`: `updatedAt` lido/gravado em ISO 8601 UTC ("data ISO 8601 válida, senão `null`"); documento com `deleted: { id: data }` lido e gravado (valor inválido ⇒ entrada ignorada); arquivo antigo sem `updatedAt`/`deleted` continua válido; ida e volta idêntica; `LegacyExportGoldenTests` verdes
- [X] T003 [P] Teste `tests/EvTrainer.Tests/State/TrainerStoreStampTests.cs` (com `ManualTimeProvider`): cada comando que muda um Pokémon carimba `UpdatedAt` com o horário atual — `RecordBattle`, `UndoLastBattle`, `TogglePokerus`, `ToggleMacho`, `SaveSettings`, `RemoveEvent` (e seu desfazer), `AdjustEvs`, `ResetHistory`, `SetKnownIvs`, `AddLevel`, `RemoveLevel`, `ReplaceLevel`, `ImportBattles`, `ImportLevels`, `ImportBackup`, `AddPokemon`, `AddFromCapture`; `Select`, `SetFilter`, `SetTheme`, `SetLanguage` não carimbam; `DeletePokemon` registra `deleted[id] = agora` e o desfazer remove o registro e carimba o Pokémon; `Snapshot()` devolve documentos e exclusões; `ApplySync(result)` substitui os dados sem carimbar, mantém a seleção se o Pokémon ainda existe (senão o primeiro) e grava
- [X] T004 [P] Teste `tests/EvTrainer.Tests/Domain/SyncMergeTests.cs` com a tabela de [contracts/sync.md](./contracts/sync.md) (11 casos: só local, só remoto, remoto mais novo, local mais novo, exclusão remota mais nova, exclusão local mais nova, edição depois da exclusão, sem data × datado, empate determinístico nos dois sentidos, exclusão de 91 dias descartada, idempotência) e as contagens `Updated`/`Added`/`Removed`, `LocalChanged`, `RemoteChanged`
- [X] T005 [P] Teste `tests/EvTrainer.Tests/State/SyncConnectionTests.cs`: `SyncConnection` (`Provider "gist"`, `Token`, `Login`, `GistId`, `LastSyncedAt`, `ETag`) ida e volta pelo repositório em memória; documento ausente ou inválido ⇒ `null`; `ClearSyncConnection` apaga

### Implementation

- [X] T006 Em `src/EvTrainer.Web/Domain/Model/TrainingPokemon.cs` adicionar `DateTimeOffset? UpdatedAt` (e no `Clone`); em `src/EvTrainer.Web/Domain/Json/PokemonJson.cs` ler/gravar `updatedAt` e o mapa `deleted` do documento (`Serialize(all, deleted, indented)` e leitura que devolve documentos + exclusões), mantendo `BackupFormat` compatível — faz T002 passar
- [X] T007 Em `src/EvTrainer.Web/State/IPokemonRepository.cs`, `LocalStorageRepository.cs` e `tests/EvTrainer.Tests/Support/InMemoryRepository.cs`: gravar/ler as exclusões junto do documento de dados (`SaveAll(all, deleted)`, `LoadDeleted()`) e `LoadSyncConnection` / `SaveSyncConnection` / `ClearSyncConnection` (chave `evtrainer:v1:sync`); criar `src/EvTrainer.Web/State/SyncConnection.cs` com o JSON tolerante — faz T005 passar
- [X] T008 Em `src/EvTrainer.Web/State/TrainerStore.cs`: receber `TimeProvider` (padrão `TimeProvider.System`), carimbar no ponto único `Commit(changedId)` usado por todos os comandos que mudam um Pokémon, registrar exclusões (e removê-las no desfazer), `Snapshot()` e `ApplySync(MergeResult)`; registrar o `TimeProvider` em `tests/EvTrainer.Tests/Support/AppTestContext.cs` — faz T003 passar
- [X] T009 Criar `src/EvTrainer.Web/Domain/SyncMerge.cs` (`SyncSnapshot`, `MergeResult`, `SyncMerge.Merge`, retenção de 90 dias) conforme research R4 — faz T004 passar
- [X] T010 [P] Criar `src/EvTrainer.Web/State/ISyncProvider.cs` (`ISyncProvider`, `SyncAccount`, `RemoteRead`, `SyncErrorKind`, `SyncException` sem texto do servidor) e `tests/EvTrainer.Tests/Support/FakeSyncProvider.cs` (remoto em memória com ETag por versão, falhas programáveis por tipo, e um gancho para simular outro aparelho gravando entre a leitura e a escrita)
- [X] T011 Rodar `dotnet format` e `dotnet test` (tudo verde)

**Checkpoint**: base de sincronização pronta, ainda sem provedor real nem tela.

---

## Phase 3: User Story 1 - Conectar o app ao meu Gist (Priority: P1) 🎯 MVP

**Goal**: conectar com um token só de gists, criar/achar o Gist secreto, enviar os dados, desconectar.

**Independent Test**: com 2 Pokémon, conectar com token válido (simulado) ⇒ "Conectado como @usuario" e o remoto com os 2 Pokémon no formato do backup; token inválido ⇒ mensagem e nada salvo; desconectar ⇒ token apagado, dados locais intactos.

### Tests for User Story 1 ⚠️

- [X] T012 [P] [US1] Teste `tests/EvTrainer.Tests/State/GistSyncProviderTests.cs` com `tests/EvTrainer.Tests/Support/FakeHttpHandler.cs`: cabeçalhos `Authorization: Bearer`, `Accept: application/vnd.github+json`, `X-GitHub-Api-Version: 2022-11-28`; conectar = `GET /user` + `GET /gists?per_page=100` com paginação por `Link`, escolhendo o gist mais antigo com `evs-trainer.json`; sem gist ⇒ `POST /gists` com `public: false`, descrição `EVs Trainer` e o arquivo; 401 ⇒ `InvalidToken`; 403/404 nas chamadas de gists ⇒ `MissingGistPermission`; `x-ratelimit-remaining: 0` ⇒ `RateLimited`; `HttpRequestException` ⇒ `Offline`; ler com `If-None-Match` (304 ⇒ `NotModified`; arquivo ausente ou `truncated` ⇒ `InvalidRemote`); gravar com `PATCH` só do arquivo; nenhuma `SyncException` contém o token
- [X] T013 [P] [US1] Teste `tests/EvTrainer.Tests/State/SyncServiceTests.cs` (parte conexão, com `FakeSyncProvider`): `ConnectAsync` válido salva a conexão (token, login, gist), faz a primeira sincronização enviando os dados locais (com `updatedAt` e `deleted`) e fica `Synced` com horário; falha devolve o `SyncErrorKind` e não salva nada; `Disconnect` apaga a conexão, fica `Off` e mantém os dados locais; `ExportBackup()` e o conteúdo enviado nunca contêm o token
- [X] T014 [P] [US1] Teste `tests/EvTrainer.Tests/Components/Sync/SyncViewTests.cs` (bUnit, `Home`): item `Menu.Sync` abre a tela; desligada mostra explicação (`Sync.Intro`), aviso `Sync.SecretWarning`, passo a passo (`Sync.Steps.*`) com link para criar o token fine-grained e a alternativa clássica com escopo `gist`, campo `input[type=password]` e **Conectar**; conexão ok ⇒ `Sync.ConnectedAs` com o login e o token não aparece em lugar nenhum do DOM; erro ⇒ `Sync.Error.{Kind}`; **Desconectar** pede confirmação e volta ao estado desligado; **Fechar** volta às abas

### Implementation for User Story 1

- [X] T015 [US1] Criar `src/EvTrainer.Web/State/GistSyncProvider.cs` (research R1–R3; base `https://api.github.com`; mapeamento de erros por status) — faz T012 passar
- [X] T016 [US1] Criar `src/EvTrainer.Web/State/SyncService.cs` com `State`, `Login`, `LastSyncedAt`, `Changed`, `ConnectAsync`, `Disconnect` e um ciclo simples de primeira sincronização (ler → `SyncMerge` → `ApplySync` → gravar se `RemoteChanged`); registrar em `src/EvTrainer.Web/Program.cs` (`HttpClient` com a base do GitHub, `ISyncProvider` → `GistSyncProvider`, `SyncService`) e em `tests/EvTrainer.Tests/Support/AppTestContext.cs` (com `FakeSyncProvider`) — faz T013 passar
- [X] T017 [US1] Criar `src/EvTrainer.Web/Components/Sync/SyncView.razor` e ligá-la em `src/EvTrainer.Web/Pages/Home.razor` (mesmo padrão de Avaliar captura) e no menu `src/EvTrainer.Web/Components/Shell/PokemonMenu.razor` (`Menu.Sync`); chaves `Sync.*` em `strings.pt-BR.json`; estilos em `src/EvTrainer.Web/wwwroot/css/app.css` (360 px e desktop) — faz T014 passar
- [X] T018 [US1] Rodar `dotnet format`, `dotnet build --no-incremental` e `dotnet test`; conferir a tela em 360 px e 1366 px sem token real

**Checkpoint**: MVP — conectar e ter os dados num Gist secreto do próprio jogador.

---

## Phase 4: User Story 2 - Usar os mesmos dados no celular e no PC (Priority: P1)

**Goal**: sincronização automática e segura entre aparelhos (combinação por Pokémon, exclusões, offline, concorrência).

**Independent Test**: dois aparelhos simulados (dois `TrainerStore` + dois `SyncService` sobre o mesmo `FakeSyncProvider`): mudança em A chega a B; conflito ⇒ vence o mais recente; exclusão propaga; desfazer traz de volta; nada mais novo é perdido.

### Tests for User Story 2 ⚠️

- [X] T019 [US2] Ampliar `tests/EvTrainer.Tests/State/SyncServiceTests.cs`: dois aparelhos — alteração em A chega a B; Pokémon novo em B chega a A; conflito ⇒ vence o mais recente; exclusão em B some em A e o desfazer em B traz de volta nos dois; sem internet (`Offline`) ⇒ `Pending`, dados locais intactos e sincroniza no próximo gatilho; outro aparelho grava entre a leitura e a escrita ⇒ o ciclo recombina antes de gravar e nada se perde; aviso `Sync.Merged` com atualizados/adicionados/removidos só quando algo veio do remoto
- [X] T020 [US2] Ampliar `tests/EvTrainer.Tests/State/SyncServiceTests.cs` (gatilhos, `ManualTimeProvider`): `Start()` conectado sincroniza; várias mudanças seguidas no `TrainerStore` geram **um** ciclo 5 s depois da última; `OnVisible()` sincroniza; `SyncNowAsync()` sincroniza; mudanças aplicadas pela própria sincronização não disparam novo ciclo; ciclos nunca rodam em paralelo; desconectado nada dispara

### Implementation for User Story 2

- [X] T021 [US2] Em `src/EvTrainer.Web/State/SyncService.cs`: ciclo completo do research R6 (leitura condicional, merge síncrono com o estado atual, releitura antes de gravar, fila serial), estados `Pending`/`Error`, aviso via `ToastService`, e gatilhos (`Start`, `OnVisible`, debounce de 5 s sobre `TrainerStore.Changed` com `TimeProvider`, `SyncNowAsync`) — faz T019 e T020 passarem
- [X] T022 [US2] Em `src/EvTrainer.Web/wwwroot/js/interop.js` adicionar `evt.onVisible(ref)` (chama `OnVisible` via `DotNetObjectReference` quando `document.visibilityState === 'visible'`) e registrá-lo uma vez (ex.: no `App.razor` ou num componente de shell, com try/catch do BL0016); chamar `SyncService.Start()` em `src/EvTrainer.Web/Program.cs` após `store.Load()`; botão **Sincronizar agora** na `SyncView`
- [X] T023 [US2] Rodar `dotnet format` e `dotnet test`

**Checkpoint**: celular e PC com os mesmos dados sem toques.

---

## Phase 5: User Story 3 - Saber o estado da sincronização (Priority: P2)

**Goal**: o indicador de status mostra sincronizado (horário), sincronizando, pendente ou erro com ação.

**Independent Test**: estados simulados ⇒ ícone e texto certos; desligado ⇒ igual a hoje; tocar abre a tela de sincronização.

### Tests for User Story 3 ⚠️

- [X] T024 [P] [US3] Ampliar `tests/EvTrainer.Tests/Components/Shell/StatusIndicatorTests.cs`: sincronização desligada ⇒ comportamento atual (`Status.Saved`/`Saving`/`Failed`); ligada ⇒ `Sync.Status.Synced` com horário, `Sync.Status.Syncing`, `Sync.Status.Pending`, `Sync.Status.Error.{Kind}`; com erro de token, a tela mostra **Reconectar**; tocar no indicador abre a `SyncView`

### Implementation for User Story 3

- [X] T025 [US3] Em `src/EvTrainer.Web/Components/Shell/StatusIndicator.razor` (e `AppHeader.razor` para o clique) refletir `SyncService.State`; em `SyncView.razor` mostrar o erro atual com a ação (**Reconectar** para token inválido/sem permissão); chaves `Sync.Status.*` — faz T024 passar
- [X] T026 [US3] Rodar `dotnet format` e `dotnet test`

**Checkpoint**: todas as histórias funcionando.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T027 Ampliar `tests/EvTrainer.Tests/Localization/NoHardcodedTextTests.cs`: `SyncView` desligada, conectada e com erro, e o indicador com sincronização ativa, sem texto fixo
- [X] T028 [P] Teste de segurança `tests/EvTrainer.Tests/State/SyncTokenLeakTests.cs` (SC-005): com um token sentinela conectado, ele não aparece no DOM renderizado de nenhuma tela, no `ExportBackup()`, no conteúdo enviado ao remoto, no documento de dados nem nas mensagens de nenhum `SyncErrorKind`
- [X] T029 [P] Atualizar `README.md`: sincronização opcional com Gist secreto (como criar o token só com "Gists", aviso de que Gist secreto é legível por quem tiver o link, como desconectar)
- [ ] T030 Validar [quickstart.md](./quickstart.md) passos 1–10 com uma conta GitHub de teste e um token descartável **criados e colados pelo próprio usuário** (o agente não cria nem digita tokens); registrar o resultado aqui
  - **Pendente do usuário** (exige conta GitHub e token reais, que o agente não cria nem digita). Já verificado sem token, no app: tela em 360 px e 1366 px (claro/escuro) sem rolagem horizontal, campo do token como senha, nenhuma requisição ao GitHub com a sincronização desligada, boot sem erros no console. Toda a lógica (conectar, combinar, exclusões, offline, concorrência, gatilhos, erros, vazamento do token) está coberta por testes com HTTP e provedor simulados.
- [X] T031 Portão final: `dotnet format PokemonEvTrainer.slnx --verify-no-changes`, `dotnet build PokemonEvTrainer.slnx --no-incremental` (sem AD0001) e `dotnet test --solution PokemonEvTrainer.slnx`

---

## Dependencies & Execution Order

- **Setup (T001)** → **Fundação (T002–T011)** → US1 → US2 → US3 → Polish.
- Fundação: T002 → T006; T005 → T007; T003 → T008 (depende de T006/T007); T004 → T009 (depende de
  T006); T010 independente.
- US2 depende da US1 (`SyncService` e provedor); US3 depende de US1 (estados) e se beneficia da US2.
- Dentro de cada história: testes falhando → provedor/serviço → tela → format/test.

### Parallel Opportunities

- Fundação: T002, T003, T004 e T005 (arquivos de teste diferentes); T010 em paralelo com T006–T009.
- US1: T012, T013 e T014.
- Polish: T028 e T029.

## Parallel Example: Foundational tests

```text
Task: "T002 Ampliar ModelJsonTests.cs / BackupFormatTests.cs (updatedAt, deleted)"
Task: "T003 TrainerStoreStampTests.cs (carimbo por comando, exclusões)"
Task: "T004 SyncMergeTests.cs (tabela do contrato)"
Task: "T005 SyncConnectionTests.cs (conexão no aparelho)"
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Fase 1 e Fase 2.
2. US1 (T012–T018) → conectar e enviar os dados para o Gist secreto do jogador (backup fora do
   aparelho).

### Incremental Delivery

1. + US2 → sincronização automática entre aparelhos.
2. + US3 → status visível.
3. Polish → texto fixo, teste de vazamento do token, README e validação com GitHub real pelo usuário.
