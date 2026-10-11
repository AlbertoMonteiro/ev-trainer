# Quickstart: validar o layout de desktop

```bash
dotnet test --solution PokemonEvTrainer.slnx
```

```bash
dotnet run --project src/EvTrainer.Web --launch-profile http
```

Use um Pokémon com metas, batalhas e anotações de IV, e pelo menos 3 Pokémon cadastrados.

Medições no console do navegador (DevTools) em cada largura:

```js
// largura usada pelo conteúdo, em % da janela (SC-001)
Math.round(document.querySelector('main.page').getBoundingClientRect().width / innerWidth * 100)
// rolagem horizontal da página (SC-005): deve ser false
document.documentElement.scrollWidth > innerWidth
// cards de adversário na primeira linha da lista principal (FR-003)
(cards => cards.filter(c => c.offsetTop === cards[0].offsetTop).length)([...document.querySelectorAll('.main-list .opponent')])
```

1. **1366 × 768, aba EVs**: painel lateral visível; resumo de EVs à esquerda e fixo ao rolar a lista;
   ≥ 4 cards por linha; clicar num card registra a batalha e o aviso aparece no canto inferior
   direito; conteúdo + painel usam ≥ 70% da largura.
2. **Painel lateral**: clicar no 3º Pokémon seleciona-o mantendo a aba; com Avaliar captura aberta,
   clicar num Pokémon fecha a avaliação; "+ Adicionar Pokémon" abre Ajustes do novo Pokémon.
3. **Aba IVs**: tabela + IVs conhecidos à esquerda, formulário de anotação à direita.
4. **Ajustes**: formulário à esquerda, backup / importação / apagar à direita.
5. **Avaliar captura** (Squirtle Nv 5 Timid `20/9/12/11/12/11`): campos e resultado lado a lado, sem
   rolar a página.
6. **1100 × 800**: layout de desktop sem painel lateral; troca de Pokémon pelo menu ☰.
7. **Redimensionar** de 1366 para 360 com texto digitado na busca e na avaliação de captura: nada se
   perde; aba e seleção mantidas.
8. **360 × 780**: todas as telas iguais à versão anterior; sem rolagem horizontal.
9. **2560 × 1440**: conteúdo limitado (~1440 px + painel) e centralizado; sem esticar.
10. **Teclado**: Tab percorre painel, abas, busca e cards com foco visível; Enter registra a batalha.
11. **Temas** claro e escuro em 1366 px.
