# Data Model: Edição manual

## `BattleEvent` (alterado)

| Campo | JSON | Tipo | Regra |
|---|---|---|---|
| Opponent | `o` | string | `""` para ajuste manual |
| Count | `n` | int | 1 para ajuste manual |
| Multiplier | `m` | int | 1 para ajuste manual |
| **SetTotals** | **`set`** | **int[6]?** | **novo**: presente ⇒ é um ajuste manual; cada valor 0–255, soma ≤ 510 |

Derivado: `IsManualAdjustment => SetTotals is not null`.

Regras afetadas:

- `Evs.Totals`: ao encontrar um ajuste, `ev = copy(SetTotals)`, `sum = ev.Sum()`; segue o laço.
- `Evs.CountFor` / `CountsByOpponent` / `Recent`: ignoram ajustes (não são adversários).
- `BattleLog.Record`: nunca agrupa uma batalha num ajuste (o último evento sendo ajuste ⇒ novo evento).
- Histórico (UI): ajuste aparece como "Ajuste manual" com os 6 valores.

## `TrainingPokemon` (alterado)

| Campo | JSON | Tipo | Regra |
|---|---|---|---|
| **KnownIvs** | **`knownIvs`** | **int?[6]** | **novo**: cada valor `null` ou 0–31; ausente ⇒ todos `null` |

## `FinalCell` (hierarquia fechada, novo caso)

`FinalKnown(int Value, bool MatchesNotes)` — usado quando `KnownIvs[i]` tem valor.
`MatchesNotes = false` quando as anotações dão faixa e o valor está fora dela, ou dão "conferir".

## Comandos do `TrainerStore` (novos)

| Comando | Efeito | Desfazer |
|---|---|---|
| `AdjustEvs(int[6] targets)` | Acrescenta ajuste no fim (nada se `targets` == totais atuais) | Remove o ajuste |
| `ResetHistory(int[6] targets)` | `Events = [ajuste(targets)]` | Restaura a lista anterior |
| `SetKnownIvs(int?[6] ivs)` | Substitui `KnownIvs` | Restaura os anteriores |
| `ReplaceLevel(int index, LevelEntry entry)` | Substitui e reordena por nível | Restaura a anotação e a posição |

Validação (UI e domínio): EVs 0–255, soma ≤ 510; IVs 0–31 ou vazio; anotação com as mesmas regras do
formulário atual (nível 1–100, espécie da linha, stats ≥ 1, EVs 0–255).
