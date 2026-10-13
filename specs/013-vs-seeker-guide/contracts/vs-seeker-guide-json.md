# Contract: vs-seeker-guide.json

Gerado por `dotnet run scripts/BuildVsSeekerGuide.cs` (raiz do repositório); embutido como
`EvTrainer.vs-seeker-guide.json`.

```json
{
  "_fonte": "pret/pokefirered@<commit>: src/vs_seeker.c (sRematches), src/data/trainers.h, src/data/trainer_parties.h",
  "trainers": [
    {
      "id": "ENGINEER_BERNIE",
      "class": "Engineer",
      "name": "Bernie",
      "location": "Rota 11",
      "double": false,
      "access": [],
      "stages": [
        { "index": 0, "team": [["Magnemite", 18], ["Magnemite", 18], ["Magneton", 18]] },
        { "index": 3, "team": [["Magneton", 28], ["Magneton", 28], ["Magneton", 28]] }
      ]
    }
  ]
}
```

Regras:
- Ordem dos treinadores: a de `sRematches` (agrupa por rota, na ordem do jogo).
- `stages` em ordem crescente de `index`, sem repetir times idênticos ao estágio anterior.
- `location`: "Rota N" (pt, igual a `reference-data.json`); `access` ⊆ {`surf`, `cyclingRoad`, `victoryRoad`}.
- Espécies com o nome da Pokédex do app (`Nidoran♀`, `Mr. Mime`, `Farfetch'd`).
- Só mapas de Kanto.
