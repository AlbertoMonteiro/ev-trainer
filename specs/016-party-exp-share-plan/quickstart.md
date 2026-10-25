# Quickstart: Equipe atual e plano de batalhas com Exp. Share

## Pré-requisitos

```bash
export PATH=$HOME/.dotnet:$PATH DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet test --solution PokemonEvTrainer.slnx
dotnet format PokemonEvTrainer.slnx --verify-no-changes
dotnet run --project src/EvTrainer.Web
```

## Cenários

### 1. Equipe (US1, US2)

1. Importe um backup com 8 Pokémon gerado antes desta versão, ou seja, sem o campo `party`.
   - O menu ☰ mostra 6 Pokémon: os alterados mais recentemente.
2. Em ☰ → **Equipe e guardados**, os outros 2 aparecem em "Guardados".
3. Toque em **Guardar** num da equipe.
   - O menu mostra 5.
4. Toque em **Pôr na equipe** num guardado.
   - O menu volta a mostrar 6.
5. Com a equipe cheia, toque em **Trocar…** num guardado e escolha quem sai.
   - Os EVs e o histórico dos dois continuam iguais.
6. Toque em **Abrir** num guardado.
   - As abas mostram esse Pokémon.
   - O menu o mostra em "Aberto (guardado)".
7. Exporte o backup.
   - Cada Pokémon tem `"party": true` ou `"party": false`.

Para os limites e a migração, veja [contracts/party.md](contracts/party.md).

### 2. Busca pelo treinador (US6)

1. Na aba de EVs, digite `bernie`.
   - Aparece o cartão **Engineer Bernie · Rota 11** com duas revanches:
     - original: +4 Sp. Atk;
     - após Fuchsia: Magneton ×3, +6 Sp. Atk.
   - Magnemite e Magneton aparecem na grade.
2. Toque em **Registrar batalha inteira** na revanche de 3 Magneton.
   - O Pokémon selecionado ganha +6 Sp. Atk, ou mais com Macho Brace ou Pokérus.
   - Se outro Pokémon estiver com o Exp. Share, ele também ganha.
3. Toque em **Desfazer** no aviso.
   - Os EVs dos dois voltam ao que eram.
4. Digite `engineer`.
   - Aparecem os cartões de Braxton e Bernie.

Mais exemplos em [contracts/trainer-search.md](contracts/trainer-search.md).

### 3. Plano com Exp. Share (US3, US4)

1. Prepare dois Pokémon na equipe:
   - Electabuzz com meta de 120 Sp. Atk, EVs 0, selecionado;
   - Jolteon com meta de 96 Sp. Atk, EVs 0, segurando o Exp. Share;
   - nenhum dos dois com Pokérus.
2. No cartão do Bernie (revanche de 3 Magneton), toque em **Planejar com Exp. Share**.
   - O plano mostra:
     - 8 batalhas com Electabuzz na frente (Macho Brace) e Jolteon com o Exp. Share;
     - 4 batalhas com Jolteon na frente e Electabuzz com o Exp. Share;
     - total 12; sozinhos seriam 18.
3. Toque em **Usar esta configuração** na segunda fase.
   - Jolteon fica selecionado e com o Macho Brace.
   - Electabuzz fica com o Exp. Share e sem o Macho Brace.
4. Registre uma batalha inteira e reabra o plano.
   - A fase 2 mostra 3 batalhas.
5. Marque o Pokérus no Electabuzz e reabra o plano.
   - O plano passa a ter 9 batalhas (caso 3 de [contracts/exp-share-plan.md](contracts/exp-share-plan.md)).

### 4. Layout

Confira em 320 px e em 1366 px:

- o menu;
- a página da equipe;
- o cartão do treinador;
- o plano.

Em todos, não pode haver rolagem horizontal, e os alvos de toque precisam ter pelo menos 44 px.
