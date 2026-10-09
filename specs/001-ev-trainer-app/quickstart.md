# Quickstart: validar o EVs Trainer

Guia de execução e verificação ponta a ponta. Formatos e regras ficam em
[data-model.md](./data-model.md) e [contracts/](./contracts/); a criação dos projetos está em
[plan.md — Scaffolding pela CLI](./plan.md#scaffolding-pela-cli).

## Pré-requisitos

- .NET SDK `11.0.100-rc.1.26425.128` ou mais novo da linha 11 (`dotnet --version` dentro do
  repositório deve mostrar `11.0.`; o `global.json` garante isso).
- Workload de WebAssembly para relinking no publish: `dotnet workload install wasm-tools`.
- Servidor estático para testar o publish: `dotnet tool install -g dotnet-serve`.
- Navegador Chromium para PWA/offline (DevTools → Application).

## 1. Formatação e testes (portão do deploy)

```bash
dotnet format PokemonEvTrainer.slnx --verify-no-changes
```

```bash
dotnet test --solution PokemonEvTrainer.slnx
```

Esperado: formatação sem diferenças e todos os testes verdes, incluindo:

| Grupo | Cobre |
|---|---|
| `Domain/EvTotalsTests` | Limites 255/510, vitaminas, multiplicador, aliases (HANDOFF §6 "EV") |
| `Domain/IvTests` | Squirtle nível 5 e interseção de 6 níveis (HANDOFF §6 "IV") |
| `Domain/SearchTests` | "rota 4", "rota 1", "brock", "nidoran f" |
| `Domain/UsefulFilterTests` | Metas `[252,0,2,252,0,4]` com DEF = 2 |
| `Domain/BackupFormatTests` | Leitura de `user-data-export.json` (SC-003) e ida e volta (SC-004) |
| `Domain/SpreadsheetImportTests` | Batalhas e níveis com TAB e `;` |
| `Localization/*` | Paridade de chaves, chaves usadas existem, sem texto fixo, troca ao vivo (SC-010) |
| `Components/*` | Toque registra e desfaz, menu, abas, status, backup, estados vazios |

## 2. Rodar em desenvolvimento

```bash
dotnet run --project src/EvTrainer.Web --launch-profile http
```

Abre pelo servidor Gateway do .NET 11 em `http://localhost:5010`. (No SDK 11 RC1 o `dotnet watch`
pode cair com um erro interno de hot reload, `DeltaMetadataWriter`; nesse caso use `dotnet run`.) Com a janela em 360 px (DevTools → modo dispositivo):

1. Estado vazio → "Adicionar Pokémon" abre Ajustes (US1-1).
2. Cadastrar Squirtle Timid com linha Squirtle/Wartortle/Blastoise e metas `252/0/2/252/0/4`.
3. Tocar em Caterpie → "+1 HP", `×1`; "Desfazer" volta a 0 (US1-2, US1-3).
4. Ligar Pokérus e Macho Brace → Caterpie dá +4 (US1-4).
5. Recarregar → dados preservados, 📱 "Salvo neste aparelho" (US1-8).
6. Buscar "rota 1" → só Pidgey e Rattata (US3-3).
7. Aba IVs: nível 5 com `20 9 12 11 12 11` → faixas do HANDOFF §6 (US4-1).
8. Sem rolagem horizontal; alternar tema no menu; `<html lang="pt-BR">`; o menu **não** mostra
   seletor de idioma (só há pt-BR) (SC-008, FR-035).

## 3. Migração dos dados atuais (SC-003)

1. Em janela anônima, "Importar backup" → `user-data-export.json` da raiz.
2. Prévia "1 Pokémon: 1 novo, 0 substituídos" → confirmar.
3. Comparar totais de EV e faixa Final de IV com o `app-atual.html` com os mesmos dados.
4. "Exportar backup" → importar em outro navegador → dados idênticos (SC-004).

## 4. Publish e offline (como no GitHub Pages)

```bash
dotnet publish src/EvTrainer.Web -c Release -o artifacts/publish
```

```bash
dotnet serve -d artifacts/publish/wwwroot -p 5080
```

1. Abre em `http://localhost:5080/` (base href `/` localmente).
2. Application → Service Workers ativo; "Offline" + recarregar → app abre e registra batalhas
   (SC-005).
3. Application → Manifest: instalável, `display: standalone` (FR-031).
4. Network: nenhuma requisição a domínios externos (Princípio V).

## 5. Deploy

Push na `main` dispara `.github/workflows/deploy.yml`: `dotnet format --verify-no-changes` →
`dotnet test` → base href `/ev-trainer/` → `dotnet publish` → `404.html` + `.nojekyll` → Pages.
Uma vez: Settings → Pages → Source: **GitHub Actions**.

Em `https://albertomonteiro.github.io/ev-trainer/`:

- Carrega, instala como app e funciona offline.
- `https://albertomonteiro.github.io/ev-trainer/qualquer-coisa` cai no app (fallback 404).
- Um teste vermelho ou arquivo mal formatado impede o deploy.

## 6. Migração para a GA do .NET 11 (quando lançada)

```bash
dotnet package list --outdated --include-prerelease
```

Para cada pacote `11.0.0-rc.*` listado, `dotnet package add <id> --version <versão GA> --project
<projeto>`; atualizar o SDK do `global.json` com `dotnet new globaljson --sdk-version <GA>
--roll-forward latestFeature --test-runner Microsoft.Testing.Platform --force`; rodar as seções 1 e 4.
