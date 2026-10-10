---

description: "Task list for Adversários de todas as espécies do FireRed"
---

# Tasks: Adversários de todas as espécies do FireRed

**Input**: Design documents from `/specs/003-all-gen3-opponents/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/reference-data.md](./contracts/reference-data.md),
[quickstart.md](./quickstart.md)

**Tests**: OBRIGATÓRIOS (constituição, Princípio III). Escrever, rodar com
`dotnet test --solution PokemonEvTrainer.slnx`, ver falhar, então implementar.

**Organization**: US1 (registrar qualquer espécie, P1) e US2 (listas Úteis/Todos com as 386, P2).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)
- **[Story]**: US1, US2

## Convenções

- Mesmas das features 001/002: dotnet CLI primeiro, `dotnet format PokemonEvTrainer.slnx` após cada
  tarefa com código, domínio sem Blazor/IO, nenhum texto visível fixo.
- Vetores `[HP, Ataque, Defesa, SpAtk, SpDef, Speed]`.
- `reference-data.json` NÃO é editado.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: EV yield das 386 espécies no `pokedex-gen3.json`.

- [X] T001 Em `scripts/BuildPokedex.cs`, gravar para cada espécie o campo `evYield` (int[6], na ordem `hp, attack, defense, special-attack, special-defense, speed`) com o valor **atual** de `stats[].effort` de `pokemon/{id}` — **sem** aplicar `past_stats` ao effort (research R1); atualizar o texto de `_fonte` para "PokéAPI (pokeapi.co): stats base da Geração III (past_stats aplicados) e EV yield (effort atual). Gerado por scripts/BuildPokedex.cs."
- [X] T002 Rodar `dotnet run scripts/BuildPokedex.cs` na raiz e conferir `pokedex-gen3.json: 386 species`, Milotic (#350) com `"evYield": [0, 0, 0, 0, 2, 0]` e `base`/`evolvesFrom` inalterados em relação ao arquivo atual (só o campo novo muda)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: 386 espécies + 6 vitaminas como adversários.

**⚠️ CRITICAL**: bloqueia US1 e US2.

### Testes (escrever primeiro, ver falhar)

- [X] T003 [P] Ampliar `tests/EvTrainer.Tests/Domain/PokedexTests.cs`: toda espécie tem `EvYield` com 6 posições e soma ≥ 1; Milotic ⇒ `[0, 0, 0, 0, 2, 0]`; Zigzagoon ⇒ `[0, 0, 0, 0, 0, 1]`; Shedinja ⇒ `[2, 0, 0, 0, 0, 0]`; valores fixados das 8 espécies com histórico divergente na PokéAPI (research R1): Yanma `[0,0,0,0,0,1]`, Slowking `[0,0,0,0,2,0]`, Misdreavus `[0,0,0,0,1,0]`, Blissey `[3,0,0,0,0,0]`, Delcatty `[1,0,0,0,0,1]`, Roselia `[0,0,0,2,0,0]`, Duskull `[0,0,0,0,1,0]`, Dusclops `[0,0,1,0,1,0]`
- [X] T004 [P] Reescrever `Loads_151_pokemon_and_6_vitamins` em `tests/EvTrainer.Tests/Domain/ReferenceDataTests.cs` para `Loads_386_species_and_6_vitamins`: `Opponents.Count == 392`; os 386 primeiros têm `Dex` 1..386 em ordem, sem lacunas; os 6 últimos são as vitaminas (`HP Up, Protein, Iron, Calcium, Zinc, Carbos`); para todo dex 1–151, `Pokedex.Find(nome).EvYield` é igual ao `EvYield` do adversário vindo do `reference-data.json` ("0 diferenças", SC-002); Mankey mantém `WildLocation` "Rota 3, 4, 22"; Milotic tem `WildLocation` vazio; `Lookup("Milotic")` ⇒ dex 350, `[0,0,0,0,2,0]`; os 34 locais de `TrainersByLocation` continuam os mesmos

### Implementação

- [X] T005 Adicionar `int[] EvYield` a `PokedexSpecies` e ler `evYield` em `Pokedex.LoadEmbedded()` em `src/EvTrainer.Web/Domain/Reference/Pokedex.cs` (atualizar construções de `PokedexSpecies` nos testes, se houver) — faz T003 passar
- [X] T006 Em `src/EvTrainer.Web/Domain/Reference/ReferenceData.cs`, fazer `LoadEmbedded()` ler também `EvTrainer.pokedex-gen3.json` (via `Pokedex.LoadEmbedded()` ou leitura direta do recurso) e montar `Opponents` = Pokémon do `reference-data.json` (#1–151) + `Opponent(Dex, Name, EvYield, "", false)` para cada espécie da Pokédex com `Dex > 151`, em ordem de dex, + as 6 vitaminas no fim; manter `Canon`, `Lookup`, `TrainersByLocation` e natures como estão — faz T004 passar

**Checkpoint**: 392 adversários carregados, Kanto idêntico.

---

## Phase 3: User Story 1 - Registrar batalha contra qualquer espécie (Priority: P1) 🎯 MVP

**Goal**: buscar e registrar qualquer espécie #1–386 com o EV correto.

**Independent Test**: aba EVs → "milo" ⇒ Milotic com "+2 SPD"; tocar ⇒ SpDef +2.

### Tests for User Story 1 ⚠️

- [X] T007 [P] [US1] Ampliar `tests/EvTrainer.Tests/Domain/SearchTests.cs`: `"milo"` contém Milotic; `"350"` e `"#350"` contêm Milotic; `"zigza"` contém Zigzagoon; espécies novas não trazem `TrainerLocations`
- [X] T008 [P] [US1] Ampliar `tests/EvTrainer.Tests/Domain/EvTotalsTests.cs`: `Milotic ×1` ⇒ `[0,0,0,0,2,0]`; `Milotic ×1 (m=4)` ⇒ `[0,0,0,0,8,0]`; `Milotic ×200` respeita 255/510
- [X] T009 [P] [US1] Ampliar `tests/EvTrainer.Tests/Domain/RecentTests.cs`: eventos com `Milotic` e `Caterpie` ⇒ Recentes `["Caterpie", "Milotic"]` (mais recente primeiro conforme a ordem dos eventos)
- [X] T010 [P] [US1] Ampliar `tests/EvTrainer.Tests/Components/Evs/OpponentSearchTests.cs`: buscar "milo" no `EvTab` ⇒ card `button.opponent[data-opp='Milotic']` com etiqueta `Evs.Card.Yield` para `+2 SPD` e `img[src='sprites/350.png']`; tocar ⇒ toast "Milotic: +2 SpDef" (`Toast.EvGain` com `Evs.Gain.Part` e `Stat.Long.4`) e o repositório recebe o evento

### Implementation for User Story 1

- [X] T011 [US1] Rodar `dotnet test`; se algum teste de T007–T010 falhar, corrigir no domínio (`OpponentSearch`, `Evs`, `BattleLog`) sem caso especial para espécies novas — espera-se que passem só com a Fundação (todas as regras já usam `ReferenceData.Opponents`)

**Checkpoint**: MVP — qualquer espécie do jogo pode ser registrada.

---

## Phase 4: User Story 2 - Listas Úteis e Todos com as 386 espécies (Priority: P2)

**Goal**: listas completas em ordem da Pokédex e fluidas com 392 cards.

**Independent Test**: "Todos" ⇒ 392 cards (#1–386 + vitaminas); "Úteis" inclui Zigzagoon e não Machop
com metas `[252, 0, 2, 252, 0, 4]`.

### Tests for User Story 2 ⚠️

- [X] T012 [P] [US2] Ampliar `tests/EvTrainer.Tests/Domain/UsefulFilterTests.cs`: com metas `[252, 0, 2, 252, 0, 4]` e totais `[0, 0, 2, 0, 0, 0]`, Zigzagoon é útil, Milotic não (meta SpD 0), Machop não
- [X] T013 [P] [US2] Ampliar `tests/EvTrainer.Tests/Components/Evs/OpponentSearchTests.cs`: com filtro "Todos" e sem eventos, `.main-list button.opponent` tem 392 itens; os `data-opp` seguem a ordem de `ReferenceData.Opponents` (Bulbasaur primeiro, Deoxys antes das vitaminas, Carbos por último)
- [X] T014 [P] [US2] Teste `tests/EvTrainer.Tests/Domain/CountsByOpponentTests.cs`: para eventos com aliases (`Nidoran F`, `Bulbasaur (rival)`), espécie nova (`Milotic`) e nome desconhecido, `Evs.CountsByOpponent(events, data)` tem, para cada adversário, o mesmo valor de `Evs.CountFor(events, nome, data)` (ausente ⇒ 0) e ignora o desconhecido

### Implementation for User Story 2

- [X] T015 [US2] Implementar `Evs.CountsByOpponent(IReadOnlyList<BattleEvent> events, ReferenceData data)` em `src/EvTrainer.Web/Domain/Evs.cs` (uma passada; chave = nome canônico; `StringComparer.Ordinal`) — faz T014 passar
- [X] T016 [US2] Em `src/EvTrainer.Web/Components/Evs/OpponentGrid.razor`, calcular `CountsByOpponent` uma vez por renderização e passar `counts.GetValueOrDefault(item.Opponent.Name)` para cada `OpponentCard` (remover o `CountFor` por card) — T013 continua passando
- [X] T017 [US2] Rodar `dotnet format` e `dotnet test`; validar [quickstart.md §3](./quickstart.md) passos 3 e 4

**Checkpoint**: listas completas e fluidas.

---

## Phase 5: Polish & Cross-Cutting Concerns

- [X] T018 Conferir que `tests/EvTrainer.Tests/Localization/NoHardcodedTextTests.cs` e os demais testes existentes continuam verdes com 392 cards (nomes das espécies novas já são dados via `Dex.Species`)
- [X] T019 Validar no app em 360 px ([quickstart.md §3](./quickstart.md)): buscar "milo" e "350", registrar Milotic, rolar "Todos" até as vitaminas sem travar e sem rolagem horizontal
- [X] T020 [P] Atualizar `README.md` (seção Features): adversários de todas as 386 espécies da Geração III, com EVs de Kanto curados e o restante da PokéAPI
- [X] T021 Rodar `dotnet format PokemonEvTrainer.slnx --verify-no-changes` e `dotnet test --solution PokemonEvTrainer.slnx` (tudo verde) como portão final

---

## Dependencies & Execution Order

- **Setup (T001–T002)** → **Fundação (T003–T006)** → US1 e US2 (independentes entre si) → Polish.
- T005 antes de T006 (ReferenceData usa `EvYield`).
- T016 depende de T015.
- Tarefas que editam `OpponentSearchTests.cs` (T010, T013) são sequenciais entre si.

### Parallel Opportunities

- T003 e T004; T007, T008, T009 (arquivos diferentes); T012 e T014; T020 a qualquer momento após T006.

---

## Parallel Example: User Story 1

```bash
Task: "Busca milo/350 em tests/EvTrainer.Tests/Domain/SearchTests.cs"
Task: "Milotic soma +2 SpDef em tests/EvTrainer.Tests/Domain/EvTotalsTests.cs"
Task: "Recentes com Milotic em tests/EvTrainer.Tests/Domain/RecentTests.cs"
```

---

## Implementation Strategy

### MVP First

1. T001–T002 (dados) → T003–T006 (Fundação) → T007–T011 (US1).
2. **Validar**: "milo" acha e registra Milotic.

### Incremental Delivery

1. Fundação + US1 → qualquer espécie registrável.
2. US2 → listas completas e contagem otimizada.
3. Polish → validação no app, README, portão final.

---

## Notes

- Commits só quando o usuário pedir; mensagens sem menção a ferramentas de IA.
- Risco documentado (research R1): yields das 8 espécies com histórico divergente na PokéAPI ficam
  fixados por teste; mudar exige decisão consciente.
