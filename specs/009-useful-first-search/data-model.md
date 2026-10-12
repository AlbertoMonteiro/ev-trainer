# Data Model: Busca com úteis primeiro

Nenhuma entidade persistida muda: backup, armazenamento local, preferências (`Filter` continua
`"useful"` / `"all"`) e sincronização ficam iguais.

## Valores calculados (não salvos)

### Partição úteis / outros

- **Entrada**: lista de `SearchHit` (adversário + locais de treinador quando a busca casou por
  treinador), totais de EV do Pokémon selecionado e suas metas.
- **Saída**: `Useful` e `Others`, duas listas de `SearchHit`.
- **Regras**:
  - um item está em `Useful` se e só se `Evs.IsUseful(opponent, totals, targets)`;
  - partição estável: a ordem relativa da entrada é mantida em cada lista;
  - todo item de entrada aparece em exatamente uma das listas (nada some, nada se repete);
  - metas todas zero ou todas completas → `Useful` vazia.

### Estado do controle de filtro

| Busca | Preferência salva | Botão Úteis | Botão Todos |
|---|---|---|---|
| vazia | useful | marcado, ativo | desmarcado, ativo |
| vazia | all | desmarcado, ativo | marcado, ativo |
| com texto | qualquer | desmarcado, desativado | desmarcado, desativado |
