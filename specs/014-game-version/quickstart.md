# Quickstart: validar "Versão do jogo"

```bash
dotnet run scripts/BuildLocations.cs   # regenera locations.json com selvagens por versão
dotnet test --solution PokemonEvTrainer.slnx
dotnet run --project src/EvTrainer.Web --launch-profile http
```

1. **Padrão** — dados novos: menu mostra FireRed marcado. (FR-002)
2. **Safari Zone** — FireRed: buscar "safari" traz Scyther e não Pinsir; LeafGreen: o inverso. (US1 cen. 1–2)
3. **Exclusivos** — LeafGreen: "rota 24" traz Bellsprout; FireRed: traz Oddish. (SC-002)
4. **Treinadores iguais** — "rota 20" nas duas versões: os Pokémon com "Treinadores: Rota 20" são os mesmos. (FR-005)
5. **Troca com busca aberta** — com "safari" digitado, trocar a versão no menu: lista muda, texto continua. (US2)
6. **Persistência** — recarregar: versão mantida. (FR-003)
