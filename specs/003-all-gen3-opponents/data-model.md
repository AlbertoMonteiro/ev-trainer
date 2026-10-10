# Data Model: Adversários de todas as espécies do FireRed

Nada muda no que é persistido (localStorage e backup). Mudam os dados de referência embutidos.

## `pokedex-gen3.json` (gerado por `scripts/BuildPokedex.cs`)

Cada item de `species` ganha um campo:

| Campo | Tipo | Regra |
|---|---|---|
| `dex` | int | 1–386 (existente) |
| `name` | string | existente (Kanto com os nomes do `reference-data.json`) |
| `base` | int[6] | stats base Gen III (existente, com `past_stats`) |
| `evolvesFrom` | int? | existente |
| **`evYield`** | int[6] | **novo**: `effort` atual da PokéAPI, ordem `[HP, Ataque, Defesa, SpAtk, SpDef, Speed]`; soma ≥ 1 |

## `PokedexSpecies` (domínio)

Ganha `EvYield` (int[6]).

## `ReferenceData.Opponents` (derivado)

Ordem e composição (392 itens):

1. #1–151 do `reference-data.json` (EV yield curado, `WildLocation`, presença em `TrainersByLocation`).
2. #152–386 do `pokedex-gen3.json`: `Opponent(Dex, Name, EvYield, WildLocation: "", IsVitamin: false)`.
3. As 6 vitaminas do `reference-data.json` (`Dex = null`, `IsVitamin = true`).

Invariantes (testadas):

- `Opponents.Count == 392`; dex 1–386 sem lacunas nem repetição.
- Para #1–151: `pokedex.EvYield == reference.EvYield` (0 diferenças).
- Todo adversário não-vitamina tem soma de EV yield ≥ 1.
- `Lookup` por nome encontra qualquer espécie (ex.: "Milotic" ⇒ dex 350, `[0,0,0,0,2,0]`).

## `Evs.CountsByOpponent` (derivado, novo)

`IReadOnlyDictionary<string, int>` nome canônico → soma de `Count` dos eventos; mesmo resultado de
`CountFor` para cada nome, calculado numa passada.
