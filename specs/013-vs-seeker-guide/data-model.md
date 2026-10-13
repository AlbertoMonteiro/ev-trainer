# Data Model: Guia de treino com VS Seeker

## vs-seeker-guide.json (gerado, embutido)

Ver [contracts/vs-seeker-guide-json.md](./contracts/vs-seeker-guide-json.md).

## Entidades de domínio

### GuideTrainer
| Campo | Tipo | Regra |
|---|---|---|
| `Id` | string | id do decomp (ex.: `ENGINEER_BERNIE`), único |
| `TrainerClass` | string | ex.: "Engineer" |
| `Name` | string | ex.: "Bernie" (como no jogo, capitalizado) |
| `Location` | string | texto pt de local (ex.: "Rota 11"), traduzido por `LocationNames` |
| `Double` | bool | batalha dupla |
| `Access` | string[] | chaves de condição: `surf`, `cyclingRoad`, `victoryRoad` |
| `Stages` | GuideStage[] | 1–6, em ordem de índice |

### GuideStage
| Campo | Tipo | Regra |
|---|---|---|
| `Index` | int | 0–5 (ver research R2) |
| `Team` | (Species, Level)[] | 1–6 Pokémon; espécie existe na Pokédex |
| `RequiresEliteFour` | bool (derivado) | `Index >= 4` |
| `Evs` | int[6] (calculado) | soma de `evYield` do time |

### Fase do jogador
`UiPreferences.GuidePhase`: `"before"` (padrão) | `"after"`. Estágios visíveis: before → `Index <= 3`; after → todos.

### GuideEntry (resultado para a tela)
`Trainer`, `VisibleStages` (em ordem), `Best` (estágio visível com mais EV do atributo), `Score` (EV do atributo no
`Best`), `WastedStats` (atributos que o `Best` dá e cuja meta do Pokémon selecionado já foi atingida).
