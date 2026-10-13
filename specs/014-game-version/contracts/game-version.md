# Contract: versão do jogo

## Menu (`PokemonMenu`)
- Grupo `.group-label` = `L["Menu.GameVersion"]` ("Versão do jogo" / "Game version").
- `.choices.versions > button[data-version=fireRed|leafGreen]`, textos `Version.FireRed` ("FireRed") e
  `Version.LeafGreen` ("LeafGreen"), `aria-pressed` na ativa.
- Clique: `Store.SetGameVersion(v)`; o menu fecha; a busca aberta é recalculada com o texto mantido.

## Busca (`EvTab`)
- `Search.Find(_query, Store.Preferences.GameVersion)`.

## Textos novos
| Chave | pt-BR | en |
|---|---|---|
| `Menu.GameVersion` | Versão do jogo | Game version |
| `Version.FireRed` | FireRed | FireRed |
| `Version.LeafGreen` | LeafGreen | LeafGreen |
