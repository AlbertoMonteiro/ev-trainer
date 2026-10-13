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
- `location`: "Rota N" (pt, igual a `reference-data.json`) ou o local das Ilhas Sevii como em `locations.json`
  ("Kindle Road"); `access` ⊆ {`surf`, `cyclingRoad`, `victoryRoad`, `sevii`, `seviiPostgame`} (`sevii` = ilhas 1–3,
  após Blaine; `seviiPostgame` = ilhas 4–7, só depois da Elite Four; nas ilhas, `surf` para Swimmers).
- Espécies com o nome da Pokédex do app (`Nidoran♀`, `Mr. Mime`, `Farfetch'd`).
- Rotas de Kanto e Ilhas Sevii.
