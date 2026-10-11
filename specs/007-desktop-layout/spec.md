# Feature Specification: Layout para desktop

**Feature Branch**: `007-desktop-layout`

**Created**: 2026-10-11

**Status**: Draft

**Input**: User description: "A aplicação deve ter um bom visual quando executando no desktop, hoje está
focada com layout só para celular"

**Contexto**: o app foi desenhado para o celular, com uma coluna única de no máximo 560 px (constituição,
Princípio VI). No desktop (ex.: 1366 × 768), essa coluna fica centralizada com mais da metade da tela
vazia dos lados. Os cards de adversários aparecem em 2 colunas, e a tabela de IVs, os formulários e a
avaliação de captura ficam empilhados, exigindo rolagem mesmo com espaço sobrando. Quem joga no
emulador do PC usa o app numa janela ao lado do jogo ou em tela cheia, e quer aproveitar a largura.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Treinar EVs no desktop aproveitando a largura (Priority: P1)

O jogador abre o app no navegador do PC. A aba EVs usa a largura da tela: o resumo dos EVs fica
visível ao lado da lista de adversários, e os cards de adversários aparecem em mais colunas, de modo
que ele vê mais opções sem rolar e continua registrando batalhas com um clique.

**Why this priority**: registrar batalhas é a ação principal do app; é onde o espaço desperdiçado mais
atrapalha no desktop.

**Independent Test**: em 1366 × 768, aba EVs com um Pokémon que tem metas ⇒ o resumo de EVs fica
visível enquanto a lista de adversários rola, há pelo menos 4 cards de adversário por linha, e
registrar uma batalha continua sendo um clique no card.

**Acceptance Scenarios**:

1. **Given** uma janela de 1024 px de largura ou mais, **When** o jogador abre a aba EVs, **Then** o
   conteúdo usa a largura disponível (com uma largura máxima confortável para leitura em telas muito
   largas), em vez de uma coluna estreita centralizada.
2. **Given** a aba EVs no desktop, **When** o jogador rola a lista de adversários, **Then** o resumo
   dos EVs (totais e metas por stat) continua visível.
3. **Given** a aba EVs no desktop, **Then** os cards de adversário aparecem em pelo menos 4 colunas em
   1366 px e se ajustam à largura da janela.
4. **Given** o desktop, **When** o jogador clica num card, **Then** a batalha é registrada com um
   clique, como no celular, e o aviso com Desfazer aparece num lugar visível sem cobrir o card.

---

### User Story 2 - IVs, ajustes e avaliação de captura lado a lado (Priority: P2)

Nas telas de formulário — aba IVs, Ajustes e Avaliar captura —, o desktop mostra os blocos lado a
lado em vez de empilhados: a tabela de IVs ao lado do formulário de anotação; na avaliação de captura,
o formulário ao lado do resultado e do "melhor até agora"; nos Ajustes, as seções em duas colunas.

**Why this priority**: reduz rolagem e permite ver o efeito do que se digita (resultado ao lado do
formulário), mas a ação principal (P1) já fica boa sem isso.

**Independent Test**: em 1366 × 768, Avaliar captura com o caso Squirtle Nv 5 Timid ⇒ os campos e o
resultado (tabela, veredito, melhor até agora) visíveis ao mesmo tempo sem rolar; aba IVs ⇒ tabela e
formulário de anotação lado a lado.

**Acceptance Scenarios**:

1. **Given** a aba IVs no desktop, **Then** a tabela de IVs e o formulário de anotação ficam lado a
   lado, e os IVs conhecidos ficam junto da tabela.
2. **Given** Avaliar captura no desktop, **When** o jogador preenche os campos, **Then** o resultado
   aparece ao lado dos campos, sem precisar rolar para vê-lo.
3. **Given** os Ajustes no desktop, **Then** as seções (dados do Pokémon, backup, importação de
   planilha, apagar) se organizam em duas colunas.

---

### User Story 3 - Lista de Pokémon sempre à mão no desktop (Priority: P3)

A partir de 1280 px, a lista dos Pokémon em treino vira um **painel lateral sempre visível**, com o
sprite e o total de EVs de cada um: trocar de Pokémon passa a ser um clique, sem abrir o menu. O menu ☰
continua existindo para tema, idioma, backup e avaliação de captura. (Clarificação 2026-10-11: Q1 = A.)

**Why this priority**: útil para quem treina vários Pokémon ao mesmo tempo; o menu atual já funciona.

**Independent Test**: com 3 Pokémon cadastrados, em 1366 × 768 ⇒ trocar do primeiro para o terceiro
leva 1 clique no painel lateral.

**Acceptance Scenarios**:

1. **Given** vários Pokémon e uma janela de 1280 px ou mais, **Then** a lista com sprite e total de EVs
   de cada um fica visível na lateral, com o selecionado destacado.
2. **Given** o painel lateral, **When** o jogador clica em outro Pokémon, **Then** ele é selecionado
   na hora, mantendo a aba atual.

---

### Edge Cases

- **Janela redimensionada** (ex.: jogo ocupando metade da tela): o layout passa do formato de desktop
  para o de celular conforme a largura, sem perder o que estava digitado nem a aba atual.
- **Tela muito larga** (2560 px ou mais): o conteúdo não se estica indefinidamente; há uma largura
  máxima a partir da qual ele fica centralizado.
- **Tablet em pé** (768 px): usa uma versão intermediária ou a de celular, sem quebrar.
- **Celular**: nada muda — o layout de coluna única, os alvos de toque e o comportamento atual são
  preservados (todas as validações atuais em 360 px continuam valendo).
- **Teclado e mouse**: no desktop, os cards, botões e campos têm estado de foco visível e de hover, e
  dá para navegar com Tab.
- **Zoom do navegador** em 150% ou 200%: o layout se adapta como se a janela fosse mais estreita, sem
  sobrepor elementos.
- **Menu aberto e janela alargada**: o menu continua funcionando; a partir de 1280 px a lista de
  Pokémon do menu é dispensável, pois o painel lateral já a mostra.
- **Janela entre 1024 e 1279 px**: layout de desktop sem painel lateral (troca pelo menu ☰).
- **Nenhum Pokémon cadastrado**: o painel lateral mostra só a ação de adicionar.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Em janelas com largura a partir de 1024 px, o app DEVE usar a largura disponível até um
  máximo confortável (em torno de 1280–1440 px de conteúdo), centralizado acima disso.
- **FR-002**: Abaixo de 1024 px, o app DEVE se comportar exatamente como hoje (coluna única, alvos de
  toque de 44 px, sem rolagem horizontal em 360 px).
- **FR-003**: Na aba EVs, no desktop, o resumo dos EVs DEVE ficar visível enquanto a lista de
  adversários rola, e a grade de adversários DEVE ter pelo menos 4 colunas em 1366 px.
- **FR-004**: Na aba IVs, no desktop, a tabela de IVs e o formulário de anotação DEVEM ficar lado a
  lado.
- **FR-005**: Em Avaliar captura, no desktop, os campos e o resultado (tabela, veredito, melhor até
  agora) DEVEM ficar lado a lado.
- **FR-006**: Nos Ajustes, no desktop, as seções DEVEM se organizar em duas colunas.
- **FR-007**: O aviso com Desfazer DEVE aparecer num canto da tela no desktop, sem cobrir a área onde
  o jogador está clicando.
- **FR-008**: Elementos clicáveis DEVEM ter estados de hover e de foco de teclado visíveis no desktop.
- **FR-009**: Mudar a largura da janela NÃO DEVE perder dados digitados, a aba atual nem a seleção.
- **FR-010**: Os temas claro e escuro DEVEM funcionar igualmente no layout de desktop.
- **FR-011**: A partir de 1280 px, o app DEVE mostrar um painel lateral com todos os Pokémon em
  treino (sprite, forma atual e total de EVs), o selecionado destacado; um clique seleciona outro
  Pokémon mantendo a aba atual; o painel tem a ação de adicionar Pokémon.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Em 1366 × 768, pelo menos 70% da largura da janela é usada pelo conteúdo (hoje, cerca de
  40%).
- **SC-002**: Em 1366 × 768, a aba EVs mostra pelo menos o dobro de cards de adversário sem rolar,
  comparada ao layout atual.
- **SC-003**: Em 1366 × 768, a avaliação de captura completa (campos e resultado) cabe sem rolagem
  vertical.
- **SC-004**: Em 360 px de largura, todas as telas ficam idênticas ao comportamento atual (sem
  regressão no celular).
- **SC-005**: Em nenhuma largura entre 360 px e 2560 px há rolagem horizontal da página ou elementos
  sobrepostos.

## Assumptions

- "Desktop" = janela com 1024 px ou mais de largura, com mouse e teclado; tablets e janelas estreitas
  seguem o layout de celular (ou um intermediário) — sem detecção de dispositivo, só pela largura.
- A identidade visual (cores, fontes, temas, sprites) é mantida; muda a organização do espaço.
- Nenhuma funcionalidade nova além do layout e do painel lateral de Pokémon (US3).
- As abas EVs / IVs / Ajustes continuam existindo no desktop; cada aba aproveita a largura, em vez de
  mostrar todas as abas ao mesmo tempo.
- **Dependência resolvida**: o Princípio VI da constituição foi emendado na v1.2.0 (2026-10-11) para
  "mobile-first, bom no desktop", mantendo todas as exigências do celular.
