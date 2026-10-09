# Contract: `PokemonSprite` e assets de sprite

## Assets

- `src/EvTrainer.Web/wwwroot/sprites/{1..386}.png` — gerados por
  `dotnet run scripts/DownloadSprites.cs` (repo root); o script falha se algum arquivo não for PNG
  64×64 ou faltar.
- `src/EvTrainer.Web/wwwroot/sprites/CREDITS.md` — origem (URL + commit), aviso
  "Pokémon images © The Pokémon Company", licença CC0 do repositório PokeAPI/sprites.
- Publicados e pré-cacheados pelo service worker como qualquer `.png` (research R2).

## Componente `Components/Shared/PokemonSprite.razor`

Parâmetros:

| Parâmetro | Tipo | Uso |
|---|---|---|
| `Name` | string | Nome do adversário/espécie (aceita aliases) |
| `Size` | int | 40 (cards, menu) ou 48 (seletor de espécie) |

Markup observável (testado com bUnit):

```html
<span class="sprite" style="--sprite-size: 40px; --badge: var(--stat-0)" aria-hidden="true">
  <span class="badge">#10</span>
  <img src="sprites/10.png" alt="" width="40" height="40" loading="lazy" decoding="async" class="loaded?" />
</span>
```

- Degradê: `--badge: linear-gradient(135deg, var(--stat-3), var(--stat-4))`; sem EV:
  `var(--accent)`.
- `<img>` só é renderizado quando `HasSprite` e ainda não houve `onerror`.
- `onload` ⇒ classe `loaded` (opacidade 1). `onerror` ⇒ `<img>` removido; o selo permanece.
- Todo o elemento é `aria-hidden="true"`; nenhum texto novo é anunciado.

## Onde aparece

| Tela | Componente | Tamanho |
|---|---|---|
| Cards de adversário (recentes, lista, resultados) | `OpponentCard` | 40 |
| Opções da busca de espécie | `SpeciesPicker` | 48 |
| Linha evolutiva escolhida | `SpeciesPicker` | 48 |
| Lista de Pokémon do menu | `PokemonMenu` (usa `DisplaySpecies`) | 40 |

## Textos novos (pt-BR)

| Chave | Texto |
|---|---|
| `Sprite.Dex` | `#{0}` |
| `Menu.Credits` | `Sprites: PokeAPI/sprites · imagens © The Pokémon Company` |
