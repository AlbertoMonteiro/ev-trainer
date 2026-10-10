# Research: Edição manual de EVs, IVs e anotações

**Feature**: [spec.md](./spec.md) · **Plan**: [plan.md](./plan.md) · **Date**: 2026-10-10

## R1. Como representar o ajuste manual de EVs

- **Decision**: um **evento de ajuste** no próprio histórico, que **fixa** os totais nos valores
  informados naquele ponto (`BattleEvent.SetTotals = int[6]`). Ao recalcular (`Evs.Totals`), quando o
  laço encontra um ajuste, os totais passam a ser exatamente esses valores e a soma é recomputada;
  eventos seguintes somam normalmente (limites 255/510, vitaminas até 100, multiplicador).
- **Rationale**: mantém o Princípio IV ("totais sempre derivados reprocessando o histórico em ordem")
  — o ajuste é só mais um item da sequência —, preserva o histórico (contagens `×N`, Recentes) e
  expressa a intenção do usuário ("o jogo diz X agora"). Pode ser removido como qualquer item.
- **Alternatives considered**: evento de **diferença** (+2/-3): depende de tudo que vem antes; remover
  uma batalha antiga depois mudaria o resultado de forma inesperada. **Sobrescrever** um campo "EVs
  base" fora do histórico: quebra o recálculo em ordem e não mostra quando o ajuste foi feito.

## R2. Apagar o histórico com decisão do usuário

- **Decision**: no salvar, se o usuário escolher "Apagar histórico", o formulário troca para uma etapa
  de confirmação (`role="alertdialog"`) com: quantas batalhas serão apagadas, que contagens `×N` e
  Recentes zeram, sugestão de exportar backup (botão) e os botões "Apagar N batalhas" (vermelho) e
  "Cancelar". Confirmado, `TrainerStore.ResetHistory(targets)` substitui `Events` por um único ajuste
  com os valores; o aviso oferece Desfazer (restaura a lista anterior inteira).
- **Rationale**: FR-002/FR-004, SC-002 — nunca apaga sem um segundo toque consciente; "Manter
  histórico" é o padrão. Confirmação inline (sem `window.confirm`) mantém textos localizáveis, funciona
  igual no PWA e dispensa JS.
- **Alternatives considered**: `window.confirm` (texto não localizável pelo app, visual do sistema);
  apagar sem perguntar (viola o pedido).

## R3. IVs conhecidos

- **Decision**: `TrainingPokemon.KnownIvs` (`int?[6]`, JSON `knownIvs`, `null` = não sabe). A linha
  Final ganha um novo caso na hierarquia fechada: `FinalKnown(int Value, bool MatchesNotes)`;
  `MatchesNotes` é falso quando há faixa calculada e o valor está fora dela (ou a faixa é "conferir").
- **Rationale**: FR-007 sem tocar nas anotações; adicionar um caso à hierarquia `closed` faz o
  compilador apontar todo `switch` que precisa tratá-lo (C# 15).
- **Alternatives considered**: transformar o IV informado em uma "anotação" fictícia (confunde a tabela
  e não representa "valor exato").

## R4. Editar anotação de nível

- **Decision**: botão "Editar" em cada linha da tabela de IVs abre o mesmo formulário de anotação em
  modo edição (preenchido com a anotação, não com os EVs atuais); salvar chama
  `TrainerStore.ReplaceLevel(index, entry)`, que reordena por nível e devolve Desfazer.
- **Rationale**: FR-008 reaproveitando validação e layout existentes.

## R5. Compatibilidade do backup

- **Decision**: campos novos opcionais: `events[].set` (int[6]) e `knownIvs` (array de 6 com `null`).
  Ausentes ⇒ nada informado. Escrita sempre inclui `knownIvs` (com `null`s) e `set` só nos ajustes.
- **Rationale**: FR-009, SC-004; `user-data-export.json` e backups atuais continuam válidos.
- **Nota**: o app antigo (`app-atual.html`) ignoraria os ajustes (nome vazio) — aceitável, ele foi
  substituído.
