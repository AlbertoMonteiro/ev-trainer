# Quickstart: validar "Botão Editar EVs no cabeçalho do resumo"

## Pré-requisitos

```bash
dotnet test --solution PokemonEvTrainer.slnx
```

```bash
dotnet run --project src/EvTrainer.Web --launch-profile http
```

Abrir http://localhost:5010 com um Pokémon que tenha EVs e metas (ex.: 414 EVs, metas somando 508).

## Cenários

1. **Total sem metas** — aba EVs: o cabeçalho mostra "414 / 510", sem "metas" nem "508". (US1 cenário 1,
   SC-001)
2. **Botão no cabeçalho** — o botão "Editar EVs" está à direita do total; não há botão abaixo da grade;
   o cartão ficou mais baixo que antes. (US1 cenário 2, SC-002)
3. **Abrir o editor** — tocar "Editar EVs": o editor abre abaixo da grade como antes e o botão some do
   cabeçalho; cancelar ou salvar: o botão volta. (US1 cenários 3–4, SC-003)
4. **Sem metas** — Pokémon com todas as metas zero: cabeçalho com total e botão, sem erro.
5. **Larguras** — repetir 1–3 a 320 px, 360 px e 1366 px (painel lateral do desktop): cabeçalho em uma
   linha, sem sobreposição nem rolagem horizontal. (FR-008, SC-004)
