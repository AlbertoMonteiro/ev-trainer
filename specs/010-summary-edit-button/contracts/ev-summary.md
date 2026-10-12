# Contract: cartão de resumo da aba EVs (`EvSummary`)

Contrato de UI verificado pelos testes bUnit.

## Estrutura

```text
section.card.summary
├── div.card-head
│   ├── h2.summary-who            # sprite (inalterado)
│   ├── span.totals.num.muted     # L["Evs.Summary.Totals", total]  → "414 / 510"
│   └── button.edit-evs           # L["Evs.Edit"]; só com o editor fechado
├── div.summary-grid              # 6 × .mini (inalterado)
└── EvEditor                      # só com o editor aberto
```

- `span.totals` não contém a soma das metas nem a palavra do recurso de metas.
- Existe no máximo um `button.edit-evs` no cartão, e ele está dentro de `.card-head`.
- Clicar em `button.edit-evs` abre o `EvEditor`; o botão some do cabeçalho até o editor fechar
  (salvar ou cancelar), e então volta.

## Textos (`strings.pt-BR.json`)

| Chave | Antes | Depois |
|---|---|---|
| `Evs.Summary.Totals` | `{0} / 510 · metas {1}` | `{0} / 510` |
| `Evs.Edit` | `Editar EVs` | inalterado |

## Estilo

- `.summary .edit-evs`: largura pelo conteúdo, `min-height: 44px`, sem quebra de linha.
- `.summary .totals`: sem quebra de linha, empurrado para a direita junto ao botão.
- A 320 px de largura: cabeçalho em uma linha, sem sobreposição nem rolagem horizontal.
