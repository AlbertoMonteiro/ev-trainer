# Quickstart: Planejador da Elite Four

```bash
dotnet run scripts/BuildLeague.cs          # regenera league.json
dotnet test --solution PokemonEvTrainer.slnx
dotnet run --project src/EvTrainer.Web --launch-profile http
```

1. Menu ☰ → **Planejar a Elite Four**.
2. "Primeira luta", inicial Bulbasaur: Lorelei com Dewgong nv 52 (Thick Fat; Ice Beam, Surf, Hail, Safeguard)…;
   Campeão com Charizard no fim.
3. Adicionar Raichu; golpes Thunderbolt e Dig: Gyarados do Lance marcado 4× (Thunderbolt); Onix do Bruno 2× (Dig);
   resumo atualizado.
4. Adicionar Golem com Earthquake: Gengar da Agatha mostra Earthquake como imune (Levitate).
5. "Sugerir para a equipe": vagas livres preenchidas só com golpes aprendíveis; golpes fixados mantidos.
6. Ameaças: Charizard na equipe → Lapras da Lorelei (Surf) e Onix do Bruno (Rock Tomb) aparecem como ameaças.
7. Trocar para "Revanche": times novos (Lorelei com Piloswine nv 63).
8. Recarregar: luta, inicial e equipe continuam. Com a sync ligada em dois navegadores, mudar a equipe num deles e
   sincronizar o outro: a equipe aparece.
