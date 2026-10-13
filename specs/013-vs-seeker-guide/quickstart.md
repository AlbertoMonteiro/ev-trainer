# Quickstart: validar "Guia de treino com VS Seeker"

## Pré-requisitos

```bash
dotnet run scripts/BuildVsSeekerGuide.cs      # regenera vs-seeker-guide.json (só quando atualizar a fonte)
dotnet test --solution PokemonEvTrainer.slnx
dotnet run --project src/EvTrainer.Web --launch-profile http
```

## Cenários

1. **Abrir** — menu → "Onde treinar": abre com o atributo de maior falta do Pokémon selecionado. (FR-001, FR-010)
2. **Sp. Atk antes da Elite Four** — Engineer Bernie (Rota 11) no topo, estágios "Original" (2 Magnemite + Magneton
   nv 18) e "Após Fuchsia" (3 Magneton nv 28, destacado, 6 Sp. Atk). (US1, US2, FR-006b)
3. **HP** — Twins Eli & Anne e Lass Julia (Rota 8) como entradas separadas, cada uma com 4 HP; marca "batalha dupla"
   nas Twins. (FR-005)
4. **Depois da Elite Four** — trocar a fase: aparecem estágios "Após a Elite Four" (ex.: Young Couple Gia & Jes,
   Nidoking + Nidoqueen nv 55) e a ordem muda; reabrir o app mantém a fase. (US2)
5. **Speed** — lista de Speed com o mesmo formato. (US1 cen. 5)
6. **Aviso** — com a meta de Speed completa, abrir HP: Beauty Grace mostra aviso de Speed desperdiçado. (FR-011)
7. **Acesso** — treinadores das Rotas 19–21 com "precisa de Surf". (FR-004)
8. **Inglês e larguras** — repetir 2 em inglês, a 320 e 1366 px.
