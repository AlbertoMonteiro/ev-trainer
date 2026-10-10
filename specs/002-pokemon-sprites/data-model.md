# Data Model: Sprites dos Pokémon

Nada novo é persistido (localStorage e backup não mudam). Tudo abaixo é derivado de dados já
embutidos (`reference-data.json`, `pokedex-gen3.json`) ou de arquivos estáticos.

## Sprite (arquivo estático)

| Campo | Regra |
|---|---|
| Caminho | `sprites/{dex}.png`, relativo ao `<base href>` |
| `dex` | 1–386 (todas as espécies de `pokedex-gen3.json`) |
| Formato | PNG 64×64, fundo transparente, estilo FireRed/LeafGreen |
| Origem | PokeAPI/sprites @ `35fdbe9…`, registrada em `sprites/CREDITS.md` |

## SpriteBadge (derivado, domínio puro)

`SpriteBadge.For(string name, ReferenceData data, Pokedex dex) → SpriteBadge`

| Campo | Tipo | Regra |
|---|---|---|
| `Dex` | int? | `dex.Find(name)?.Dex` ou `data.Lookup(name)?.Dex`; null para vitaminas e nomes desconhecidos |
| `Stats` | int[] (0–2 itens) | Índices dos stats com EV > 0 no adversário (`data.Lookup(name)`), ordenados por rendimento desc. e depois índice; no máximo 2; vazio se não houver dado de EV |
| `Initial` | string | Primeira letra do nome canônico (maiúscula), usada quando `Dex` é null |
| `HasSprite` | bool | `Dex is >= 1 and <= 386` |

Apresentação (UI): `Stats` vazio ⇒ `--accent`; 1 item ⇒ `--stat-{i}`; 2 itens ⇒ degradê
`--stat-{a}` → `--stat-{b}`. Rótulo: `Dex` ⇒ chave `Sprite.Dex` (`#{0}`), senão `Initial`.

Exemplos (testes): Caterpie ⇒ `Dex 10, Stats [0]`; Ivysaur ⇒ `Dex 2, Stats [3, 4]`; Venusaur
(2 SpA, 1 SpD) ⇒ `Stats [3, 4]`; Calcium ⇒ `Dex null, Stats [3], Initial "C"`; Treecko (#252) ⇒
`Dex 252, Stats []`; "Agumon" ⇒ `Dex null, Stats [], Initial "A"`.

> Atualizado pela feature 003: as espécies #152–386 passaram a ter EV yield, então Treecko agora é
> `Dex 252, Stats [5]`; o caso "sem dado de EV ⇒ cor de destaque" vale só para nomes fora dos dados
> do jogo (ex.: "Agumon").

## Espécie exibida de um Pokémon em treino (derivado)

`TrainingPokemon.DisplaySpecies` = espécie da última entrada de `IvLog` (lista ordenada por nível)
se ela existir na linha evolutiva; senão `Species[0].Name`; senão `Name`.
