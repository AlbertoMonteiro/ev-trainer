# Research: Versão do jogo

## R1 — O que muda entre as versões

- **Decision**: só os selvagens. Verificado na decompilação: `trainer_parties.h` e `vs_seeker.c` não têm diferenças
  por versão; `wild_encounters.json` tem 124 tabelas `_FireRed` e 124 `_LeafGreen`, com 17 espécies exclusivas de cada.
- **Rationale**: FR-004/FR-005 com o mínimo de mudança.

## R2 — Formato dos dados

- **Decision**: em `locations.json`, `"wild": { "fireRed": [...], "leafGreen": [...] }` por local; o gerador lê as duas
  tabelas. `ReferenceData.WildPlaces(version)` devolve espécie → locais.
- **Alternatives considered**: dois arquivos — duplicaria treinadores e nomes.

## R3 — Anotações curadas `wildLocation`

- **Decision**: para Pokémon (não vitaminas), a busca deixa de indexar `wildLocation` curado; os selvagens vêm só dos
  dados do jogo por versão. Vitaminas mantêm a anotação ("Celadon Dept. Store"). Rivais e líderes citados nas notas
  curadas já estão nos treinadores gerados (Pallet Town, Rota 22, Cerulean City, ginásios).
- **Rationale**: FR-006 sem curadoria por versão; as notas eram de FireRed ("Rota 7, 8 (FR)") e duplicavam o que os
  dados do jogo já cobrem.
- **Alternatives considered**: manter as notas e marcar por versão à mão — trabalho manual e sujeito a erro.

## R4 — Busca por versão

- **Decision**: `OpponentSearch` monta dois índices na construção e `Find(query, version)` escolhe; os treinadores são
  os mesmos nos dois. A aba EVs passa `Store.Preferences.GameVersion`.
- **Rationale**: troca instantânea (FR-007); custo de memória desprezível.

## R5 — Preferência e menu

- **Decision**: `UiPreferences.GameVersion` = `"fireRed"` (padrão) | `"leafGreen"`; valor inválido lido como
  `"fireRed"`. Menu: grupo `Menu.GameVersion` com botões `data-version` e `aria-pressed`, logo abaixo de Tema.
- **Rationale**: FR-001–FR-003, FR-008 (menu = 2 toques), segue o padrão de Tema/Idioma.
