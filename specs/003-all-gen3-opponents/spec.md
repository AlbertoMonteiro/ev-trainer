# Feature Specification: Adversários de todas as espécies do FireRed

**Feature Branch**: `003-all-gen3-opponents`

**Created**: 2026-10-09

**Status**: Draft

**Input**: User description: "Como a ideia é ser possivel usar no FireRed, então a aplicação deve ter
todos pokemons disponiveis no jogo, e não so a pokedex de Kanto, vi que é possivel adicionar todos os
386, mas na tela de evs, ao buscar "Milo" que deveria ser Milotic, não traz nada"

**Contexto**: a feature 001 limitou os adversários aos 151 de Kanto + 6 vitaminas
(`reference-data.json`). A Pokédex embutida (correção da 001) e os sprites (002) já cobrem #1–386.
No FireRed há Pokémon de outras gerações (Sevii Islands, trocas, eventos), então a contagem de EVs
precisa aceitar qualquer espécie #1–386.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Registrar batalha contra qualquer espécie (Priority: P1)

O jogador derrota um Pokémon que não é de Kanto (ex.: um Milotic de treinador, um Sneasel nas
Sevii Islands), busca pelo nome ou número na aba EVs, encontra e toca para registrar; o app soma os
EVs corretos daquela espécie.

**Why this priority**: sem isso o app não conta parte das batalhas reais do FireRed — o objetivo
central do app.

**Independent Test**: buscar "milo" na aba EVs ⇒ Milotic aparece; tocar ⇒ +2 SpDef.

**Acceptance Scenarios**:

1. **Given** a aba EVs, **When** o jogador busca "milo", **Then** Milotic aparece nos resultados com
   a etiqueta "+2 SPD" e o sprite.
2. **Given** o resultado Milotic, **When** o jogador toca, **Then** a batalha é registrada e o SpDef
   sobe 2 (respeitando limites, Pokérus e Macho Brace como nos demais).
3. **Given** uma busca pelo número ("#350" ou "350"), **Then** Milotic aparece.
4. **Given** uma espécie de #152–386 já registrada, **Then** ela aparece em Recentes, no histórico e
   na contagem `×N` como as de Kanto.

---

### User Story 2 - Listas Úteis e Todos com as 386 espécies (Priority: P2)

Sem texto na busca, "Todos" lista as 386 espécies em ordem da Pokédex (vitaminas no fim) e "Úteis"
inclui qualquer espécie que ainda ajude as metas.

**Why this priority**: ao planejar o treino o jogador precisa ver todas as opções úteis, não só as
de Kanto.

**Independent Test**: com metas definidas, "Todos" mostra 392 cards (386 + 6 vitaminas); "Úteis"
inclui espécies como Zigzagoon (+1 Speed) quando a meta de Speed não foi atingida.

**Acceptance Scenarios**:

1. **Given** o filtro "Todos", **Then** a lista tem as 386 espécies em ordem da Pokédex e depois as
   6 vitaminas.
2. **Given** metas `[252, 0, 2, 252, 0, 4]` e Speed = 0, **Then** "Úteis" inclui Zigzagoon e não
   inclui Machop.

---

### Edge Cases

- Espécies sem local selvagem ou de treinador cadastrado (todas de #152–386 nesta versão): aparecem
  pela busca por nome ou número; o local fica vazio (sem erro).
- Backups antigos e eventos já gravados continuam valendo; nomes de espécies novas passam a contar
  EVs (antes contavam zero por serem desconhecidos).
- Os valores de Kanto (#1–151) DEVEM continuar exatamente os do `reference-data.json`.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Os adversários DEVEM incluir todas as espécies #1–386 com o EV yield da Geração III,
  mais as 6 vitaminas.
- **FR-002**: Os EV yields de #1–151 DEVEM ser os do `reference-data.json` (curadoria existente);
  os de #152–386 DEVEM vir de uma fonte pública verificável, validada contra os 151 curados.
- **FR-003**: Busca, filtro Úteis, Recentes, contagem, histórico, importação de planilha e cálculo de
  EVs DEVEM tratar as espécies novas exatamente como as de Kanto.
- **FR-004**: A lista "Todos" DEVE seguir a ordem da Pokédex (#1–386) com as vitaminas no fim.
- **FR-005**: A experiência de rolagem e toque DEVE continuar fluida com a lista maior (392 cards).

### Key Entities

- **Adversário**: espécie (#1–386) ou vitamina, com EV yield; locais selvagens e de treinadores só
  existem para Kanto nesta versão.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das 386 espécies podem ser encontradas pela busca por nome e por número e
  registradas com o EV yield correto.
- **SC-002**: Os 151 EV yields de Kanto permanecem idênticos aos atuais (0 diferenças).
- **SC-003**: Registrar uma batalha continua levando 1 toque e o ganho aparece em menos de 1 segundo
  com a lista "Todos" aberta.

## Assumptions

- EV yields da Geração III para #152–386 obtidos da PokéAPI (valor atual de `effort`): conferido que
  esse valor coincide com os 151 curados; o histórico `past_stats` não é usado para EVs (introduz
  erros em Raichu, Alakazam e Electrode).
- Locais selvagens e treinadores das Sevii Islands e de espécies de outras gerações ficam fora do
  escopo (continua a pendência do HANDOFF §7).
