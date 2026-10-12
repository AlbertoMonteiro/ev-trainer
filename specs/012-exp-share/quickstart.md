# Quickstart: validar "Exp. Share"

## Pré-requisitos

```bash
dotnet test --solution PokemonEvTrainer.slnx
dotnet run --project src/EvTrainer.Web --launch-profile http
```

Abrir http://localhost:5010 com dois Pokémon (ex.: Wartortle e Abra).

## Cenários

1. **Marcar o portador** — selecionar Wartortle; no cartão de resumo, "Exp. Share" → Abra. (US1 cen. 5)
2. **Ganho básico** — tocar Caterpie: aviso "Caterpie: +1 HP · Abra +1 HP"; abrir o Abra: +1 HP, histórico
   "Caterpie ×1 · Exp. Share". (US1 cen. 1, US2 cen. 3)
3. **Macho Brace** — MACHO no Wartortle, tocar Caterpie: Wartortle +2, Abra +1. (US1 cen. 2)
4. **Pokérus do portador** — selecionar Abra, PKRS; voltar ao Wartortle (MACHO desligado), tocar Caterpie:
   Wartortle +1, Abra +2. (US1 cen. 3)
5. **Desfazer** — tocar Desfazer no aviso: os dois voltam. (US2 cen. 2)
6. **Limite** — com o Abra em 510, tocar Caterpie: aviso indica limite para o Abra; Wartortle ganha. (US2 cen. 4)
7. **Vitamina** — usar HP Up no Wartortle: Abra não muda. (FR-012)
8. **Portador como principal** — selecionar Abra e tocar Caterpie: só o Abra ganha. (FR-008)
9. **Trocar portador** — marcar outro Pokémon: o Abra deixa de ter Exp. Share. (US1 cen. 6)
10. **Backup** — exportar, apagar dados, importar: o portador volta. (SC-004)
11. **Inglês** — repetir 2 com o app em inglês: textos em inglês.
