# Data Model: Versão do jogo

## UiPreferences (aparelho)

| Campo | Tipo | Regra |
|---|---|---|
| `GameVersion` | string | `"fireRed"` (padrão) \| `"leafGreen"`; outro valor → `"fireRed"` |

## locations.json (gerado)

```json
{ "pt": "Safari Zone", "en": "Safari Zone",
  "trainers": [],
  "wild": { "fireRed": ["Scyther", "..."], "leafGreen": ["Pinsir", "..."] } }
```

## Domínio

- `GameVersion` { FireRed, LeafGreen }.
- `ReferenceData.WildPlaces(GameVersion)`: espécie → locais (nome pt) onde é selvagem naquela versão.
- `OpponentSearch.Find(query, GameVersion)`: casa por nome/número, locais de treinador (iguais) e locais selvagens da
  versão; vitaminas também pela anotação curada.
