# Quickstart: validar a sincronização com Gist

```bash
dotnet test --solution PokemonEvTrainer.slnx
```

```bash
dotnet run --project src/EvTrainer.Web --launch-profile http
```

Pré-requisitos: uma conta GitHub de teste e um token fine-grained com só "Gists: Read and write"
(siga o passo a passo da própria tela). Use dois perfis de navegador (ou o celular e o PC) para
simular dois aparelhos. **Não** use o token em nada além disso; revogue-o ao terminar.

1. **Conectar (aparelho A)**: menu → **Sincronização** → colar o token → **Conectar** ⇒ "Conectado
   como @conta"; no GitHub aparece um Gist **secreto** "EVs Trainer" com `evs-trainer.json` no formato
   do backup; o token não aparece na tela nem no backup exportado.
2. **Token ruim**: um token qualquer ⇒ "token inválido"; um token sem permissão de gists ⇒ "sem
   permissão de gists"; nada é salvo.
3. **Segundo aparelho (B)**: conectar com a mesma conta ⇒ B recebe os Pokémon de A (aviso
   "N adicionados").
4. **Automática**: em A, registrar 3 batalhas seguidas ⇒ ~5 s depois, status "sincronizado"; em B,
   voltar à aba ⇒ o Pokémon chega atualizado sem tocar em nada.
5. **Conflito**: desconectar a internet de A, alterar o Pokémon X em A e depois em B (com internet);
   reconectar A ⇒ vale a alteração mais recente; nada mais novo é perdido.
6. **Exclusão**: apagar o Pokémon Y em B ⇒ some em A após sincronizar; **Desfazer** em B ⇒ volta nos
   dois.
7. **Offline**: sem internet, treinar normalmente ⇒ status "pendente"; ao voltar a internet ⇒
   "sincronizado".
8. **Token revogado** no GitHub ⇒ status de erro com "Reconectar"; dados locais intactos.
9. **Desconectar** ⇒ token apagado (verificar `localStorage`), dados locais e Gist intactos.
10. **Celular e desktop**: tela de sincronização em 360 px e em 1366 px; textos sem chaves cruas.
