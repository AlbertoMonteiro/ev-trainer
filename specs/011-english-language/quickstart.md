# Quickstart: validar "App em inglês"

## Pré-requisitos

```bash
dotnet test --solution PokemonEvTrainer.slnx
```

```bash
dotnet run --project src/EvTrainer.Web --launch-profile http
```

Usar perfis limpos do navegador (ou `localStorage.clear()`) e mudar o idioma do navegador nas
configurações (ou, no Playwright, `locale`).

## Cenários

1. **Navegador em inglês** — perfil limpo com `en-US`: o app abre em inglês; `<html lang="en">`.
   Percorrer EVs, IVs, Settings, captura, edição manual, backup, sincronização, menu e avisos: nenhum
   texto em português. (US1, SC-001)
2. **Navegador em português** — perfil limpo com `pt-BR`: tudo como antes. (US1, SC-002)
3. **Lista mista** — `es, pt-BR, en` → português; `fr, ja` → inglês. (US1 cenários 3–4)
4. **Troca manual** — em inglês, digitar uma busca, abrir o menu e tocar "Português (Brasil)": troca
   na hora, a busca e a aba continuam; recarregar com o navegador em inglês: segue em português.
   (US2, SC-003, SC-004)
5. **Usuário atual** — com `evtrainer:v1:prefs` contendo `"language":"pt-BR"` e navegador em inglês:
   abre em português. (FR-009)
6. **Locais em inglês** — em inglês, buscar `route 22`, `pewter gym`, `routes 12-15`: mesmos
   adversários que `rota 22`, `ginásio de pewter`, `rotas 12-15` em português; os cartões mostram
   "Route 22", "Pewter Gym (Brock)". (US3, SC-005)
7. **Backup cruzado** — exportar backup em inglês e importar com o app em português: importa igual.
8. **Larguras** — repetir 1 e 6 a 320 px e 1366 px: textos em inglês sem quebrar layout.
