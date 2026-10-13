# Feature Specification: Versão do jogo (FireRed / LeafGreen)

**Feature Branch**: `014-game-version`

**Created**: 2026-10-13

**Status**: Draft

**Input**: User description: "Vale lembrar que o app é para ser usado tanto para FireRed e LeafGreen, então se for
necessário, tem que fazer ajustar de quais pokemons mudam de uma versão para outra, sendo assim o usuário deveria poder
escolher a versão do jogo na aplicação para que o filtro seja aplicado de forma correta."

**Contexto**: o app atende FireRed e LeafGreen. Conferido no código do jogo (decompilação `pret/pokefirered`): os
**times dos treinadores são iguais** nas duas versões (inclusive revanches do VS Seeker), mas os **Pokémon selvagens**
mudam — 17 espécies só aparecem no FireRed (Ekans, Arbok, Oddish, Gloom, Psyduck, Golduck, Growlithe, Shellder, Seadra,
Scyther, Electabuzz, Weezing, Wooper, Murkrow, Qwilfish, Delibird, Skarmory) e 17 só no LeafGreen (Sandshrew,
Sandslash, Vulpix, Bellsprout, Weepinbell, Slowpoke, Slowbro, Staryu, Kingler, Muk, Magmar, Pinsir, Marill,
Misdreavus, Sneasel, Remoraid, Mantine), e as tabelas de encontro de vários locais diferem. Hoje a busca por local
usa só os selvagens do FireRed, e algumas anotações de local selvagem citam "(FR)".

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Escolher a versão do jogo (Priority: P1)

O jogador indica no app se joga FireRed ou LeafGreen. A partir daí, a busca por local mostra os Pokémon selvagens da
versão escolhida: em LeafGreen, buscar "rota 3" não traz Ekans (exclusivo do FireRed) como selvagem; buscar "rota 24"
traz Bellsprout; na Safari Zone aparece Pinsir em vez de Scyther.

**Why this priority**: é o pedido; sem a versão certa, a busca sugere Pokémon que não existem no jogo do jogador.

**Independent Test**: escolher LeafGreen, buscar um local onde as versões diferem e conferir que só aparecem os
selvagens do LeafGreen (e os dos treinadores, que são iguais); trocar para FireRed e conferir o inverso.

**Acceptance Scenarios**:

1. **Given** a versão LeafGreen, **When** o jogador busca a Safari Zone, **Then** Pinsir aparece entre os selvagens e
   Scyther não aparece como selvagem.
2. **Given** a versão FireRed, **When** o jogador busca a Safari Zone, **Then** Scyther aparece e Pinsir não.
3. **Given** qualquer versão, **When** o jogador busca um local com treinadores, **Then** os Pokémon dos treinadores
   são os mesmos nas duas versões, com o mesmo indicador de local de treinador.
4. **Given** a versão escolhida, **When** o jogador fecha e reabre o app, **Then** a versão continua a mesma.
5. **Given** o app sem versão escolhida (primeiro uso ou dados antigos), **When** o jogador usa a busca, **Then** o app
   considera FireRed e mostra onde trocar a versão.

---

### User Story 2 - Versão visível e fácil de trocar (Priority: P2)

O jogador vê qual versão está ativa e troca com um toque, sem perder o que estava fazendo (Pokémon selecionado, busca
digitada). A troca reflete na hora nos resultados da busca.

**Why this priority**: evita resultados errados por esquecer a versão escolhida, e quem joga as duas versões alterna.

**Independent Test**: com uma busca aberta, trocar a versão e ver os resultados mudarem sem recarregar.

**Acceptance Scenarios**:

1. **Given** uma busca aberta, **When** o jogador troca a versão, **Then** os resultados são recalculados na hora e a
   busca digitada continua.
2. **Given** o menu aberto, **When** o jogador olha as opções, **Then** vê "FireRed" e "LeafGreen" com a ativa marcada.

---

### Edge Cases

- **Pokémon exclusivo como adversário de treinador**: aparece normalmente na busca pelo local do treinador nas duas
  versões (os times não mudam); só não aparece como selvagem na versão em que não existe.
- **Registrar batalhas e EVs**: não dependem da versão; trocar a versão não altera nenhum dado salvo, histórico,
  backup ou sincronização.
- **Filtros "Úteis" e "Todos" sem busca**: listam todas as espécies como hoje (o jogador pode enfrentar qualquer uma em
  trocas, treinadores ou eventos); a versão só afeta o que é mostrado como encontrado em cada local.
- **Anotações curadas de local selvagem** que citam uma versão (ex.: "Rota 7, 8 (FR)"): passam a valer só para a versão
  citada, ou são substituídas pelos dados do jogo.
- **Guia "Onde treinar"**: inalterado (só treinadores, iguais nas duas versões).
- **Captura e avaliação de IVs**: inalteradas.
- **Idioma**: nomes "FireRed" e "LeafGreen" iguais nos dois idiomas.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O app MUST permitir escolher a versão do jogo: FireRed ou LeafGreen.
- **FR-002**: Sem escolha feita, a versão considerada MUST ser FireRed.
- **FR-003**: A escolha MUST ficar salva no aparelho e valer nas próximas aberturas.
- **FR-004**: A busca por local MUST considerar como selvagens de cada local apenas os Pokémon encontrados naquele
  local na versão escolhida (grama, água, Rock Smash e pesca).
- **FR-005**: A busca por local MUST continuar mostrando os Pokémon dos treinadores daquele local, iguais nas duas
  versões.
- **FR-006**: Anotações de local selvagem específicas de uma versão MUST NOT aparecer nem casar na busca quando a outra
  versão estiver escolhida.
- **FR-007**: A troca de versão MUST atualizar os resultados na hora, sem recarregar e sem perder o estado da tela.
- **FR-008**: A versão ativa MUST estar visível e poder ser trocada com no máximo 2 toques de qualquer tela.
- **FR-009**: Trocar a versão MUST NOT alterar dados salvos, backup ou sincronização.
- **FR-010**: Os textos novos MUST vir dos recursos de idioma (pt e en).

### Key Entities

- **Versão do jogo**: FireRed ou LeafGreen; preferência do aparelho.
- **Selvagens por local e versão**: para cada local, a lista de espécies selvagens no FireRed e no LeafGreen.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Em 100% dos locais, os selvagens mostrados na busca correspondem à tabela de encontros da versão
  escolhida.
- **SC-002**: Nenhum dos 17 exclusivos do FireRed aparece como selvagem na busca com LeafGreen escolhido, e vice-versa.
- **SC-003**: Os Pokémon de treinadores encontrados por local são idênticos nas duas versões.
- **SC-004**: O jogador troca a versão em no máximo 2 toques, e a escolha persiste em 100% das reaberturas.

## Assumptions

- A versão é uma **preferência do aparelho** (como tema e idioma), não um dado de cada Pokémon nem do backup: o caso
  comum é um jogo por aparelho. Quem joga as duas versões alterna pelo menu.
- O padrão FireRed preserva o comportamento atual para quem já usa o app.
- Fonte: tabelas de encontro de FireRed e de LeafGreen da decompilação `pret/pokefirered`
  (`src/data/wild_encounters.json`), as mesmas usadas na busca por local.
- Times de treinadores e revanches são idênticos nas duas versões (verificado: o código do jogo não tem diferenças por
  versão nos times).
