# Feature Specification: Botão Editar EVs no cabeçalho do resumo

**Feature Branch**: `010-summary-edit-button`

**Created**: 2026-10-12

**Status**: Draft

**Input**: User description: "Na tela de evs, tem a descrição do pokemon assim: 414 / 510 · metas 508.
Essa parte que fala metas 508, que foi o que defini, ao meu ver não faz sentido mostrar. 414/510 já é
suficiente. Sendo assim, o botão editar evs, pode ficar nesse espaço"

**Contexto**: na aba EVs, o cartão de resumo do Pokémon selecionado tem um cabeçalho com o sprite à
esquerda e, à direita, o texto **"414 / 510 · metas 508"** (EVs atuais somados / limite do jogo · soma
das metas). Logo abaixo vem a grade dos seis atributos, cada um com "atual/meta" e uma barra. O botão
**"Editar EVs"** fica sozinho numa linha no fim do cartão. A soma das metas é um número que o próprio
jogador definiu e que já aparece, atributo por atributo, na grade; repeti-la no cabeçalho só ocupa
espaço. O botão, por sua vez, gasta uma linha inteira do cartão — numa tela de celular, espaço vertical
que empurra a lista de adversários para baixo.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cabeçalho mais limpo, com o botão no lugar das metas (Priority: P1)

O jogador abre a aba EVs. O cabeçalho do resumo mostra o sprite, o total de EVs no formato
**"414 / 510"** (sem "· metas 508") e, no mesmo cabeçalho, o botão **"Editar EVs"**. Não existe mais
uma linha extra só para o botão no fim do cartão, então o cartão fica mais baixo e a lista de
adversários começa mais acima.

**Why this priority**: é todo o pedido; remove informação redundante e recupera espaço vertical.

**Independent Test**: selecionar um Pokémon com EVs e metas, abrir a aba EVs e conferir que o
cabeçalho mostra só "atual / 510" e o botão "Editar EVs", e que não há botão abaixo da grade.

**Acceptance Scenarios**:

1. **Given** um Pokémon com 414 EVs somados e metas somando 508, **When** o jogador abre a aba EVs,
   **Then** o cabeçalho mostra "414 / 510" e nenhum texto com "metas" ou "508".
2. **Given** a aba EVs aberta, **When** o jogador olha o cabeçalho do resumo, **Then** o botão
   "Editar EVs" está no cabeçalho, ao lado do total, e não existe outro botão "Editar EVs" abaixo da
   grade de atributos.
3. **Given** o cabeçalho com o botão, **When** o jogador toca em "Editar EVs", **Then** o editor de
   EVs abre exatamente como antes (mesmos campos, mesma validação, mesmas opções de histórico).
4. **Given** o editor de EVs aberto, **When** o jogador olha o cabeçalho, **Then** o botão "Editar
   EVs" não aparece (o editor já está aberto); **When** o editor é fechado (salvar ou cancelar),
   **Then** o botão volta ao cabeçalho.

---

### Edge Cases

- **Tela estreita (celular ~320px)**: sprite, total e botão cabem na mesma linha do cabeçalho sem
  sobrepor e sem rolagem horizontal; o botão continua com área de toque confortável.
- **Pokémon sem metas** (todas zero): o cabeçalho mostra só o total e o botão, como em qualquer outro
  caso — nada muda por não haver metas.
- **Total acima de 510** (dados importados inconsistentes): o total aparece como está (ex.: "512 /
  510"); esta feature não muda essa regra.
- **Layout de desktop** (painel lateral do Pokémon, feature 007): o cabeçalho segue a mesma regra —
  total sem metas e o botão no cabeçalho.
- **Leitores de tela**: o botão continua sendo um botão com o rótulo "Editar EVs"; o total continua
  legível como texto.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O cabeçalho do resumo de EVs MUST mostrar o total de EVs do Pokémon selecionado no
  formato "atual / 510", sem a soma das metas.
- **FR-002**: O texto "metas {soma}" MUST deixar de aparecer em qualquer lugar do cabeçalho do resumo.
- **FR-003**: O botão "Editar EVs" MUST ficar no cabeçalho do resumo, junto ao total, no espaço antes
  ocupado pela soma das metas.
- **FR-004**: O cartão de resumo MUST NOT ter mais o botão "Editar EVs" numa linha própria abaixo da
  grade de atributos.
- **FR-005**: Tocar no botão do cabeçalho MUST abrir o mesmo editor de EVs de hoje, no mesmo lugar do
  cartão, sem mudança de comportamento.
- **FR-006**: Enquanto o editor de EVs estiver aberto, o botão "Editar EVs" do cabeçalho MUST ficar
  oculto; ao fechar o editor, MUST voltar a aparecer.
- **FR-007**: A grade de atributos MUST continuar mostrando "atual/meta" por atributo, com as barras,
  sem mudança.
- **FR-008**: O cabeçalho MUST caber numa linha em telas de celular de 320px de largura, sem
  sobreposição nem rolagem horizontal.
- **FR-009**: Os textos (total e rótulo do botão) MUST continuar vindo dos textos localizados do app,
  prontos para tradução.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Em 100% dos Pokémon selecionados, o cabeçalho do resumo mostra o total sem nenhuma
  menção às metas.
- **SC-002**: A altura do cartão de resumo (com o editor fechado) diminui em pelo menos a altura de uma
  linha de botão em relação à versão atual, na mesma tela.
- **SC-003**: O jogador abre o editor de EVs com um único toque, a partir do cabeçalho, sem rolar a
  tela.
- **SC-004**: Em tela de 320px de largura, o cabeçalho não apresenta sobreposição nem rolagem
  horizontal.

## Assumptions

- A soma das metas é redundante porque cada meta já aparece por atributo na grade; não é necessário
  movê-la para outro lugar.
- O editor continua abrindo dentro do cartão de resumo, abaixo da grade, como hoje; só o botão muda de
  lugar.
- Esconder o botão enquanto o editor está aberto segue o comportamento atual (hoje o botão também some
  quando o editor abre).
- O botão do cabeçalho pode ter um visual mais compacto que o atual, desde que mantenha área de toque
  adequada para celular.
- Nenhum dado salvo, formato de backup ou sincronização muda; é uma mudança só de apresentação.
