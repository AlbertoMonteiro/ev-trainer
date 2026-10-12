# Contract: listas da aba EVs

Contrato de UI verificado pelos testes bUnit. "Úteis" = `Evs.IsUseful` para o Pokémon selecionado.

## Região `.main-list`

| Situação | Grades renderizadas (em ordem) | Títulos (`h3.section-title`) |
|---|---|---|
| Sem busca, filtro Úteis | 1 (só úteis, sem recentes) — **inalterado** | `Evs.List.Useful` |
| Sem busca, filtro Todos, há úteis e outros | 2 | `Evs.List.Useful`, `Evs.List.Others` |
| Sem busca, filtro Todos, nenhum útil | 1 | `Evs.List.All` |
| Sem busca, filtro Todos, todos úteis | 1 | `Evs.List.Useful` |
| Busca, há úteis e outros | 2 | `Evs.List.Useful`, `Evs.List.OtherResults` |
| Busca, nenhum útil | 1 | `Evs.List.Results` |
| Busca, todos úteis | 1 | `Evs.List.Useful` |
| Busca sem resultado | 1, vazia, com `Empty.NoResults` | `Evs.List.Results` |

- Cada grade mantém, entre seus itens, a ordem da lista de origem (ordem da busca, ou Pokédex e
  vitaminas no fim sem busca).
- Os recentes continuam fora da `.main-list` sem busca e ocultos com busca.
- Todos os cartões continuam registrando batalha com 1 toque.

## Controle `.segmented` (`OpponentSearchBar`)

- Sem busca: `button.useful` / `button.all` ativos, `aria-pressed` conforme a preferência salva.
- Com busca (texto não vazio após aparar espaços): os dois botões com `disabled` e
  `aria-pressed="false"`; a preferência salva não muda.

## Textos novos (`strings.pt-BR.json`)

| Chave | Texto |
|---|---|
| `Evs.List.OtherResults` | Outros resultados |
| `Evs.List.Others` | Outros adversários |
