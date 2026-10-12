# Research: Busca com úteis primeiro

Não havia itens NEEDS CLARIFICATION no contexto técnico; abaixo, as decisões de desenho.

## R1 — Onde fica a regra de ordenação

- **Decision**: função pura `Evs.UsefulFirst(hits, totals, targets)` em `Domain/Evs.cs`, devolvendo
  `(Useful, Others)` por partição estável sobre `Evs.IsUseful`.
- **Rationale**: Princípio II (domínio em C# puro, testável sem UI); reaproveita exatamente a regra do
  filtro (FR-002) e preserva a ordem de entrada em cada grupo (FR-004).
- **Alternatives considered**: ordenar com `OrderBy` dentro da `EvTab` (mistura regra com tela, mais
  difícil de testar); mudar `OpponentSearch.Find` para ordenar (a busca não conhece o Pokémon
  selecionado e é usada sem contexto de treino).

## R2 — Como mostrar os dois grupos

- **Decision**: a região `.main-list` passa a conter uma ou duas `OpponentGrid`, cada uma com seu
  `h3.section-title`. Grupo vazio não é renderizado; se nenhum adversário é útil (ou todos são), uma
  única grade com o título de hoje ("Resultados" na busca, "Todos" sem busca) ou "Úteis para as metas".
- **Rationale**: reusa o componente e o CSS da grade (2 colunas no celular, ≥ 4 no desktop); títulos
  são o mesmo padrão de "Recentes"; nenhuma mudança visual nos cartões (Assumptions da spec).
- **Alternatives considered**: lista única com cartões não úteis esmaecidos (sem título, menos claro e
  pede um estilo novo de cartão); separador sem título (não acessível a leitor de tela).

## R3 — Títulos

- **Decision**: "Úteis para as metas" (`Evs.List.Useful`, já existe); "Outros resultados"
  (`Evs.List.OtherResults`, novo) na busca; "Outros adversários" (`Evs.List.Others`, novo) em "Todos"
  sem busca, onde "resultados" não faz sentido.
- **Rationale**: FR-003/FR-009 com texto natural em cada contexto.

## R4 — Controle Úteis/Todos durante a busca (Q1 = A)

- **Decision**: `OpponentSearchBar` recebe `Searching`; quando verdadeiro, os dois botões ficam
  `disabled` com `aria-pressed="false"` e a preferência salva não é alterada. CSS de
  `.segmented button:disabled` com opacidade reduzida e cursor padrão.
- **Rationale**: o controle não "mente" (SC-003) e continua no lugar, sem a caixa de busca mudar de
  largura enquanto o jogador digita; a escolha volta ao apagar a busca (FR-008) porque nunca foi
  alterada.
- **Alternatives considered**: esconder o controle (a caixa de busca pula de largura a cada primeira/
  última letra); manter o filtro aplicado à busca (opção B, recusada pelo usuário).

## R5 — Recalcular após batalha

- **Decision**: nenhum mecanismo novo: a `EvTab` já re-renderiza a cada `Store.Changed`, e a partição é
  calculada no render a partir de `Store.SelectedTotals` e das metas.
- **Rationale**: FR-006 sai de graça; testado com bUnit registrando uma batalha que completa a meta.

## R6 — Testes existentes afetados

- **Decision**: os testes que afirmam "Todos" com título único e a lista inteira em ordem da Pokédex
  passam a afirmar a nova regra (úteis primeiro, cada grupo em ordem da Pokédex, vitaminas no fim do
  seu grupo); o teste de busca que verifica o título "Resultados" passa a usar um Pokémon cujos
  resultados não sejam úteis, ou verifica os dois títulos.
- **Rationale**: a spec muda o comportamento de propósito (US3); o teste antigo deixa de ser verdade,
  não é uma regressão.
