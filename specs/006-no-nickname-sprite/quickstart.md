# Quickstart: Pokémon identificado pela espécie, sem apelido

## Pré-requisitos

SDK do `global.json` instalado; na raiz do repositório:

```bash
dotnet test
```

```bash
dotnet run --project src/EvTrainer.Web
```

## Cenários de validação

### 1. Evoluir sem renomear (Story 1)

1. Menu → "Adicionar Pokémon". Nos Ajustes, confirmar que não há campo "Nome".
2. Buscar e escolher **Ralts** → a linha mostra Ralts, Kirlia, Gardevoir com Ralts marcado como forma atual.
3. Salvar sem digitar nada → menu mostra a imagem e o texto "Ralts".
4. Nos Ajustes, tocar no chip **Kirlia** → salvar → menu, aba EVs e aba IVs mostram Kirlia.
5. Aba IVs → anotar nível 30 como **Gardevoir** → a forma atual vira Gardevoir sem passar pelos Ajustes.
6. Anotar nível 10 como **Ralts** → a forma atual continua Gardevoir.

### 2. Imagem no resumo de EVs (Story 2)

1. Com o Pokémon do cenário 1, abrir a aba EVs → o resumo mostra a imagem de 64 px e não mostra o nome em
   texto; os totais continuam visíveis.
2. Leitor de tela (ou inspeção do DOM): o título é anunciado como "EVs de Gardevoir".
3. Largura 360 px (DevTools): sem rolagem horizontal.
4. Offline com o sprite fora do cache: aparece o selo colorido com "#282".

### 3. Dados antigos (Story 3)

1. Importar um backup antigo em que um Pokémon tem `"name": "Gardi"`, linha Ralts/Kirlia/Gardevoir e
   anotação nível 20 como Kirlia → aparece como Kirlia; batalhas, metas e IVs iguais.
2. Exportar e importar de novo → idêntico; o arquivo traz `"name": "Kirlia"`.
3. `user-data-export.json` de referência continua importando sem erro (`LegacyExportGoldenTests`).

## Testes que comprovam

- Domínio: `CurrentFormTests` (tabela de `Resolve`, `AdvanceTo`), `ModelJsonTests`/`BackupFormatTests`
  (ida e volta, `"name"` antigo).
- Componentes: `SettingsFormTests` (sem nome, forma atual), `SpeciesPickerTests` (chips alternáveis),
  `EvSummaryTests` (sprite + `aria-label`, sem texto do nome), `PokemonMenuTests`, `LevelFormTests`.
- Idioma: `KeyParityTests`, `NoHardcodedTextTests`.
