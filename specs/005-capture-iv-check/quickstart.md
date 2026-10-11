# Quickstart: validar a avaliação de captura

```bash
dotnet test --solution PokemonEvTrainer.slnx
```

```bash
dotnet run --project src/EvTrainer.Web --launch-profile http
```

1. **Avaliar**: menu → **Avaliar captura** → espécie Squirtle, nível 5, nature Timid, stats
   `20/9/12/11/12/11` ⇒ faixas `12–31, 4–31, 10–29, 20–31, 12–31, 14–31`, ATK esmaecido (não conta),
   veredito Incerto com dica de nível baixo, nature Favorece.
2. **Stats que contam**: tocar em ATK ⇒ passa a contar e o veredito é recalculado; **Restaurar
   padrão** volta ao automático; reabrir a tela com Squirtle mantém a escolha.
3. **Melhor até agora**: **Nova avaliação** ⇒ aviso "guardado como melhor"; digitar stats maiores ⇒
   cartão diz "Melhor"; **Nova avaliação** ⇒ substitui (Desfazer volta o anterior); stats menores ⇒
   "Pior" e não substitui; **Esquecer** ⇒ sem melhor (Desfazer).
4. **Adicionar à lista**: com uma avaliação válida, **Adicionar à minha lista** ⇒ novo Squirtle
   selecionado, linha Squirtle → Wartortle → Blastoise, nature Timid, aba IVs com a anotação do nível 5
   e as mesmas faixas; Desfazer remove.
5. **Sem Pokémon**: apagar todos ⇒ a tela vazia oferece **Avaliar captura**.
6. **Erros**: stat impossível ⇒ "?" com explicação e veredito indisponível; Shedinja ⇒ HP "não dá
   para saber".
7. **Celular**: 360 px, claro e escuro, sem rolagem horizontal, offline.
8. **Backup**: exportar ⇒ o arquivo não contém o melhor até agora nem a marcação de stats.
