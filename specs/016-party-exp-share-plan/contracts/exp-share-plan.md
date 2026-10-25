# Contract: plano de batalhas com Exp. Share

## Domínio

```text
ExpSharePlanner.Plan(PlanMon a, PlanMon? b, int[] yieldPerBattle) → SharePlan
```

As configurações, os critérios de escolha e os limites estão em [research R2](../research.md#r2--algoritmo-do-plano).

## Casos de referência (testes)

Os casos usam Sp. Atk, partem de EVs 0 e não têm Macho Brace marcado. As metas são as do quadro.

- **Fases** dá as batalhas por configuração, na ordem `ALeadsBShares`, `BLeadsAShares`, `AAlone`, `BAlone`.
- **Sobra** é o que fica faltando somado dos dois.
- **Sozinhos** são as batalhas de cada um sozinho com o Macho Brace.

| # | Meta A | Meta B | Rend./batalha | Pokérus A | Pokérus B | Fases (1, 2, 3, 4) | Total | Sobra | Sozinhos |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 120 | 96 | 6 (Bernie, Magneton ×3) | não | não | 8, 4, 0, 0 | 12 | 0 | 10 + 8 = 18 |
| 2 | 252 | 252 | 6 | não | não | 14, 14, 0, 0 | 28 | 0 | 21 + 21 = 42 |
| 3 | 120 | 96 | 6 | sim | não | 0, 8, 1, 0 | 9 | 0 | 5 + 8 = 13 |
| 4 | 120 | 0 | 6 | não | não | 0, 0, 10, 0 | 10 | 0 | 10 + 0 |
| 5 | 100 | 90 | 6 | não | não | 7, 2, 0, 2 | 11 | 4 (A) | 8 + 7 = 15 (sobra 10) |
| 6 | 40 | 40 | 4 (Bernie original) | não | não | 4, 2, 0, 1 | 7 | 0 | 5 + 5 = 10 |

Regras gerais, verificadas com todos os casos acima e com casos aleatórios pequenos:

- o resultado é igual ao da força bruta nas 4 dimensões;
- nenhum atributo passa da capacidade;
- a soma dos ganhos nunca passa de `510 − total`.

Outros casos:

- **B nulo**: só `AAlone`.
- **Ninguém com capacidade nos atributos do adversário**: `Useless = true` e nenhuma fase.
- **Rendimento em mais de um atributo**: o plano respeita a capacidade de cada um. Exemplo: rendimento 2 de Ataque e 1
  de Speed; A precisa de Ataque, mas está com a Speed completa. Então A nunca entra em nenhuma configuração, porque
  ganharia Speed, e o plano usa só `BAlone`.

## UI — `ExpSharePlanView`

### Entradas

- **A**: um `select` com os Pokémon da equipe. Por padrão, o selecionado.
- **B**: um `select` com os Pokémon da equipe, sem A, ou "ninguém". Por padrão, o portador do Exp. Share, se for
  diferente de A.
- **Adversário**: um campo de busca que lista:
  - treinadores pelo nome ou pela classe (`FindTrainers`), sem os de batalha dupla, com o motivo indicado;
  - Pokémon pelo nome.
- **Revanche**: escolhida na lista de revanches do treinador. Por padrão, a primeira visível na fase do guia
  (`Preferences.GuidePhase`).
- **Pré-preenchimento**: ao vir do `TrainerCard`, o treinador e a revanche já chegam escolhidos.

### Saída

Em `.plan-result`:

- **uma linha por fase**, em `.plan-phase` com `data-setup`. Cada uma mostra:
  - "N batalhas";
  - quem vai na frente com o Macho Brace e quem segura o Exp. Share;
  - "+x Sp. Atk por batalha" para cada um;
  - o botão "Usar esta configuração".
- **EVs finais** de cada um nos atributos do adversário, como "Sp. Atk 0 → 120 / meta 120".
- **O que sobra**, quando houver: "faltarão 4 Sp. Atk para Electabuzz: complete com adversários de menor rendimento".
- **O total**: "12 batalhas · sozinhos seriam 18 (6 a menos)".
- **Quando o adversário não serve**, uma frase dizendo que ele não ajuda nenhum dos dois.

### Usar esta configuração

- O botão chama `ApplyConfiguration` e mostra um aviso com Desfazer.
- Se A ou B estiver guardado, o app pergunta "Colocar X na equipe?" antes de aplicar.
- Se a equipe estiver cheia, o app leva à tela da equipe.

### Registrar batalha (só com treinador)

O botão "Registrar 1 batalha" (US5) chama `RecordTeam`. O plano é recalculado logo depois.
