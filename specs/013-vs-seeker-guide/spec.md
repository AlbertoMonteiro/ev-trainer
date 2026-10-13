# Feature Specification: Guia de treino com VS Seeker

**Feature Branch**: `013-vs-seeker-guide`

**Created**: 2026-10-13

**Status**: Draft

**Input**: User description: "não da para assumir que só será usado um vs seeker, por exemplo a rota 8, que tem a
batalha de dupla e a lass Julia, elas estão distantes, então ao usar o vs seeker, não ativa para as duas, só ativa para
quem está no campo de visão, e elas nunca vão estar no mesmo campo de visão. Sendo assim, seria interessante ter uma
página dedicada na aplicação já trazendo essas dicas de locais, ricas em detalhes como você trouxe. É assim, seria
importante incluir treinadores para treinar speed também. Como você viu que muda ao derrotar elite four, então a página
deveria ter essa opção, para listar com e depois de derrotar a E4"

**Contexto**: em FireRed, o **VS Seeker** reativa os treinadores que estão **na tela** quando ele é usado (precisa de
cerca de 100 passos para recarregar). Treinadores da mesma rota mas longe um do outro (ex.: Twins Eli & Anne e Lass
Julia na Rota 8) exigem usos separados. Os times de revanche ficam mais fortes conforme o jogo avança — alguns só
aparecem com os níveis mais altos depois de derrotar a Elite Four. Hoje o jogador precisa pesquisar fora do app quais
treinadores dão os EVs que ele quer; nas conversas de treino foram levantadas listas por atributo (HP, Ataque,
Sp. Atk) com local, time, nível e EVs por batalha.

## Clarifications

### Session 2026-10-13

- Q: Como a página deve mostrar a evolução do time de cada treinador entre as revanches, já que alguns (ex.: Engineer
  Bernie) mudam o time mais de uma vez antes da Elite Four? → A: Cada treinador mostra todos os estágios em sequência
  (original → 1ª revanche → 2ª revanche…), cada um com time, níveis e EVs, marcando os que exigem a Elite Four; o filtro
  "antes/depois da Elite Four" esconde os estágios ainda indisponíveis; a ordenação usa o melhor estágio disponível.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver onde treinar cada atributo com o VS Seeker (Priority: P1)

O jogador abre a página **"Onde treinar"** e escolhe um atributo (HP, Ataque, Sp. Atk ou Speed). A página lista os
treinadores de revanche que dão esse atributo, do melhor para o pior, cada um com: nome e classe, local (rota e
referência de onde fica), time com níveis, EVs por batalha (indicando quando também dá outros atributos) e observações
(ex.: "precisa de Surf", "batalha dupla"). Cada treinador é uma entrada própria: a página nunca soma treinadores como se
um único uso do VS Seeker reativasse todos.

**Why this priority**: é o pedido central; substitui a pesquisa fora do app.

**Independent Test**: abrir a página, escolher HP e conferir que aparecem Twins Eli & Anne e Lass Julia (Rota 8) como
entradas separadas, com time, nível e "4 HP" cada; escolher Sp. Atk e ver o Engineer Bernie (Rota 11, 6 Sp. Atk) no topo.

**Acceptance Scenarios**:

1. **Given** a página aberta, **When** o jogador escolhe "HP", **Then** vê os treinadores que dão HP ordenados por HP
   por batalha (maior primeiro), cada um com local, time, níveis e EVs.
2. **Given** um treinador cujo time também dá outros atributos (ex.: Beauty Grace: 3 HP + 3 Speed), **When** ele
   aparece na lista de HP, **Then** os outros EVs aparecem destacados como "também dá", para o jogador avaliar se
   atrapalha o treino.
3. **Given** dois treinadores na mesma rota, **When** aparecem na lista, **Then** são entradas separadas, cada uma com
   seus EVs; a página não mostra soma entre treinadores.
4. **Given** um treinador que exige Surf, Cycling Road ou outra condição de acesso, **When** aparece, **Then** a
   condição é indicada.
5. **Given** a página, **When** o jogador escolhe "Speed", **Then** vê treinadores de Speed com as mesmas informações.

---

### User Story 2 - Estágios das revanches, antes ou depois da Elite Four (Priority: P1)

Cada treinador mostra **todos os estágios** do time em sequência — batalha original, 1ª revanche, 2ª revanche… — cada
um com Pokémon, níveis e EVs por batalha, porque o time muda a cada revanche (ex.: o Engineer Bernie começa com
Magnemite e Magneton e, numa revanche seguinte, passa a 3 Magneton, ainda antes da Elite Four). Estágios que só
aparecem depois de derrotar a Elite Four são marcados. O jogador indica se já derrotou a Elite Four: com "antes", esses
estágios ficam ocultos; com "depois", todos aparecem. A lista é ordenada pelo **melhor estágio disponível** de cada
treinador. A escolha fica salva no aparelho.

**Why this priority**: sem os estágios a lista mostra times que o jogador não está enfrentando, ou esconde as revanches
melhores que já estão ao alcance.

**Independent Test**: com "antes da Elite Four", o Engineer Bernie (Rota 11) mostra a sequência de estágios até o de
3 Magneton, e a sua posição na lista de Sp. Atk usa esse estágio; o Young Couple Gia & Jes (Rota 12) mostra
Nidoran♂ + Nidoran♀ → Nidorino + Nidorina, sem o estágio de nv 55; com "depois", o estágio Nidoking + Nidoqueen nv 55
aparece marcado como pós-Elite Four.

**Acceptance Scenarios**:

1. **Given** um treinador com vários estágios, **When** ele aparece na lista, **Then** os estágios aparecem em ordem
   (original primeiro), cada um com time, níveis e EVs por batalha.
2. **Given** "antes da Elite Four" selecionado, **When** a lista é exibida, **Then** estágios que exigem a Elite Four
   não aparecem, e treinadores sem nenhum estágio útil disponível para o atributo não aparecem.
3. **Given** "depois da Elite Four" selecionado, **When** a lista é exibida, **Then** todos os estágios aparecem, e os
   que exigem a Elite Four têm a marca "após a Elite Four".
4. **Given** a fase escolhida, **When** a lista é ordenada, **Then** cada treinador é posicionado pelo estágio
   disponível que dá mais EVs do atributo escolhido, e esse estágio aparece em destaque.
5. **Given** a escolha feita, **When** o jogador reabre o app, **Then** a fase escolhida continua selecionada.

---

### User Story 3 - Começar pelo que o Pokémon selecionado precisa (Priority: P2)

Ao abrir a página, o atributo já vem escolhido de acordo com o Pokémon selecionado: o atributo com meta ainda não
atingida em que falta mais EV. Treinadores cujo time daria EVs em atributos já completos são marcados com aviso
("dá Defesa, que já está completa").

**Why this priority**: conecta o guia ao treino atual; útil, mas o guia funciona sem isso.

**Independent Test**: com um Pokémon que só tem meta incompleta em Sp. Atk, abrir a página e ver Sp. Atk selecionado.

**Acceptance Scenarios**:

1. **Given** um Pokémon com metas de HP (faltam 200) e Speed (faltam 50), **When** a página abre, **Then** HP vem
   selecionado.
2. **Given** um treinador que também dá um atributo cuja meta já foi atingida, **When** aparece, **Then** há um aviso
   de que esse EV seria desperdiçado.
3. **Given** nenhum Pokémon ou nenhuma meta incompleta, **When** a página abre, **Then** HP vem selecionado.

---

### Edge Cases

- **Mesmo treinador, vários atributos**: aparece em cada lista de atributo que ele dá, com o atributo da lista em
  destaque.
- **Batalhas duplas** (Twins, Young Couple, Sis & Bro): uma entrada com os dois Pokémon; indicação "batalha dupla"
  (precisa de 2 Pokémon aptos).
- **Treinadores dentro de prédios ou ginásios**: não são reativados pelo VS Seeker e não aparecem.
- **Ilhas Sevii**: fora do escopo desta versão (o jogador ainda não chegou lá); a página informa que não estão
  incluídas.
- **Dados incertos**: quando as fontes divergem (ex.: se a revanche da Lass Julia muda o time), a entrada mostra uma
  nota "a confirmar no jogo".
- **Idioma**: nomes de treinadores e Pokémon como no jogo em inglês; locais e textos no idioma ativo (pt/en).
- **Offline**: a página funciona sem internet, como o resto do app.
- **Multiplicadores**: a página mostra EVs base; uma nota lembra que Macho Brace dobra e Pokérus + Macho Brace
  quadruplicam, e que quem tem o Exp. Share ganha o mesmo (dobrado só pelo Pokérus dele).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O app MUST ter uma página "Onde treinar" acessível pelo menu (e a partir da aba EVs).
- **FR-002**: A página MUST permitir escolher o atributo: HP, Ataque, Sp. Atk ou Speed.
- **FR-003**: Para o atributo escolhido, a página MUST listar os treinadores de revanche (VS Seeker) da região de
  Kanto que dão esse atributo, ordenados pelos EVs desse atributo por batalha (maior primeiro; empate: menor nível
  primeiro).
- **FR-004**: Cada entrada MUST mostrar: classe e nome do treinador, local (rota e referência), time com níveis, EVs por
  batalha de todos os atributos (o atributo escolhido em destaque) e condições/observações (Surf, batalha dupla,
  Cycling Road, dado incerto).
- **FR-005**: Cada treinador MUST ser uma entrada própria; a página MUST NOT somar EVs de treinadores diferentes nem
  sugerir que um único uso do VS Seeker reativa treinadores distantes.
- **FR-006**: Cada treinador MUST mostrar todos os seus estágios de time em ordem (original, 1ª revanche, 2ª
  revanche…), cada um com Pokémon, níveis e EVs por batalha, marcando os estágios que só aparecem depois da Elite Four.
- **FR-006a**: A página MUST permitir escolher a fase "antes da Elite Four" ou "depois da Elite Four"; com "antes", os
  estágios que exigem a Elite Four MUST NOT aparecer; treinadores sem estágio disponível que dê o atributo escolhido
  MUST NOT aparecer.
- **FR-006b**: A ordenação (FR-003) MUST usar, para cada treinador, o estágio disponível na fase escolhida que dá mais
  EVs do atributo; esse estágio MUST aparecer em destaque.
- **FR-007**: A fase escolhida MUST ficar salva no aparelho.
- **FR-008**: Treinadores dentro de prédios/ginásios e das Ilhas Sevii MUST NOT aparecer; a página MUST informar que as
  Ilhas Sevii não estão incluídas.
- **FR-009**: A página MUST mostrar a lembrança dos multiplicadores (Macho Brace, Pokérus, Exp. Share) e do recarregamento
  do VS Seeker (~100 passos, só treinadores na tela).
- **FR-010**: Ao abrir, o atributo MUST vir pré-selecionado pelo Pokémon selecionado (meta incompleta com mais EV
  faltando; sem isso, HP).
- **FR-011**: Entradas que dão EV em atributos com meta já atingida pelo Pokémon selecionado MUST ter um aviso.
- **FR-012**: Os dados MUST estar embutidos no app (funciona offline) e a página MUST citar as fontes usadas.
- **FR-013**: Todos os textos MUST vir dos recursos de idioma (pt e en).

### Key Entities

- **Treinador de revanche**: classe, nome, local (rota + referência textual), condições de acesso, se é batalha dupla.
- **Estágio de time**: posição na sequência do treinador (original, 1ª revanche, 2ª…), lista de Pokémon com nível e se
  exige a Elite Four; os EVs por batalha vêm do rendimento de cada Pokémon.
- **Nota de incerteza**: texto opcional quando as fontes divergem.
- **Fase do jogador**: antes/depois da Elite Four (preferência do aparelho).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: O jogador encontra o melhor treinador para um atributo em no máximo 2 toques a partir de qualquer tela.
- **SC-002**: 100% das entradas mostram local, time com níveis e EVs por batalha.
- **SC-003**: Nenhuma entrada soma EVs de mais de um treinador.
- **SC-004**: Trocar a fase (antes/depois da Elite Four) atualiza a lista na hora, e a escolha persiste em 100% das
  reaberturas.
- **SC-007**: 100% dos treinadores com mais de um estágio mostram todos os estágios disponíveis na fase, em ordem.
- **SC-005**: Os EVs por batalha de cada entrada batem com a soma dos rendimentos dos Pokémon do time (verificável
  automaticamente com os dados de rendimento que o app já tem).
- **SC-006**: A lista inclui pelo menos os treinadores levantados nas pesquisas de HP, Ataque e Sp. Atk, mais uma lista
  equivalente de Speed.

## Assumptions

- O VS Seeker reativa apenas treinadores visíveis na tela; por isso cada treinador é tratado individualmente.
- Cada treinador tem a sequência completa de estágios de revanche; a única condição de progresso modelada é "exige a
  Elite Four". Onde as fontes não deixam claro se um estágio exige a Elite Four, o estágio recebe nota de incerteza.
- Escopo de atributos desta versão: HP, Ataque, Sp. Atk e Speed (os pedidos). Defesa e Sp. Def podem ser acrescentados
  depois com os mesmos dados.
- Fontes: Dragonfly Cave (FR/LG VS Seeker), Serebii (rotas de Kanto, Gen 3), Marriland (EV training FR/LG); a curadoria
  é feita uma vez e embutida no app.
- Registrar batalhas continua sendo feito na aba EVs; a página é um guia de consulta.
