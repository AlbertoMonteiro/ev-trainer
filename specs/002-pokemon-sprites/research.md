# Research: Sprites dos Pokémon

**Feature**: [spec.md](./spec.md) · **Plan**: [plan.md](./plan.md) · **Date**: 2026-10-09

## R1. Fonte e obtenção dos sprites

- **Decision**: baixar os PNG de
  `PokeAPI/sprites/sprites/pokemon/versions/generation-iii/firered-leafgreen/{dex}.png` (#1–386),
  **fixados no commit `35fdbe9bdec8f519f882c3edc3c0185f08af4d86`**, com um file-based app
  `scripts/DownloadSprites.cs` (`dotnet run scripts/DownloadSprites.cs`), salvando em
  `src/EvTrainer.Web/wwwroot/sprites/{dex}.png` e gerando `wwwroot/sprites/CREDITS.md` (origem,
  commit, aviso "© The Pokémon Company", licença CC0 do repositório). O script valida a assinatura
  PNG e o tamanho 64×64 de cada arquivo e falha se faltar algum.
- **Rationale**: FR-004/FR-012 (imagens dentro do app, processo reproduzível); mesma abordagem do
  `BuildPokedex.cs` e da regra "dotnet CLI primeiro". Conferido: a pasta FR/LG cobre #1, #152 e
  #386; os arquivos têm 64×64 e ~0,5–0,9 KB (≈250 KB no total, SC-004).
- **Alternatives considered**: apontar o app para `raw.githubusercontent.com` (viola o Princípio V e
  falha offline); Emerald/Ruby-Sapphire (estilo diferente do jogo que o usuário joga); baixar à mão
  (não reproduzível).

## R2. Offline e pré-carregamento

- **Decision**: nada novo. O service worker publicado (template `--pwa`) já guarda no cache, na
  instalação, todo asset publicado que casa com `/\.png$/`, então os 386 sprites entram no cache
  junto com o app. O "buraco" (perder a conexão antes de a instalação terminar) é aceito (decisão do
  usuário) e coberto pelo selo (R4).
- **Rationale**: FR-005 sem código extra. O `loading="lazy"` (FR-009) controla quando a página
  decodifica e mostra cada imagem; o pré-cache do service worker em segundo plano é o que garante o
  uso offline e não bloqueia a interface.
- **Alternatives considered**: imagem única com todos os sprites (descartada pelo usuário);
  excluir sprites do pré-cache e guardar sob demanda (mais buracos offline).

## R3. Exibição

- **Decision**: componente `PokemonSprite` com caixa de tamanho fixo (40 px nos cards e no menu,
  48 px no seletor de espécie), `image-rendering: pixelated` (pixel art nítida ao reduzir 64→40/48),
  `<img loading="lazy" decoding="async" alt="" width height>`; no card de adversário o sprite fica à
  esquerda do nome, sem mudar a área de toque (o card inteiro continua sendo o botão).
- **Rationale**: FR-001–FR-003, FR-007 (tamanho fixo ⇒ zero deslocamento, SC-003), FR-010
  (`alt=""` + `aria-hidden` no selo: o nome visível já é o texto acessível).
- **Alternatives considered**: `background-image` em CSS (sem eventos de erro para o selo);
  `<picture>`/srcset (só há um tamanho de origem).

## R4. Selo de reserva e detecção de falha

- **Decision**: o selo é sempre desenhado por baixo; o `<img>` fica por cima com `opacity: 0` e
  ganha a classe `loaded` no evento `@onload` (transição de opacidade, sem transição com
  `prefers-reduced-motion`). No `@onerror`, o componente para de renderizar o `<img>` e sobra o
  selo. Sem JavaScript próprio (eventos nativos do Blazor).
- **Cor**: regra de domínio pura `SpriteBadge.For(...)` (testável com xUnit): stats com EV > 0 do
  adversário, em ordem de rendimento e depois de índice, no máximo 2 (conferido: nenhum adversário
  dá EV em mais de 2 stats) ⇒ cor única ou degradê; sem dado de EV (#152–386 ou nome desconhecido)
  ⇒ cor de destaque. Usa os tokens `--stat-0…5`/`--accent` já existentes (tema claro e escuro).
- **Rótulo**: `#{dex}` via chave de idioma `Sprite.Dex`; sem número (vitaminas, espécie digitada no
  app antigo) ⇒ inicial do nome.
- **Rationale**: FR-006–FR-008, SC-002 (nenhum ícone quebrado).
- **Alternatives considered**: `onerror` em JS trocando o `src` (JS próprio desnecessário);
  silhueta/Poké Ball genérica (perde a informação do stat).

## R5. Espécie do Pokémon em treino (menu)

- **Decision**: espécie da anotação de nível mais recente (`IvLog` está ordenado por nível ⇒ a
  última), senão a primeira de `Species`; o número vem de `Pokedex.Find(nome)`; sem match ⇒ selo
  com a inicial.
- **Rationale**: FR-003 e premissa da spec (não existe "espécie atual" persistida).

## R6. Crédito

- **Decision**: rodapé do menu ☰ com o texto da chave `Menu.Credits` ("Sprites: PokeAPI/sprites ·
  imagens © The Pokémon Company") e link para o repositório (`target="_blank" rel="noopener"`), além
  do `CREDITS.md` ao lado dos arquivos.
- **Rationale**: FR-011/FR-013 com o menor acréscimo de UI; o link só é seguido se o usuário tocar
  (não é requisição automática, Princípio V preservado).
- **Alternatives considered**: tela "Sobre" separada (mais UI do que o necessário agora).
