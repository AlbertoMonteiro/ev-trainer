# Data Model: Exp. Share

## TrainingPokemon (alterado)

| Campo | Tipo | JSON | Regra |
|---|---|---|---|
| `ExpShare` | bool | `"expShare"` (gravado só quando `true`; ausente = `false`) | No máximo um Pokémon com `true` no conjunto. |

## BattleEvent (alterado)

| Campo | Tipo | JSON | Regra |
|---|---|---|---|
| `Shared` | bool | `"x"` (gravado só quando `true`; ausente = `false`) | Batalha recebida pelo Exp. Share. Nunca em ajuste manual. |

Compatibilidade: arquivos sem esses campos leem como `false`; `Read(Write(x)) == x` continua valendo.

## Regras de ganho (calculadas)

Multiplicador ao registrar 1 batalha não-vitamina contra `O` com principal `P` e portador `H ≠ P`:

| P.Pokérus | P.Macho | H.Pokérus | H.Macho | mult. P | mult. H |
|---|---|---|---|---|---|
| não | não | não | — | 1 | 1 |
| não | sim | não | — | 2 | 1 |
| não | não | sim | — | 1 | 2 |
| sim | sim | sim | — | 4 | 2 |
| sim | não | não | sim | 2 | 1 |

(`H.Macho` nunca conta para ganhos compartilhados.) Cada um aplica seus limites (255/atributo, 510/total).

## Portador único (TrainerStore)

- `SetExpShare(id | null)`: desmarca todos, marca `id` (se existir e ≠ nada); carimba os Pokémon alterados.
- Normalização após `Load`, `ImportBackup`, `ApplySync`: com 2+ marcados, fica o de `UpdatedAt` mais recente
  (empate: menor `Order`).
- `DeletePokemon(portador)`: o portador some com o Pokémon; o desfazer devolve a marcação.
- Selecionar o portador como principal: nada é compartilhado (`H == P`).

## Resultado do registro

`SharedRecord(string MainId, RecordResult Main, string? HolderId, RecordResult? Holder)` — `Holder*` nulos quando não
houve compartilhamento (sem portador, portador = principal, ou vitamina).
