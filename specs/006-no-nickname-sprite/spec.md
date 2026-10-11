# Feature Specification: Pokémon identificado pela espécie, sem apelido

**Feature Branch**: `006-no-nickname-sprite`

**Created**: 2026-10-11

**Status**: Draft

**Input**: User description: "Atualmente o pokemon tem um apelido, mas quando evolui, tenho que trocar seu
apelido, então quero simplificar isso e não ter mais apelido, além disso quero também ter um indicativo
visual do pokemon quando eu estiver na tela de evs, pois hoje atualmente tem só seu nome. Como vou ter a
imagem do pokemon, então não preciso do nome."

**Contexto**: hoje cada Pokémon em treino tem um campo "Nome" (apelido) obrigatório nos Ajustes. Ao
escolher a espécie, o nome é preenchido com ela; quando o Pokémon evolui, o jogador precisa trocar o nome à
mão para refletir a nova forma. A tela de EVs mostra só esse nome no título, sem imagem. Esta feature
remove o apelido: o Pokémon passa a ser identificado pela sua **forma atual** (a espécie em que ele está
agora, dentro da linha evolutiva), e a tela de EVs mostra a imagem dessa forma no lugar do nome.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Evoluir sem renomear (Priority: P1)

O jogador está treinando um Ralts. Ele evolui no jogo para Kirlia. No app, ele marca a evolução (nos
Ajustes, tocando na evolução, ou anotando um nível já como Kirlia) e pronto: em todo o app o Pokémon
passa a aparecer como Kirlia, com a imagem de Kirlia. Não existe mais campo de nome para editar.

**Why this priority**: é a dor principal relatada — cada evolução exige editar o nome à mão.

**Independent Test**: criar um Pokémon Ralts, tocar em "Evolui para Kirlia" nos Ajustes e salvar ⇒ menu,
tela de EVs e aba IVs mostram Kirlia; nenhum campo de nome existe nos Ajustes.

**Acceptance Scenarios**:

1. **Given** os Ajustes de um Pokémon, **Then** não há campo "Nome"; a espécie (forma atual) é escolhida
   na lista de espécies, e o formulário pode ser salvo sem digitar nenhum nome.
2. **Given** um Pokémon novo, **When** o jogador escolhe uma espécie (ex.: Kirlia), **Then** a forma
   atual é essa espécie, mesmo que a linha evolutiva inclua formas anteriores e posteriores.
3. **Given** um Pokémon na forma Ralts, **When** o jogador toca na evolução Kirlia e salva, **Then** a
   forma atual passa a ser Kirlia e todo o app mostra Kirlia.
4. **Given** um Pokémon na forma Ralts, **When** o jogador anota um nível com a espécie Kirlia, **Then** a
   forma atual passa a ser Kirlia automaticamente, sem passar pelos Ajustes.
5. **Given** a forma atual Kirlia, **When** o jogador anota um nível como Ralts (forma anterior, ex.:
   anotação atrasada), **Then** a forma atual continua Kirlia (anotações nunca "desevoluem" o Pokémon).
6. **Given** a linha evolutiva nos Ajustes, **When** o jogador escolhe outra forma da linha como atual
   (ex.: corrigir um toque errado), **Then** essa passa a ser a forma atual.

---

### User Story 2 - Ver a imagem do Pokémon na tela de EVs (Priority: P1)

Na tela de EVs, no lugar do título com o nome, o jogador vê a imagem da forma atual do Pokémon ao lado dos
totais de EV, e reconhece de relance qual Pokémon está treinando.

**Why this priority**: é a segunda metade do pedido e o principal ganho visual; sem ela, remover o nome
deixaria a tela de EVs sem identificação.

**Independent Test**: com um Kirlia selecionado, abrir a aba EVs ⇒ o resumo mostra a imagem de Kirlia e
não mostra o texto do nome; um leitor de tela anuncia "Kirlia".

**Acceptance Scenarios**:

1. **Given** um Pokémon selecionado, **When** o jogador abre a aba EVs, **Then** o resumo de EVs mostra a
   imagem da forma atual no lugar do título com o nome, e os totais continuam visíveis.
2. **Given** a imagem no resumo, **Then** ela tem um rótulo acessível com o nome da espécie (para leitor
   de tela), embora o nome não apareça como texto visível.
3. **Given** uma espécie sem imagem disponível (ou falha ao carregar), **Then** aparece o mesmo selo
   colorido de reserva usado no resto do app, sem quebrar o layout.
4. **Given** a evolução registrada (Story 1), **When** o jogador volta à aba EVs, **Then** a imagem já é
   da nova forma.
5. **Given** a tela de 360 px de largura, **Then** a imagem e os totais cabem sem rolagem horizontal.

---

### User Story 3 - Dados antigos continuam funcionando (Priority: P2)

O jogador já tem Pokémon salvos (e backups) com apelidos. Ao abrir a nova versão, cada Pokémon aparece
pela sua forma atual, deduzida dos dados existentes, sem perder nada do treino.

**Why this priority**: o app já está em uso; perder ou bagunçar dados existentes seria pior que o
problema original.

**Independent Test**: carregar dados antigos com um Pokémon de nome "Kirlia" e linha Ralts→Kirlia→
Gardevoir ⇒ forma atual Kirlia; batalhas, metas, anotações e IVs inalterados.

**Acceptance Scenarios**:

1. **Given** dados salvos antes desta versão, **When** o app abre, **Then** cada Pokémon recebe uma
   forma atual pela regra de dedução (ver Assumptions) e todo o resto (batalhas, metas, nature, anotações,
   IVs informados, Pokérus/Macho Brace) permanece igual.
2. **Given** um backup antigo com apelidos, **When** importado, **Then** é aceito sem erro e cada Pokémon
   recebe a forma atual pela mesma regra.
3. **Given** um backup exportado pela nova versão, **When** importado de volta, **Then** a forma atual e
   todos os dados voltam idênticos.

---

### Edge Cases

- **Dois Pokémon da mesma espécie** (ex.: dois Ralts): no menu, além da imagem e da espécie, cada item
  mostra a nature e o total de EVs, o que basta para diferenciá-los; a ordem do menu não muda.
- **Pokémon sem espécie definida** (dados antigos ou incompletos): mostra o selo de reserva com a inicial
  "?" e o texto "Pokémon sem espécie" onde antes ia o nome; os Ajustes pedem para escolher uma espécie.
- **Apelido antigo que não é nome de espécie** (ex.: "Gardi"): é descartado; a forma atual é deduzida da
  linha e das anotações.
- **Troca de linha evolutiva** nos Ajustes (escolher outra espécie fora da linha): a forma atual passa a
  ser a espécie escolhida.
- **Anotação de nível de espécie fora da linha**: não altera a forma atual (já não é permitido criar).
- **Evolução ramificada** (ex.: Kirlia → Gardevoir, ou outra forma em gerações futuras): a forma atual é
  sempre a que o jogador escolheu ou anotou, nunca escolhida automaticamente entre ramos.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O app NÃO DEVE ter mais campo de nome/apelido para o Pokémon; criar e salvar os Ajustes
  NÃO DEVE exigir digitar nome.
- **FR-002**: Cada Pokémon DEVE ter uma **forma atual**, que é uma das espécies da sua linha evolutiva.
- **FR-003**: Ao escolher uma espécie nos Ajustes, a forma atual DEVE ser a espécie escolhida; ao tocar
  numa evolução, a forma atual DEVE passar a ser essa evolução.
- **FR-004**: Os Ajustes DEVEM permitir escolher qualquer forma da linha evolutiva como atual.
- **FR-005**: Anotar um nível com uma espécie posterior à forma atual na linha DEVE atualizar a forma
  atual para essa espécie; anotar com uma forma anterior ou igual NÃO DEVE alterá-la.
- **FR-006**: Todo lugar que hoje mostra o nome do Pokémon (menu, aba IVs, exclusão, avisos) DEVE mostrar
  o nome da espécie da forma atual.
- **FR-007**: O resumo da tela de EVs DEVE mostrar a imagem da forma atual no lugar do título com o nome,
  sem o nome como texto visível, mas com o nome da espécie como rótulo acessível.
- **FR-008**: Quando não houver imagem para a forma atual, o resumo DEVE mostrar o selo de reserva já usado
  no app.
- **FR-009**: Dados salvos e backups anteriores DEVEM continuar sendo aceitos; a forma atual DEVE ser
  deduzida deles sem perder nenhum outro dado.
- **FR-010**: A forma atual DEVE ser salva no aparelho e incluída no backup exportado.
- **FR-011**: Todo texto novo (ex.: "Forma atual", "Pokémon sem espécie") DEVE vir dos recursos de idioma.

### Key Entities

- **Pokémon em treino**: deixa de ter apelido; ganha a **forma atual** (uma espécie da linha evolutiva),
  que define a imagem e o nome exibidos.
- **Linha evolutiva**: lista ordenada de 1 a 3 espécies (inalterada); a forma atual é uma delas.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Registrar uma evolução exige no máximo 2 toques e 0 caracteres digitados (antes: abrir os
  Ajustes, apagar e redigitar o nome).
- **SC-002**: Em 100% dos casos, depois de registrar a evolução, menu, tela de EVs e aba IVs mostram a
  nova forma sem nenhuma outra ação.
- **SC-003**: O jogador identifica o Pokémon ativo na tela de EVs pela imagem em menos de 1 segundo, sem
  ler texto.
- **SC-004**: 100% dos dados e backups antigos abrem na nova versão com batalhas, metas, anotações e IVs
  idênticos aos de antes.

## Assumptions

- **Regra de dedução da forma atual para dados antigos**, na ordem: (1) se o apelido for o nome de uma
  espécie da linha, essa espécie; (2) senão, a espécie mais avançada na linha entre as anotações de nível;
  (3) senão, a primeira espécie da linha. O apelido é então descartado.
- O backup exportado continua no formato do HANDOFF §3.1; o campo de nome passa a ser preenchido com o
  nome da forma atual, para que versões anteriores do app ainda consigam ler o arquivo.
- Na tela de EVs só o resumo troca nome por imagem; os cards de oponentes já mostram imagens e não mudam.
- No menu e na aba IVs, o nome da espécie continua visível em texto (ajuda a diferenciar formas parecidas e
  dois Pokémon da mesma espécie); a remoção do texto vale só para o resumo da tela de EVs, como pedido.
- As imagens são as mesmas já usadas pelo app (sprites da Gen 3 com selo de reserva); nada novo é baixado.
