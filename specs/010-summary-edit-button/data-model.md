# Data Model: Botão Editar EVs no cabeçalho do resumo

Nenhuma entidade persistida muda: backup, armazenamento local, preferências e sincronização ficam
iguais. As metas continuam salvas e mostradas por atributo na grade do resumo.

## Valores calculados (não salvos)

| Valor | Origem | Mostrado em |
|---|---|---|
| Total atual | soma de `Store.SelectedTotals` | cabeçalho do resumo, "{total} / 510" |
| Soma das metas | soma de `Targets` | **não é mais mostrada** |
| Atual/meta por atributo | `SelectedTotals[i]`, `Targets[i]` | grade do resumo (inalterado) |

## Estado do cartão

| `_editing` | Cabeçalho | Abaixo da grade |
|---|---|---|
| falso | sprite, total, `button.edit-evs` | nada |
| verdadeiro | sprite, total | `EvEditor` |
