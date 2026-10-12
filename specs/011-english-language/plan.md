# Implementation Plan: App em inglês

**Branch**: `011-english-language` | **Date**: 2026-10-12 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/011-english-language/spec.md`

## Summary

A infraestrutura de idiomas já existe (catálogo JSON embutido por idioma, `LanguageState`,
`JsonStringLocalizer`, seletor no menu que aparece com 2+ idiomas, `lang` do documento, teste de
paridade de chaves). Esta feature: (1) adiciona `strings.en.json` com as ~260 chaves traduzidas;
(2) torna o idioma salvo opcional (`UiPreferences.Language` passa a `string?`) e, sem idioma salvo,
escolhe pelo `navigator.languages` com uma função pura de domínio (`LanguageMatcher.Pick`: primeiro
`pt*` → `pt-BR`, primeiro `en*` → `en`, senão `en`), também aplicada no script inline do `index.html`
para o `lang` e o texto de erro já saírem certos; (3) traduz os locais de referência com um arquivo
embutido `reference-data.en.json` (texto pt → texto en) usado na exibição dos cartões e indexado pela
busca nos dois idiomas, com `RouteExpander` reconhecendo também "route/routes".

## Technical Context

**Language/Version**: C# 15 / .NET 11 (SDK RC fixado no `global.json`)

**Primary Dependencies**: nenhuma nova.

**Storage**: `evtrainer:v1:prefs.language` passa a ser opcional (ausente/null = seguir o navegador);
valores existentes preservados. Backup e sincronização inalterados (ver [data-model.md](./data-model.md)).

**Testing**: xUnit v3 (escolha de idioma, expansão de rotas em inglês, busca bilíngue, tradução de
locais, completude de `reference-data.en.json`); bUnit (troca de idioma no menu mantendo estado, cartões
com locais traduzidos, telas em inglês sem texto fixo e sem texto em português); paridade de chaves
já existente passa a cobrir `en`.

**Target Platform**: browsers de celular e desktop (320–2560 px).

**Project Type**: SPA estática PWA no GitHub Pages, 2 projetos (inalterado).

**Performance Goals**: troca de idioma < 1 s (re-render já existente); busca indexa 2 textos de local
por adversário em vez de 1 — custo de memória desprezível (≤ 392 adversários).

**Constraints**: nenhum texto em pt na interface em inglês; `lang` correto desde o primeiro paint;
sem permissão de localização; JS mínimo (só leitura de `navigator.languages` no script inline e uma
função em `interop.js`).

**Scale/Scope**: 1 arquivo de textos novo (~260 chaves), 1 arquivo de locais (~80 entradas),
1 classe de domínio nova, ajustes em `UiPreferences`, `Program.cs`, `IBrowserApi`/`interop.js`,
`index.html`, `OpponentSearch`/`RouteExpander`, `OpponentCard`/`ReferenceData`; testes.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.* Constituição v1.3.0.

| Princípio / Seção | Verificação | Pré | Pós |
|---|---|---|---|
| I. Estático no browser | Tudo no cliente; idioma lido do navegador, sem serviço externo. | ✅ | ✅ |
| II. Dois projetos | Nenhum projeto novo; `LanguageMatcher` e tradução de locais são C# puro em `Domain/`. | ✅ | ✅ |
| III. Test-First | Testes de domínio e bUnit escritos antes e vistos falhar; o teste `Embedded_catalog_has_only_pt_BR_for_now` é atualizado para a nova realidade (pt-BR + en), não removido. | ✅ | ✅ |
| IV. Fidelidade Gen 3 | Nomes oficiais em inglês do FireRed para locais; nomes de Pokémon/natures inalterados. | ✅ | ✅ |
| V. Dados locais | Idioma é preferência do aparelho; não entra no backup nem na sincronização. | ✅ | ✅ |
| VI. Mobile-first, bom no desktop, acessível | `lang` do documento acompanha o idioma; seletor no menu (2 toques); textos em inglês mais curtos ou equivalentes — validação a 320 px. | ✅ | ✅ |
| VII. Outros idiomas | É a aplicação direta do princípio: novo arquivo de recursos + tradução dos dados de referência; componentes não ganham texto fixo; paridade de chaves testada; idioma lembrado no aparelho. A única mudança fora de recursos é a detecção pelo navegador, que o princípio não proíbe. | ✅ | ✅ |
| Restrições / dotnet CLI primeiro | Sem pacotes; JS mínimo e documentado em `contracts/ui-shell` desta feature. | ✅ | ✅ |

Resultado: **sem violações**.

## Project Structure

### Documentation (this feature)

```text
specs/011-english-language/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── language.md
├── checklists/
│   └── requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
src/EvTrainer.Web/
├── Domain/LanguageMatcher.cs                   # novo: Pick(IEnumerable<string> browserLanguages) → "pt-BR" | "en"
├── Domain/RouteExpander.cs                     # "route(s) N, M–K" além de "rota(s)"
├── Domain/OpponentSearch.cs                    # indexa local pt + en
├── Domain/Reference/LocationNames.cs           # novo: tradução pt → en dos locais (reference-data.en.json)
├── Domain/Model/TrainingPokemon.cs             # UiPreferences.Language: string? (null = seguir navegador)
├── Localization/strings.en.json                # novo: todas as chaves
├── State/IPokemonRepository.cs                 # IBrowserApi.Languages()
├── State/LocalStorageRepository.cs             # evt.languages
├── State/TrainerStore.cs                       # SetLanguage grava escolha explícita
├── Components/Evs/OpponentCard.razor           # locais de treinador no idioma ativo
├── Program.cs                                  # idioma salvo ?? LanguageMatcher.Pick(navegador)
├── wwwroot/js/interop.js                       # languages(): navigator.languages
├── wwwroot/index.html                          # script inline: lang + texto de erro conforme idioma
└── EvTrainer.Web.csproj                        # EmbeddedResource EvTrainer.reference-data.en.json
reference-data.en.json                          # novo (raiz, ao lado de reference-data.json): { "locations": { pt: en } }
tests/EvTrainer.Tests/
├── Domain/LanguageMatcherTests.cs              # novo
├── Domain/RouteExpanderTests.cs                # route/routes
├── Domain/SearchTests.cs                       # busca em inglês
├── Domain/LocationNamesTests.cs                # novo: todo local de referência tem tradução
├── Domain/ModelJsonTests.cs                    # prefs: language ausente/null/antigo
├── State/TrainerStoreTests.cs                  # salvar tema/filtro preserva language null
├── Components/Shell/PokemonMenuTests.cs        # troca mantém estado, opções no próprio idioma
├── Components/Evs/OpponentSearchTests.cs       # cartões com local em inglês
└── Localization/*                              # paridade cobre en; NoHardcodedText em inglês; sem pt em en
```

**Structure Decision**: mesma solução de 2 projetos; regras puras (escolha de idioma, tradução e
expansão de locais) no domínio, aplicação na inicialização e nos componentes.

## Complexity Tracking

Sem violações da constituição a justificar.
