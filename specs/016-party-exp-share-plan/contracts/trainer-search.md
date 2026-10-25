# Contract: busca por treinador

## Domínio

```text
OpponentSearch.FindTrainers(string query) → IReadOnlyList<GuideTrainer>
OpponentSearch.Find(string query, GameVersion version) → IReadOnlyList<SearchHit>   // + espécies dos times
```

| Busca | Treinadores | Grade (além do que já casava) |
|---|---|---|
| `bernie` | Engineer Bernie (Rota 11) | Magnemite e Magneton, com "Rota 11" |
| `Engineer Bernie` | Engineer Bernie | idem |
| `engineer` | todos os Engineer do guia | as espécies dos times deles |
| `rota 11` | nenhum (nome e classe não incluem o local) | como hoje |
| `brock` | nenhum (sem revanche, fora do guia) | como hoje |
| `` (vazio) | nenhum | nenhum |

Regras de texto:

- A busca ignora maiúsculas e acentos (`TextFold`).
- O texto casa em qualquer posição.
- Uma busca que termina em dígito não casa no meio de um número.
- A ordem dos cartões é a do guia.

## UI — `TrainerCard` (aba de EVs, durante a busca)

- **Posição**: os cartões ficam acima das grades, em `.trainer-results`, e só aparecem quando há busca.
- **Conteúdo de cada cartão** (`.trainer-card[data-trainer=ID]`):
  - classe e nome, e o local;
  - uma linha por revanche (`.trainer-stage[data-stage=N]`), com:
    - o time, como "Magneton ×3 · Nv. 28";
    - os EVs por batalha, como "+6 Sp. Atk";
    - a marca "após a Elite Four" quando a revanche exige a Elite Four;
    - o botão "Registrar batalha inteira";
    - o botão "Planejar com Exp. Share", que não aparece em batalhas duplas.
  - em batalhas duplas, a marca "batalha dupla", com o motivo de não haver plano.
- **"Registrar batalha inteira"**:
  - chama `RecordTeam`;
  - o aviso diz "Bernie: +6 Sp. Atk · Jolteon +6 Sp. Atk" (ganhos somados) e tem Desfazer, que chama `UndoTeam`.
- **"Planejar com Exp. Share"**: abre o `ExpSharePlanView` com o treinador e a revanche escolhidos.
