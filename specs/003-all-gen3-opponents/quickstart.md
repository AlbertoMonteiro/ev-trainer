# Quickstart: validar adversários #1–386

## 1. Regenerar a Pokédex com EV yields

```bash
dotnet run scripts/BuildPokedex.cs
```

Esperado: `pokedex-gen3.json: 386 species` e cada espécie com `evYield`.

## 2. Testes

```bash
dotnet test --solution PokemonEvTrainer.slnx
```

Inclui: 392 adversários em ordem, consistência #1–151 com o `reference-data.json`, Milotic/Zigzagoon
na busca e no filtro Úteis, `CountsByOpponent` igual a `CountFor`.

## 3. No app

```bash
dotnet run --project src/EvTrainer.Web --launch-profile http
```

1. Aba EVs → buscar "milo" ⇒ Milotic com "+2 SPD" e sprite; tocar ⇒ SpDef +2.
2. Buscar "350" ⇒ Milotic.
3. Filtro "Todos" ⇒ #1 a #386 e as 6 vitaminas no fim; rolar até o fim sem travar.
4. Metas com Speed > 0 ⇒ "Úteis" inclui Zigzagoon.
