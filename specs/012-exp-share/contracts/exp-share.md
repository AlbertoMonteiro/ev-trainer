# Contract: Exp. Share

## Domínio (`BattleLog`)

- `RecordShared(TrainingPokemon holder, string opponent, ReferenceData data) → RecordResult`: adiciona 1 batalha
  com `Shared = true`, `Multiplier = holder.Pokerus ? 2 : 1`; agrupa só com o último evento se `Shared`, mesmo
  adversário canônico e mesmo multiplicador.
- `UndoLastShared(TrainingPokemon holder, string opponent, ReferenceData data)`: se o último evento for compartilhado
  e do mesmo adversário canônico, remove 1 unidade (remove o evento ao chegar a 0); senão não faz nada.

## Store (`TrainerStore`)

- `string? ExpShareHolderId`
- `void SetExpShare(string? id)`
- `SharedRecord? RecordBattle(string opponent)` — compartilha quando há portador ≠ selecionado e o adversário não é
  vitamina.
- `void UndoBattle(SharedRecord record)` — desfaz no principal e, se houver, no portador.

## UI

### `EvSummary` (cartão de resumo)

- Com 2+ Pokémon: `label[for=exp-share]` "Exp. Share" + `select#exp-share.exp-share` com opção `""` =
  `ExpShare.None` ("Ninguém"/"Nobody") e uma opção por outro Pokémon (`value` = id, texto = rótulo do Pokémon); o
  valor selecionado é o portador atual (ou `""`). Mudar chama `SetExpShare`.
- Com 1 Pokémon: nada é renderizado.
- Se o selecionado for o próprio portador: o seletor mostra o portador e uma nota `ExpShare.Self` ("Este Pokémon
  está com o Exp. Share: as batalhas dele não são compartilhadas").

### Aviso (`EvTab`)

- Principal: texto atual (`Toast.EvGain` / `Toast.NoEvGain`).
- Compartilhado: acrescenta `Toast.ExpShareGain` ("{0}: {1}" com o rótulo do portador e os ganhos) ou
  `Toast.ExpShareNoGain` ("{0}: limite atingido").
- Desfazer: `UndoBattle(record)`.

### Histórico (`RecentHistory`)

- Evento com `Shared`: `span.meta.shared` com `History.Shared` ("Exp. Share").

## Textos novos (pt-BR / en)

| Chave | pt-BR | en |
|---|---|---|
| `ExpShare.Label` | Exp. Share | Exp. Share |
| `ExpShare.None` | Ninguém | Nobody |
| `ExpShare.Self` | Este Pokémon está com o Exp. Share: as batalhas dele não são compartilhadas. | This Pokémon holds the Exp. Share: its battles aren't shared. |
| `Toast.ExpShareGain` | {0} {1} | {0} {1} |
| `Toast.ExpShareNoGain` | {0}: sem EV (limite) | {0}: no EVs (limit) |
| `History.Shared` | Exp. Share | Exp. Share |
