# Feature Specification: Busca com úteis primeiro

**Feature Branch**: `009-useful-first-search`

**Created**: 2026-10-12

**Status**: Draft

**Input**: User description: "Ao fazer uma busca, por rota, ou qualquer outra, o ideal é que nos
primeiros resultados, venham o que são de fatos uteis para meu treino atual, inclusive fica sem fazer
sentido ficar com uteis marcado"

**Contexto**: na aba EVs, sem busca, o filtro **Úteis** mostra só os adversários que ajudam nas metas
do Pokémon selecionado (todo EV que o adversário dá cai num atributo que ainda não chegou à meta; um
adversário que joga EV num atributo já completo não é útil, porque EVs não podem ser desfeitos). Ao
digitar uma busca (nome, número, rota, líder, local de treinador), a lista passa a mostrar os
resultados na ordem da busca, **ignorando** o filtro — mas o botão **Úteis** continua aparecendo
marcado. Numa rota com muitos Pokémon, o jogador precisa procurar com os olhos quais servem para o
treino atual, e o filtro marcado sugere uma coisa que não está acontecendo.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Resultados da busca com os úteis primeiro (Priority: P1)

O jogador está treinando um Pokémon (por exemplo, metas em Ataque e Velocidade), chega numa rota e
digita "rota 3". Os adversários dessa rota que são úteis para o treino atual aparecem primeiro, sob o
título **"Úteis para as metas"**; os demais resultados da busca vêm depois, sob **"Outros
resultados"**, e continuam podendo ser tocados (às vezes o jogador é obrigado a enfrentar um deles).

**Why this priority**: é o pedido central; resolve o caso mais comum (buscar a rota onde está e saber
na hora quem enfrentar).

**Independent Test**: com um Pokémon com metas parciais, buscar uma rota que tenha adversários úteis e
não úteis e conferir que todos os úteis vêm antes de qualquer não útil, cada grupo com seu título.

**Acceptance Scenarios**:

1. **Given** um Pokémon com meta de Ataque não completa e Defesa completa, **When** o jogador busca
   uma rota com um adversário que dá só Ataque e outro que dá Defesa, **Then** o que dá Ataque aparece
   no grupo "Úteis para as metas" e o que dá Defesa no grupo "Outros resultados", depois dele.
2. **Given** uma busca em que nenhum resultado é útil, **When** a lista aparece, **Then** a lista
   aparece como hoje, com o título "Resultados", sem grupo de úteis vazio.
3. **Given** uma busca em que todos os resultados são úteis, **When** a lista aparece, **Then** só o
   grupo "Úteis para as metas" é mostrado, sem grupo "Outros resultados" vazio.
4. **Given** uma busca por treinador (com o local do treinador indicado no cartão), **When** os
   resultados são agrupados, **Then** o cartão continua mostrando o local do treinador.
5. **Given** resultados dentro de um mesmo grupo, **When** a lista aparece, **Then** eles mantêm a
   ordem que a busca já usa hoje.
6. **Given** o jogador registra uma batalha a partir de um resultado e com isso uma meta é completada,
   **When** a lista é atualizada, **Then** os adversários que deixaram de ser úteis passam para "Outros
   resultados" sem precisar refazer a busca.

---

### User Story 2 - O filtro não finge estar ativo durante a busca (Priority: P2)

Enquanto há texto na busca, o controle **Úteis / Todos** não aparece marcado de um jeito que não
corresponda ao que a lista mostra: durante a busca o controle fica **desativado, sem nenhuma opção
marcada** (continua no lugar, para a caixa de busca não mudar de tamanho enquanto o jogador digita), e
a lista mostra todos os resultados com os úteis primeiro (US1). Ao apagar a busca, o controle volta a
funcionar com a escolha de antes.

**Why this priority**: corrige a incoerência apontada pelo jogador, mas depende da US1 para fazer
sentido.

**Independent Test**: digitar uma busca e conferir o estado do controle de filtro; apagar a busca e
conferir que o filtro volta a funcionar como antes, com a escolha anterior preservada.

**Acceptance Scenarios**:

1. **Given** o filtro em "Úteis" e uma busca digitada, **When** o jogador olha o controle, **Then**
   ele aparece desativado, sem "Úteis" nem "Todos" marcado, e tocar nele não faz nada.
2. **Given** uma busca digitada, **When** o jogador apaga a busca, **Then** a lista volta ao modo sem
   busca com a mesma escolha de filtro de antes (Úteis ou Todos).

---

### User Story 3 - "Todos" também traz os úteis primeiro (Priority: P3)

Sem busca, com o filtro em **Todos**, a lista mostra primeiro os adversários úteis e depois os demais,
com os mesmos dois títulos, em vez de uma lista única na ordem da Pokédex.

**Why this priority**: mesma ideia do pedido aplicada à lista completa; útil, mas o jogador raramente
usa "Todos" sem buscar.

**Independent Test**: com o filtro em Todos e sem busca, conferir que todos os úteis aparecem antes de
qualquer não útil.

**Acceptance Scenarios**:

1. **Given** filtro em Todos e nenhuma busca, **When** a lista aparece, **Then** o grupo "Úteis para as
   metas" vem primeiro e "Outros" depois, cada um na ordem atual.

---

### Edge Cases

- **Pokémon sem metas definidas** (todas zero) ou com todas as metas completas: nenhum adversário é
  útil; a busca mostra só os resultados, como hoje, sem grupo de úteis vazio.
- **Nenhum Pokémon selecionado**: não há o que ordenar; a busca mostra os resultados como hoje.
- **Vitaminas** nos resultados: seguem a mesma regra de utilidade já usada no filtro (inclusive o
  limite de uso de vitaminas).
- **Adversário que dá EV em dois atributos, um com meta aberta e outro completo**: não é útil (regra
  atual), vai para "Outros resultados".
- **Recentes**: continuam ocultos durante a busca, como hoje.
- **Busca sem resultado**: continua a mensagem atual de "Nada encontrado".
- **Troca de Pokémon selecionado** com a busca aberta: o agrupamento passa a refletir as metas do novo
  Pokémon.

## Clarifications

### Session 2026-10-12

- Q: O que o controle Úteis/Todos faz durante a busca? → A: fica desativado (sem opção marcada) e a
  busca mostra todos os resultados com os úteis primeiro; ao apagar a busca, volta a escolha anterior.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Ao buscar, o app DEVE mostrar primeiro os resultados úteis para as metas do Pokémon
  selecionado e depois os demais resultados.
- **FR-002**: "Útil" DEVE seguir exatamente a mesma regra já usada pelo filtro Úteis (nenhum EV do
  adversário cai em atributo com meta completa; vitaminas respeitam o limite de uso).
- **FR-003**: Quando há resultados úteis, cada grupo DEVE ter um título visível ("Úteis para as metas"
  / "Outros resultados") e um grupo vazio NÃO DEVE ser mostrado; quando nenhum resultado é útil, a
  lista DEVE aparecer como hoje, com o título "Resultados".
- **FR-004**: Dentro de cada grupo, a ordem DEVE ser a ordem atual da busca.
- **FR-005**: Resultados não úteis DEVEM continuar tocáveis para registrar batalha, sem confirmação
  extra.
- **FR-006**: O agrupamento DEVE ser recalculado após cada batalha registrada, desfeita ou edição de
  EVs/metas, sem o jogador refazer a busca.
- **FR-007**: Durante a busca, o controle Úteis/Todos DEVE ficar desativado, sem nenhuma opção marcada
  (também para leitores de tela), mantendo seu lugar na tela; a busca sempre mostra todos os
  resultados, com os úteis primeiro.
- **FR-008**: A escolha de filtro do jogador DEVE ser preservada ao apagar a busca.
- **FR-009**: Sem busca e com o filtro em Todos, a lista DEVE mostrar os úteis primeiro, com os mesmos
  títulos de grupo.
- **FR-010**: Os novos textos DEVEM estar no arquivo de textos da interface (pt-BR), sem texto fixo nas
  telas, e o comportamento DEVE ser o mesmo no celular e no desktop.

### Key Entities

- **Resultado de busca**: um adversário encontrado (com o local do treinador quando a busca casou por
  treinador) e se ele é útil ou não para o Pokémon selecionado — a utilidade é calculada na hora, nada
  novo é salvo.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Em qualquer busca, 100% dos resultados úteis aparecem antes do primeiro resultado não
  útil.
- **SC-002**: Numa busca de rota, o jogador identifica o primeiro adversário útil sem rolar a tela
  sempre que existir pelo menos um útil.
- **SC-003**: Em nenhum momento o controle de filtro aparece marcado de forma diferente do que a lista
  mostra.
- **SC-004**: Nenhuma regressão: sem busca e com o filtro em Úteis, a lista mostra exatamente os mesmos
  adversários de antes.

## Assumptions

- A regra de utilidade não muda; esta feature só muda a ordem e o agrupamento dos resultados.
- "Outros resultados" não recebe destaque negativo além do título do grupo (os cartões continuam
  iguais).
- A ordenação usa só "útil / não útil" (não há ranking por quantidade de EV ou por proximidade da meta).
- Nenhum dado novo é salvo; backup, sincronização e formato de dados não mudam.
- O título "Resultados" atual é substituído pelos dois títulos de grupo quando há separação; quando
  nenhum resultado é útil, o grupo único usa o título "Resultados" de hoje.
