# Contract: equipe atual

## JSON do Pokémon (backup, Gist, aparelho)

```json
{ "currentSpecies": "Electabuzz", "...": "...", "expShare": true, "party": true }
```

- `party`: `true` (equipe) ou `false` (guardado).
- O campo é omitido quando o valor é desconhecido.
- Um campo ausente ou com valor inválido é lido como desconhecido e resolvido por `Party.Normalize`
  ([research R1](../research.md#r1--representação-da-equipe)).
- Leitores antigos ignoram o campo, porque o JSON é tolerante.

## Invariantes (testes)

1. Depois de `Load`, `ImportBackup` e `ApplySync`:
   - há no máximo 6 Pokémon com `Party == true`;
   - nenhum Pokémon tem `Party == null`;
   - nenhum Pokémon guardado tem o Exp. Share.
2. Dados antigos (todos `null`):
   - com 3 Pokémon, todos ficam na equipe;
   - com 8 Pokémon, ficam na equipe os 6 de `UpdatedAt` mais recente. Sem carimbo, valem os de menor `Order`.
3. Merge de dois aparelhos com 7 marcados na equipe: sai o de `UpdatedAt` mais antigo.
4. `JoinParty` com a equipe cheia e sem `replacing` não muda nada e devolve `false`.
5. `LeaveParty` não muda `Events`, `IvLog`, `Targets`, `KnownIvs`, `Pokerus` nem `MachoBrace`.
6. `AddPokemon` com 6 na equipe cria o Pokémon guardado e selecionado.

## UI

### Menu ☰ e painel lateral

- `PokemonList` mostra só `PartyOrdered`.
- Se o selecionado estiver guardado, ele aparece depois, em `.pokemon-list.opened`, sob o rótulo "Aberto (guardado)".
- O menu ganha o item `.party` ("Equipe e guardados"), logo após a lista.
- O painel lateral ganha o botão `.manage-party`.

### `PartyView`

- **Seção "Equipe (n/6)"**: uma linha por Pokémon, com:
  - sprite, nome, nature e total de EV;
  - os botões ↑ e ↓, com `aria-label` "Subir X" e "Descer X";
  - o botão "Guardar";
  - o botão "Abrir".
- **Seção "Guardados (n)"**: uma linha por Pokémon, com:
  - o botão "Pôr na equipe". Com a equipe cheia, ele vira "Trocar…" e abre a escolha de quem sai, entre os 6 da equipe;
  - o botão "Abrir".
- **"Abrir"**: seleciona o Pokémon e fecha a página, voltando às abas.
- **Equipe vazia**: aparece o texto "Nenhum Pokémon na equipe".
