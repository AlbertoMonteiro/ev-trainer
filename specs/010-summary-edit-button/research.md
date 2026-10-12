# Research: Botão Editar EVs no cabeçalho do resumo

Não havia itens NEEDS CLARIFICATION no contexto técnico; abaixo, as decisões de desenho.

## R1 — Texto do total

- **Decision**: mudar só o valor do recurso `Evs.Summary.Totals` para `"{0} / 510"` e passar apenas o
  total atual na chamada.
- **Rationale**: FR-001/FR-002; a chave continua a mesma, então traduções futuras e testes que usam
  `T("Evs.Summary.Totals", …)` seguem a mesma fonte.
- **Alternatives considered**: chave nova (`Evs.Summary.Total`) — troca de nome sem ganho; montar o
  texto no componente — viola o Princípio VII.

## R2 — Onde fica o botão

- **Decision**: `button.edit-evs` vira o último filho do `.card-head` (depois de `span.totals`), só
  renderizado quando o editor está fechado. O total ganha `margin-left: auto` para o par total + botão
  ficar à direita, com o sprite à esquerda.
- **Rationale**: FR-003/FR-004; mantém a classe `edit-evs`, então os testes de editor e de texto fixo
  não precisam mudar de seletor; ordem de leitura natural (sprite, total, ação).
- **Alternatives considered**: botão ícone (lápis) sem texto — menos claro e exigiria `aria-label` e um
  ícone novo; envolver total e botão num `div` — mais marcação sem necessidade, o flex do cabeçalho já
  resolve.

## R3 — Comportamento com o editor aberto

- **Decision**: como hoje, o botão não é renderizado enquanto `_editing` é verdadeiro; o `EvEditor`
  continua abaixo da grade; ao fechar, o botão volta ao cabeçalho.
- **Rationale**: FR-005/FR-006; nenhum fluxo do editor muda.
- **Alternatives considered**: botão vira "Fechar" no cabeçalho — duplica o cancelar do editor e muda
  o comportamento, fora do escopo.

## R4 — Estilo e largura mínima

- **Decision**: `.summary .edit-evs` perde `width: 100%` e `margin-top`; mantém `min-height: 44px`,
  usa `padding: 0 12px` e `white-space: nowrap`; `.summary .totals` com `white-space: nowrap`. O
  `.card-head` do resumo já usa `align-items: center`.
- **Rationale**: a 320 px o espaço útil do cartão é ~260 px (320 − margens − padding de 14 px);
  sprite 64 + total (~75 px em fonte mono 13 px) + botão "Editar EVs" (~100 px) + 2 gaps de 8 px ≈
  255 px — cabe em uma linha (FR-008, SC-004); 44 px de altura mantém a área de toque.
- **Alternatives considered**: diminuir o sprite — muda o título do cartão, fora do escopo; permitir
  quebra de linha — violaria FR-008.
