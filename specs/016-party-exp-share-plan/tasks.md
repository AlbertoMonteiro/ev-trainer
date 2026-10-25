# Tasks: Equipe atual e plano de batalhas com Exp. Share

**Input**: Design documents from `/specs/016-party-exp-share-plan/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: obrigatórios (Princípio III, TDD). Cada teste é escrito e visto falhar antes do código.

**Convenções**:

- Os caminhos são relativos à raiz do repositório.
- Domínio em `src/EvTrainer.Web/Domain/`, testes em `tests/EvTrainer.Tests/`.
- Textos novos sempre em `src/EvTrainer.Web/Localization/strings.pt-BR.json` e `strings.en.json`.
- CSS em `src/EvTrainer.Web/wwwroot/css/app.css`.
- Os testes usam `AppTestContext`, `InMemoryRepository` e `Pokemons` de `tests/EvTrainer.Tests/Support/`.

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Setup

- [X] T001 Conferir a linha de base: `dotnet test --solution PokemonEvTrainer.slnx` verde e
  `dotnet format PokemonEvTrainer.slnx --verify-no-changes` limpo, antes de qualquer mudança.

## Phase 2: Foundational (bloqueia as histórias)

**Campo `party`**

- [X] T002 [P] Testes em `tests/EvTrainer.Tests/Domain/ModelJsonTests.cs` (contracts/party.md):
  - `"party": true` e `"party": false` fazem ida e volta;
  - `party` ausente ou inválido (ex.: `"party": "x"`) é lido como `null`;
  - `null` não é escrito (o campo some do JSON);
  - `Clone()` copia `Party`.
- [X] T003 Adicionar `public bool? Party { get; set; }` em `src/EvTrainer.Web/Domain/Model/TrainingPokemon.cs`
  (doc: "true na equipe, false guardado, null dado antigo"), copiar no `Clone()`.
  - Mapear `[JsonPropertyName("party")] bool? Party` no DTO de `src/EvTrainer.Web/Domain/Json/PokemonJson.cs`, como
    `expShare`. Na leitura, aceitar só booleano.
  - T002 verde.

**Normalização da equipe**

- [X] T004 [P] Testes em `tests/EvTrainer.Tests/Domain/PartyTests.cs` (research R1, contracts/party.md, invariantes 1–3):
  - 3 Pokémon `null`: todos ficam `true`;
  - 8 `null` com `UpdatedAt` diferentes: os 6 mais recentes ficam `true` e os outros `false`;
  - 8 `null` sem `UpdatedAt`: os 6 de menor `Order` ficam `true`;
  - 7 `true`: o de `UpdatedAt` mais antigo vira `false`;
  - 4 `true` e 3 `null`: os 2 `null` mais recentes entram e o terceiro vira `false`;
  - o portador do Exp. Share que ficou `false` perde o `ExpShare`;
  - `Normalize` devolve só os ids alterados e não mexe em `UpdatedAt`;
  - `Members` respeita `Order` e depois o id.
- [X] T005 Criar `src/EvTrainer.Web/Domain/Party.cs` (static): `MaxSize = 6`,
  `Normalize(IDictionary<string, TrainingPokemon>) → string[]`, `Members(...)`. A recência segue `UpdatedAt` desc (null
  por último), `Order` asc e id ordinal. T004 verde.
- [X] T006 [P] Testes em `tests/EvTrainer.Tests/State/TrainerStorePartyTests.cs`:
  - `Load`, `ImportBackup` e `ApplySync` chamam a normalização: depois deles não há `null`, há no máximo 6 `true` e
    nenhum guardado com Exp. Share;
  - `Load` com dados antigos salva o resultado sem carimbar.
- [X] T007 Em `src/EvTrainer.Web/State/TrainerStore.cs`:
  - chamar `Party.Normalize(_all)` em `Load`, `ImportBackup` (incluir os alterados no `Commit`) e `ApplySync`, depois de
    `KeepOneExpShareHolder`;
  - adicionar `PartyOrdered`, `Stored` e `IsInParty(id)`.
  - T006 verde.

**Busca de treinadores e batalha inteira** (usadas por US3, US5 e US6)

- [X] T008 [P] Testes em `tests/EvTrainer.Tests/Domain/SearchTests.cs` (contracts/trainer-search.md, tabela de
  `FindTrainers`):
  - `bernie` e `Engineer Bernie` → só `ENGINEER_BERNIE`;
  - `engineer` → `ENGINEER_BRAXTON` e `ENGINEER_BERNIE`, na ordem do guia;
  - `rota 11`, `brock` e vazio → nenhum;
  - acentos e maiúsculas são ignorados.
- [X] T009 `src/EvTrainer.Web/Domain/OpponentSearch.cs`:
  - o construtor recebe `VsSeekerGuide` e pré-normaliza `"{classe} {nome}"`, `nome` e `classe` com `TextFold`, sem
    `RouteExpander`;
  - `FindTrainers(query)` usa o mesmo `Matches`;
  - atualizar `src/EvTrainer.Web/Program.cs` (DI) e `tests/EvTrainer.Tests/Support/AppTestContext.cs`, além dos testes
    que constroem `OpponentSearch` à mão.
  - T008 verde.
- [X] T010 [P] Testes em `tests/EvTrainer.Tests/State/TrainerStoreTeamTests.cs` (research R4):
  - `RecordTeam(["Magneton","Magneton","Magneton"])` sem modificadores dá +6 Sp. Atk e gera um evento `Count = 3`;
  - com o portador do Exp. Share, ele também ganha +6, num evento `Shared`;
  - com Macho Brace no principal, ele ganha +12 e o portador +6;
  - `UndoTeam` volta os dois exatamente ao estado anterior;
  - sem Pokémon selecionado, devolve uma lista vazia.
- [X] T011 `TrainerStore.RecordTeam(IReadOnlyList<string>) → IReadOnlyList<SharedRecord>` (`RecordBattle` por espécie)
  e `UndoTeam(IReadOnlyList<SharedRecord>)` (`UndoBattle` em ordem inversa). T010 verde.

**Checkpoint**: o domínio e o store estão prontos; as histórias podem começar.

## Phase 3: User Story 1 — Lista só com a equipe (P1) 🎯 MVP

**Goal**: o menu ☰ e o painel lateral mostram só os até 6 Pokémon da equipe.

**Independent Test**: com 8 Pokémon (6 na equipe), o menu e o painel listam 6. Um guardado aberto aparece em "Aberto
(guardado)".

- [X] T012 [P] [US1] Testes bUnit em `tests/EvTrainer.Tests/Components/Shell/PokemonMenuTests.cs` e
  `PokemonSidebarTests.cs`:
  - só os da equipe aparecem, na ordem;
  - o selecionado guardado aparece em `.pokemon-list.opened`, com o rótulo;
  - existem o item `.party` no menu e o botão `.manage-party` no painel.
- [X] T013 [US1] `src/EvTrainer.Web/Components/Shell/PokemonList.razor`:
  - iterar `Store.PartyOrdered`;
  - se `Store.Selected` estiver guardado, renderizar depois uma `ul.pokemon-list.opened` com o rótulo `Menu.Opened`.
- [X] T014 [US1] Item `.party` (`Menu.Party`) em `src/EvTrainer.Web/Components/Shell/PokemonMenu.razor`, com
  `OnParty`, ligado via `AppHeader.razor` até `Pages/Home.razor`.
  - Botão `.manage-party` em `PokemonSidebar.razor`.
  - `Home` ganha `_party` (mesmo padrão de `_planning`/`_guiding`, fechando as outras páginas). Por enquanto ele
    renderiza um `PartyView` mínimo (título e Fechar).
  - Textos pt/en.
  - T012 verde.

## Phase 4: User Story 2 — Editar a equipe (P1)

**Goal**: uma página para pôr, tirar, trocar, reordenar e abrir Pokémon.

**Independent Test**: guardar um, pôr outro e trocar com a equipe cheia; o menu reflete tudo e os dados ficam
intactos.

- [X] T015 [P] [US2] Testes em `tests/EvTrainer.Tests/State/TrainerStorePartyTests.cs` (data-model, contracts/party.md
  invariantes 4–6):
  - `JoinParty` com vaga põe o Pokémon na equipe;
  - com a equipe cheia e sem `replacing`, devolve `false` e nada muda;
  - com `replacing`, troca;
  - `LeaveParty` mantém `Events`, `IvLog`, `Targets`, `KnownIvs`, `Pokerus` e `MachoBrace`, e tira o `ExpShare`;
  - `MoveInParty` troca a `Order` com o vizinho da equipe e carimba os dois;
  - `AddPokemon` e `AddFromCapture` com vaga põem na equipe; com 6, deixam guardado e selecionado;
  - o Desfazer de `DeletePokemon` devolve à equipe só se houver vaga;
  - `SetExpShare(id)` de um guardado não faz nada.
- [X] T016 [US2] Implementar `JoinParty(id, replacing?)`, `LeaveParty(id)` e `MoveInParty(id, delta)` em
  `src/EvTrainer.Web/State/TrainerStore.cs` (carimbam os alterados via `Commit`).
  - Ajustar `AddPokemon`, `AddFromCapture`, o Desfazer de `DeletePokemon` e `SetExpShare`.
  - T015 verde.
- [X] T017 [P] [US2] Testes bUnit em `tests/EvTrainer.Tests/Components/Party/PartyViewTests.cs`:
  - seções "Equipe (n/6)" e "Guardados (n)";
  - ↑/↓ com `aria-label`;
  - Guardar e Pôr na equipe;
  - com a equipe cheia, "Trocar…" lista os 6 e troca;
  - "Abrir" seleciona e fecha;
  - equipe vazia mostra o texto;
  - o menu reflete as mudanças.
- [X] T018 [US2] `src/EvTrainer.Web/Components/Party/PartyView.razor` (contracts/party.md, UI):
  - cada linha mostra o sprite (`PokemonSprite`), o nome (`PokemonLabel`), a nature e o total de EV;
  - adicionar o namespace em `_Imports.razor`, os textos pt/en e o CSS (alvos de 44 px, coluna única no celular).
  - T017 verde.
- [X] T019 [US2] Aviso de equipe cheia:
  - ao cadastrar com 6 na equipe, a `Home` ou o menu mostram um toast `Party.FullOnAdd` com a ação "Ver equipe", que
    abre `_party`;
  - teste bUnit em `tests/EvTrainer.Tests/Components/Shell/PokemonMenuTests.cs`.
- [X] T020 [US2] `src/EvTrainer.Web/Components/Evs/EvSummary.razor`: o seletor do Exp. Share lista só a equipe, mais o
  portador atual se ele estiver guardado. Teste em `tests/EvTrainer.Tests/Components/Evs/EvSummaryTests.cs`.

## Phase 5: User Story 3 — Quantas batalhas com cada um na frente (P1)

**Goal**: o plano de batalhas com o menor restante e o menor número de batalhas.

**Independent Test**: os casos 1–6 de contracts/exp-share-plan.md batem e a página mostra 8 + 4 = 12 para o exemplo do
Bernie.

- [X] T021 [P] [US3] Testes em `tests/EvTrainer.Tests/Domain/ExpSharePlannerTests.cs`:
  - a tabela de casos 1–6 de contracts/exp-share-plan.md: fases, total, sobra e "sozinhos";
  - B nulo dá só `AAlone`;
  - `Useless` quando ninguém tem capacidade;
  - rendimento em 2 atributos com A completo num deles dá só `BAlone`;
  - teto de 510 (`Totals` somando 500, capacidade total 10);
  - meta acima de 255 é limitada a 255;
  - comparação com uma força bruta 4D (laços até a capacidade) em cerca de 200 casos aleatórios pequenos, com seed
    fixa: mesma sobra e mesmo total de batalhas.
- [X] T022 [US3] `src/EvTrainer.Web/Domain/ExpSharePlanner.cs` (data-model, research R2):
  - `PlanMon(int[] Totals, int[] Targets, bool Pokerus)`, `enum Setup`, `PlanPhase`, `SharePlan`;
  - `static Plan(PlanMon a, PlanMon? b, int[] yieldPerBattle)` com laços em `x1` e `x2`, e `x3`/`x4` máximos;
  - critérios: sobra, depois batalhas, depois configurações usadas, depois preferência pela ordem do enum;
  - multiplicador via `Evs.Multiplier`.
  - T021 verde.
- [X] T023 [P] [US3] Testes bUnit em `tests/EvTrainer.Tests/Components/Plan/ExpSharePlanViewTests.cs`:
  - por padrão, A é o selecionado e B o portador;
  - os seletores só têm Pokémon da equipe;
  - buscar "bernie" lista o treinador;
  - escolher a revanche de 3 Magneton mostra `.plan-phase` 8 e 4, "12 batalhas" e "sozinhos seriam 18";
  - com sobra, aparece o texto de restante;
  - um treinador de batalha dupla não pode ser escolhido e mostra o motivo;
  - um adversário selvagem (Magnemite) funciona;
  - um adversário inútil mostra a frase.
- [X] T024 [US3] `src/EvTrainer.Web/Components/Plan/ExpSharePlanView.razor` (contracts/exp-share-plan.md, UI):
  - o rendimento do treinador é a soma de `ReferenceData.Lookup(espécie).EvYield` do time;
  - parâmetros `InitialTrainerId` e `InitialStage` para o pré-preenchimento;
  - `Home` ganha `_expPlan` (com o treinador inicial);
  - link `.plan-battles` (`ExpShare.PlanLink`) no `EvSummary`, que abre a página;
  - textos pt/en e CSS.
  - T023 verde.

## Phase 6: User Story 4 — Aplicar a configuração (P2)

**Goal**: um toque deixa o jogo configurado para a fase (selecionado, Macho Brace, Exp. Share), com Desfazer.

**Independent Test**: aplicar a fase 2 do exemplo deixa Jolteon selecionado com o Macho Brace e Electabuzz com o
Exp. Share. O Desfazer restaura tudo.

- [X] T025 [P] [US4] Testes em `tests/EvTrainer.Tests/State/TrainerStorePartyTests.cs` (ou novo
  `TrainerStoreConfigurationTests.cs`):
  - `ApplyConfiguration(lead, holder)` seleciona `lead`, marca o Macho Brace nele e o tira do `holder`;
  - o `holder` fica com o Exp. Share (tirado de qualquer outro);
  - com `holder` nulo, ninguém fica com o Exp. Share;
  - o Desfazer restaura a seleção, os dois Macho Brace e o portador anterior;
  - um id inexistente devolve null.
- [X] T026 [US4] `TrainerStore.ApplyConfiguration(string leadId, string? holderId) → Action?` (research R5), num único
  `Commit`. T025 verde.
- [X] T027 [P] [US4] Testes bUnit em `ExpSharePlanViewTests.cs`:
  - "Usar esta configuração" aplica e mostra um toast com Desfazer;
  - depois de registrar uma batalha, a fase mostra uma a menos;
  - com um Pokémon guardado, a página pergunta "Colocar X na equipe?";
  - com a equipe cheia, oferece abrir a tela da equipe.
- [X] T028 [US4] Botão `.use-setup` por fase em `ExpSharePlanView.razor`, com a confirmação de entrar na equipe
  (`JoinParty`) e o atalho para `_party`. Textos pt/en. T027 verde.

## Phase 7: User Story 6 — Buscar pelo treinador (P2)

**Goal**: a busca pelo nome ou pela classe mostra cartões de treinador; os Pokémon dos times aparecem na grade.

**Independent Test**: "bernie" mostra o cartão com 2 revanches. "Registrar batalha inteira" dá +6 Sp. Atk, e o
Desfazer volta.

- [X] T029 [P] [US6] Testes em `tests/EvTrainer.Tests/Domain/SearchTests.cs`:
  - `Find("bernie")` inclui Magnemite e Magneton com `TrainerLocations` contendo "Rota 11";
  - uma espécie que já casava por outro critério não é duplicada.
- [X] T030 [US6] `OpponentSearch.Find` acrescenta as espécies dos times de `FindTrainers`, de todas as revanches, com o
  local do treinador. T029 verde.
- [X] T031 [P] [US6] Testes bUnit em `tests/EvTrainer.Tests/Components/Evs/TrainerCardTests.cs` e `EvTabFlowTests.cs`:
  - o cartão `.trainer-card[data-trainer=ENGINEER_BERNIE]` tem 2 `.trainer-stage`, com o time ("Magneton ×3 · Nv. 28")
    e "+6 Sp. Atk";
  - revanches que exigem a Elite Four têm a marca;
  - "Registrar batalha inteira" registra e o toast tem Desfazer (os EVs voltam);
  - em batalha dupla não há "Planejar com Exp. Share", só a marca;
  - "Planejar com Exp. Share" abre o plano já preenchido;
  - sem busca, não há cartões.
- [X] T032 [US6] `src/EvTrainer.Web/Components/Evs/TrainerCard.razor` e a seção `.trainer-results` em
  `src/EvTrainer.Web/Components/Evs/EvTab.razor`, acima das grades, só durante a busca.
  - Os EVs por batalha vêm da soma dos `EvYield` do time.
  - O toast junta os ganhos dos `SharedRecord` (principal e portador) e chama `UndoTeam`.
  - `EvTab` ganha `OnPlan(trainerId, stage)`, ligado à `Home`.
  - Textos pt/en e CSS.
  - T031 verde.

## Phase 8: User Story 5 — Registrar a batalha inteira no plano (P3)

**Goal**: registrar 1 batalha do treinador sem sair do plano.

**Independent Test**: no plano do Bernie, "Registrar 1 batalha" dá os EVs previstos aos dois, e o plano diminui uma
batalha.

- [X] T033 [P] [US5] Teste bUnit em `ExpSharePlanViewTests.cs`:
  - o botão `.record-battle` só aparece com treinador;
  - ele registra o time (`RecordTeam`) e o toast tem Desfazer (`UndoTeam`);
  - o plano é recalculado.
- [X] T034 [US5] Botão `.record-battle` em `ExpSharePlanView.razor`, reusando a montagem de toast do `TrainerCard`
  (extrair um helper, ex. `Components/Evs/BattleToast.cs`, se a lógica repetir). T033 verde.

## Phase 9: Polish

- [X] T035 [P] Cobrir as páginas novas (`PartyView`, `ExpSharePlanView`, `TrainerCard`) em
  `tests/EvTrainer.Tests/Localization/NoHardcodedTextTests.cs` e `EnglishScreensTests.cs`, e conferir
  `KeyParityTests.cs`.
- [X] T036 [P] Confirmar com teste que o JSON exportado (`Store.ExportBackup()`) tem `party` em cada Pokémon, e que
  `SyncServiceTests` leva o campo e vem com no máximo 6 na equipe após o merge.
- [X] T037 README: linha da feature (equipe, plano com Exp. Share, busca por treinador).
- [X] T038 `dotnet format PokemonEvTrainer.slnx`, `dotnet build --no-incremental` (0 avisos) e `dotnet test`.
- [X] T039 Conferência no navegador (quickstart.md, cenários 1–4) em 320 px e 1366 px: sem rolagem horizontal e alvos
  de toque ≥ 44 px.

## Dependencies & Execution Order

- **Fase 1 → Fase 2**: a Fase 2 bloqueia tudo.
  - T002 → T003 → T004/T005 → T006/T007.
  - T008/T009 e T010/T011 são independentes do `party`.
- **US1 (Fase 3)**: depende de T007.
- **US2 (Fase 4)**: depende de US1 (a página `_party` e o menu).
- **US3 (Fase 5)**: depende de T007 (seletores da equipe) e de T009 (busca de treinadores). É independente de US1 e
  US2.
- **US4 (Fase 6)**: depende de US3. T028 usa `JoinParty` de US2.
- **US6 (Fase 7)**: depende de T009 e T011. O botão "Planejar" depende de US3 (T024).
- **US5 (Fase 8)**: depende de US3 e de T011.
- **Polish**: depois de todas.

## Parallel Opportunities

- **Fase 2**: T002, T004, T008 e T010 são arquivos de teste diferentes e rodam juntos. Depois, T005 e T009 em paralelo.
- **US1 e US3** podem andar em paralelo após a Fase 2 (arquivos distintos: Shell × Domain/Plan).
- **Dentro de cada história**, os testes [P] de domínio e de bUnit podem ser escritos juntos.
  - Exemplo em US3: T021 (`ExpSharePlannerTests.cs`) e T023 (`ExpSharePlanViewTests.cs`).

## Implementation Strategy

1. **MVP**: Fases 1–4 (US1 e US2), a equipe de 6 com a tela de edição. Resolve o incômodo da lista longa sozinha.
2. **Plano** (US3 e US4): o ganho principal do pedido.
3. **Busca por treinador** (US6) e **batalha inteira no plano** (US5).
4. **Polish**, e então o squash na `main` com aprovação do usuário.
