# Contract: `league.json`

Gerado por `dotnet run scripts/BuildLeague.cs` (raiz do repositório). Nomes em inglês, como no jogo.

```json
{
  "_fonte": "pret/pokefirered@<commit>: ... Gerado por scripts/BuildLeague.cs.",
  "moves": {
    "Thunderbolt": { "type": "Electric", "power": 95, "accuracy": 100, "pp": 15, "damage": "normal" },
    "Dragon Rage": { "type": "Dragon", "power": 1, "accuracy": 100, "pp": 10, "damage": "fixed" },
    "Hail": { "type": "Ice", "power": 0, "accuracy": 0, "pp": 10, "damage": "status" }
  },
  "tms": { "TM24": "Thunderbolt", "HM05": "Flash" },
  "species": {
    "Gengar": {
      "types": ["Ghost", "Poison"],
      "abilities": ["Levitate"],
      "level": [[1, "Lick"], [29, "Shadow Punch"]],
      "tm": ["TM24", "HM05"],
      "tutor": ["Dream Eater"]
    }
  },
  "trainers": [
    {
      "id": "lorelei", "name": "Lorelei",
      "teams": {
        "first": [{ "species": "Dewgong", "level": 52, "item": null, "ability": "Thick Fat",
                    "moves": ["Ice Beam", "Surf", "Hail", "Safeguard"] }],
        "rematch": [ ... ]
      }
    },
    {
      "id": "champion", "name": "Rival",
      "teams": { "first.bulbasaur": [ ... ], "first.charmander": [ ... ], "first.squirtle": [ ... ],
                 "rematch.bulbasaur": [ ... ], "rematch.charmander": [ ... ], "rematch.squirtle": [ ... ] }
    }
  ]
}
```

- `moves` contém todos os golpes aprendíveis (nível, TM/HM, tutor) ou usados nos times.
- `species` contém as 386 espécies do Pokédex do app (Deoxys com uma entrada).
- Chave do time do Campeão: `<luta>.<inicial do jogador>`.
- Ordem dos treinadores: Lorelei, Bruno, Agatha, Lance, Campeão; ordem dos Pokémon: a do jogo.
- `level`: `[nível, golpe]` na ordem do jogo; `tm`: rótulos `TMnn`/`HMnn` (golpe em `tms`); `tutor`: golpes.
