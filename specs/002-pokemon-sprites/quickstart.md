# Quickstart: validar os sprites

## 1. Gerar os sprites (uma vez, ou para atualizar)

```bash
dotnet run scripts/DownloadSprites.cs
```

Esperado: `sprites: 386 files` e 386 PNGs em `src/EvTrainer.Web/wwwroot/sprites/` (+ `CREDITS.md`),
cerca de 250 KB no total.

## 2. Testes

```bash
dotnet test --solution PokemonEvTrainer.slnx
```

Inclui `Domain/SpriteBadgeTests` (cores e rótulos dos exemplos do [data-model](./data-model.md)) e
`Components/Shared/PokemonSpriteTests` (markup do [contrato](./contracts/pokemon-sprite.md), troca
para o selo no `onerror`, classe `loaded` no `onload`).

## 3. No app

```bash
dotnet run --project src/EvTrainer.Web --launch-profile http
```

Em 360 px:

1. Aba EVs: cada card de Pokémon mostra o sprite à esquerda do nome; vitaminas mostram o selo.
2. Ajustes → "Trocar espécie" → buscar "squir": opções e linha com sprites.
3. Menu ☰: sprite de cada Pokémon em treino e linha de créditos no rodapé.
4. DevTools → Network → bloquear `sprites/*`, recarregar: todos os lugares mostram selos coloridos
   (Caterpie vermelho "#10", Ivysaur degradê azul→verde "#2"), nenhum ícone quebrado, nenhum card
   muda de tamanho.

## 4. Publish e offline

```bash
dotnet publish src/EvTrainer.Web -c Release -o artifacts/publish
```

Servir `artifacts/publish/wwwroot`, abrir uma vez, ficar offline e recarregar: os sprites continuam
aparecendo (vieram do cache do service worker). O download inicial cresce até ~400 KB (SC-004).
