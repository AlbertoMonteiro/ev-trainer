# Data Model: Pokémon identificado pela espécie, sem apelido

## TrainingPokemon (alterado)

| Campo | Antes | Depois |
|---|---|---|
| `Name` (apelido livre, obrigatório) | `string` | **removido** |
| `CurrentSpecies` | — | `string`: nome de uma espécie de `Species`, ou `""` quando a linha não tem espécie |
| `DisplaySpecies` (derivado) | última anotação → 1ª espécie → nome | **removido** (substituído por `CurrentSpecies`) |

Demais campos (`Nature`, `Species`, `Targets`, `Events`, `IvLog`, `Pokerus`, `MachoBrace`, `Order`,
`KnownIvs`) inalterados.

### Regras

- **Invariante**: `CurrentSpecies == ""` ou `Species.Any(s => s.Name == CurrentSpecies)`.
- **Ordem da linha**: o índice em `Species` define "anterior"/"posterior".
- **`AdvanceTo(species)`**: se `species` está na linha e (`CurrentSpecies == ""` ou índice de `species` >
  índice de `CurrentSpecies`), `CurrentSpecies = species`; senão não muda.
- **`InsertLevel(entry)`**: insere ordenado por nível (como hoje) e chama `AdvanceTo(entry.Species)`.
- **Ajustes salvos** (`SaveSettings(current, nature, targets, species)`): `current` precisa estar na linha
  nova; a forma atual é gravada como veio.
- **Troca de linha** (escolher espécie fora da linha atual): a forma atual é a espécie escolhida.

## CurrentForm.Resolve (novo, domínio puro)

`Resolve(string? name, IReadOnlyList<SpeciesEntry> line, IReadOnlyList<LevelEntry> ivLog) → string`

1. `name` (sem espaços nas pontas) igual, sem diferenciar maiúsculas, a uma espécie da linha → nome da
   linha.
2. Senão, entre as anotações cuja espécie está na linha, a de maior índice na linha.
3. Senão, a primeira espécie da linha com nome não vazio.
4. Senão, `""`.

Aplicado em toda leitura (`PokemonJson.FromDto`): armazenamento local, backup e `user-data-export.json`.

| Exemplo | name | linha | anotações | Resultado |
|---|---|---|---|---|
| apelido = espécie | `"Kirlia"` | Ralts, Kirlia, Gardevoir | — | Kirlia |
| caixa diferente | `"kirlia"` | Ralts, Kirlia, Gardevoir | — | Kirlia |
| apelido livre | `"Gardi"` | Ralts, Kirlia, Gardevoir | Ralts nv 5, Kirlia nv 20 | Kirlia |
| apelido livre, sem notas | `"Gardi"` | Ralts, Kirlia, Gardevoir | — | Ralts |
| sem espécie | `"X"` | (1 espécie sem nome) | — | `""` |

## Formato JSON (HANDOFF §3.1)

Sem propriedade nova. `"name"` passa a conter a forma atual:

- **Escrita**: `"name": CurrentSpecies`.
- **Leitura**: `CurrentSpecies = CurrentForm.Resolve(dto.name, species, ivLog)`; `"name"` ausente ou
  nulo é aceito.

Versões anteriores do app leem o arquivo novo normalmente: veem o nome da espécie como "apelido".

## SettingsFormModel (alterado)

- Remove `Name`; adiciona `Current` (`string`).
- `IsValid`: metas válidas, linha com 1–3 espécies e `Current` presente na linha.
- `From(pokemon)`: `Current = pokemon.CurrentSpecies`.
