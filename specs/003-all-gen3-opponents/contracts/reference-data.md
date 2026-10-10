# Contract: dados de referência e API do domínio (mudanças)

## `pokedex-gen3.json`

```json
{
  "_fonte": "PokéAPI (pokeapi.co): stats base da Geração III (past_stats aplicados) e EV yield (effort atual). Gerado por scripts/BuildPokedex.cs.",
  "stats": ["HP", "Ataque", "Defesa", "SpAtk", "SpDef", "Speed"],
  "species": [
    { "dex": 350, "name": "Milotic", "base": [95, 60, 79, 100, 125, 81], "evolvesFrom": 349, "evYield": [0, 0, 0, 0, 2, 0] }
  ]
}
```

Regenerar: `dotnet run scripts/BuildPokedex.cs` (raiz do repositório).

## Domínio

```csharp
public sealed record PokedexSpecies(int Dex, string Name, int[] Base, int? EvolvesFrom, int[] EvYield);

// ReferenceData.LoadEmbedded(): lê reference-data.json + pokedex-gen3.json
IReadOnlyList<Opponent> Opponents { get; }   // 386 espécies em ordem da Pokédex + 6 vitaminas

static class Evs
{
    // novo: contagem de todos os adversários numa passada (usado pela grade)
    IReadOnlyDictionary<string, int> CountsByOpponent(IReadOnlyList<BattleEvent> events, ReferenceData data);
}
```

Sem mudanças na UI além do volume da lista: os componentes já usam `ReferenceData.Opponents`.
