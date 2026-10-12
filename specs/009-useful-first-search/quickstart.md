# Quickstart: validar "Busca com úteis primeiro"

## Pré-requisitos

```bash
dotnet test --solution PokemonEvTrainer.slnx
```

```bash
dotnet run --project src/EvTrainer.Web --launch-profile http
```

Abrir http://localhost:5010 com um Pokémon que tenha metas parciais (ex.: Ataque 252 com 40 EVs,
Velocidade 252 com 0, Defesa 0 com 0).

## Cenários

1. **Busca de rota agrupada** — digitar `rota 3`: os que dão só Ataque/Velocidade aparecem sob "Úteis
   para as metas"; os que dão outros atributos, sob "Outros resultados", depois. Cada grupo na ordem de
   antes. (US1, SC-001)
2. **Nenhum útil** — buscar um adversário que só dá Defesa (ex.: `geodude`): uma única lista
   "Resultados". (US1 cenário 2)
3. **Busca por treinador** — buscar `brock`: os cartões continuam mostrando o local do treinador nos
   dois grupos. (US1 cenário 4)
4. **Reagrupar após batalha** — com a busca aberta, registrar batalhas até completar a meta de Ataque:
   os que dão Ataque passam para "Outros resultados" sem refazer a busca; desfazer devolve. (FR-006)
5. **Controle durante a busca** — com "Úteis" marcado, digitar algo: Úteis/Todos ficam desativados e
   nenhum marcado; tocar não faz nada. Apagar a busca: "Úteis" volta marcado. Repetir com "Todos".
   (US2, SC-003, FR-008)
6. **Todos sem busca** — tocar "Todos" com a busca vazia: "Úteis para as metas" primeiro e "Outros
   adversários" depois. (US3)
7. **Sem regressão** — "Úteis" sem busca mostra exatamente os mesmos adversários de antes. (SC-004)
8. **Larguras** — repetir 1 e 5 a 360 px e a 1366 px: sem rolagem horizontal; o controle não muda a
   largura da caixa de busca ao começar/apagar a busca.
