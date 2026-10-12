# Tasks: App em inglês

**Input**: Design documents from `/specs/011-english-language/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/language.md, quickstart.md

**Tests**: obrigatórios (Princípio III — TDD): cada teste é escrito e visto falhar antes do código.

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Setup

Nenhuma tarefa: sem pacotes nem projetos novos.

## Phase 2: Foundational

**Purpose**: o catálogo inglês existir com todas as chaves; sem isso nenhuma história funciona.

- [X] T001 Em `tests/EvTrainer.Tests/Localization/JsonStringLocalizerTests.cs`, trocar `Embedded_catalog_has_only_pt_BR_for_now` por `Embedded_catalog_has_pt_BR_first_then_en`: `catalog.Languages` = `["pt-BR", "en"]`, `Find("en", "Language.Name")` = "English", `Find("en", "App.Title")` = "EVs Trainer". Ver falhar.
- [X] T002 Criar `src/EvTrainer.Web/Localization/strings.en.json` com **todas** as chaves de `strings.pt-BR.json`, na mesma ordem, mesmos marcadores `{n}` por chave, traduzidas para inglês natural de jogador de Pokémon (termos do jogo em inglês: "Nature", "EVs", "IVs", "Pokérus", "HP/Attack/Defense/Sp. Atk/Sp. Def/Speed" para `Stat.Long.*`, `Stat.Short.*` iguais; notas "Great/Good/Fair/Poor"; "Route" em vez de "Rota"); `Language.Name` = "English". O glob `Localization/strings.*.json` do csproj já embute o arquivo. T001 e `KeyParityTests` verdes.
- [X] T003 [P] Teste novo em `tests/EvTrainer.Tests/Localization/KeyParityTests.cs`: `English_texts_have_no_portuguese_words` — nenhum valor de `strings.en.json` contém (palavra inteira, sem diferenciar maiúsculas/acentos) termos de controle: "rota", "ginásio", "ataque", "defesa", "buscar", "editar", "salvar", "cancelar", "úteis", "metas", "nível", "não", "você", "treinador", "adversário", "sincroniz", "configura". Ajustar `strings.en.json` até passar.

**Checkpoint**: catálogo bilíngue completo; o seletor do menu já aparece (2 idiomas).

## Phase 3: User Story 1 — Abrir o app no idioma do navegador (P1) 🎯 MVP

**Goal**: sem idioma salvo, o app abre no idioma do navegador (pt* → pt-BR, en* → en, senão en).

**Independent Test**: perfil limpo com navegador em `en-US` → tudo em inglês e `<html lang="en">`; com `pt-BR` → português.

- [X] T004 [P] [US1] Criar `tests/EvTrainer.Tests/Domain/LanguageMatcherTests.cs` cobrindo a tabela de `data-model.md`: `["en-US","pt-BR"]`→`en`; `["pt-PT"]`→`pt-BR`; `["es","pt-BR","en"]`→`pt-BR`; `["fr","ja"]`→`en`; `[]`→`en`; `["EN-gb"]`→`en`; `["pt"]`→`pt-BR`; entradas vazias/espaços ignoradas. E `Resolve(saved, available, browser)`: salvo disponível (`"pt-BR"`) vence o navegador; salvo `null` → `Pick(browser)`; salvo desconhecido (`"de"`) → `Pick(browser)`.
- [X] T005 [US1] Criar `src/EvTrainer.Web/Domain/LanguageMatcher.cs` (C# puro): `public static string Pick(IEnumerable<string> browserLanguages)` (subtag primário antes de `-`, `OrdinalIgnoreCase`; `pt`→`"pt-BR"`, `en`→`"en"`; nenhum → `"en"`) e `public static string Resolve(string? saved, IReadOnlyCollection<string> available, IEnumerable<string> browserLanguages)` ("valor salvo disponível no catálogo → usado; ausente, null ou desconhecido → idioma detectado pelo navegador"). T004 verde.
- [X] T006 [P] [US1] Em `tests/EvTrainer.Tests/Domain/ModelJsonTests.cs`: preferências sem `language` → `Language` null; `"language":null` → null; `"language":"pt-BR"` → "pt-BR"; serializar `new UiPreferences()` não grava idioma (ou grava null) e relê null. Em `tests/EvTrainer.Tests/State/TrainerStoreTests.cs`: com `Language` null, mudar tema e filtro mantém `Language` null no repositório; `SetLanguage("en")` grava "en".
- [X] T007 [US1] Em `src/EvTrainer.Web/Domain/Model/TrainingPokemon.cs`, `UiPreferences.Language` passa a `public string? Language { get; init; }` (padrão `null`; comentário: "null = seguir o idioma do navegador"); ajustar usos que assumiam não nulo (`src/EvTrainer.Web/Program.cs`, `src/EvTrainer.Web/State/TrainerStore.cs`, testes de suporte). T006 verde.
- [X] T008 [US1] Em `src/EvTrainer.Web/State/IPokemonRepository.cs`, `IBrowserApi` ganha `IReadOnlyList<string> Languages();`; implementar em `src/EvTrainer.Web/State/LocalStorageRepository.cs` (ou onde `BrowserApi` estiver) chamando `evt.languages` (resultado vazio em erro de JS); adicionar `languages()` em `src/EvTrainer.Web/wwwroot/js/interop.js` conforme `contracts/language.md` ("`navigator.languages`; se ausente, `[navigator.language]`; se nada, `[]`"); `tests/EvTrainer.Tests/Support/FakeBrowserApi.cs` ganha `BrowserLanguages` configurável (padrão `["pt-BR"]`).
- [X] T009 [US1] Em `src/EvTrainer.Web/Program.cs`: `var language = LanguageMatcher.Resolve(store.Preferences.Language, catalog.Languages, browser.Languages());` → `LanguageState.Current = language` e `IBrowserApi.SetLanguage(language)` (o `lang` do documento); não gravar o idioma detectado.
- [X] T010 [US1] Em `src/EvTrainer.Web/wwwroot/index.html`, script inline: mesma regra (pref salva `pt-BR`/`en` → usa; senão percorre `navigator.languages || [navigator.language]`, primeiro `pt*`→`pt-BR`, `en*`→`en`, senão `en`); define `lang` no `<html>` e os textos de `#blazor-error-ui` (pt: "Ocorreu um erro inesperado." / "Recarregar"; en: "An unexpected error has occurred." / "Reload") — envolver os textos em `<span class="error-text">` e no `a.reload` para o script trocar.
- [X] T011 [P] [US1] Em `tests/EvTrainer.Tests/Localization/NoHardcodedTextTests.cs` (ou arquivo novo `EnglishScreensTests.cs` na mesma pasta), teste `English_screens_show_no_portuguese`: `Language.Current = "en"`, renderizar `Home` com um Pokémon com eventos, IV log e capture, percorrer as abas EVs/IVs/Ajustes, menu, editor manual, captura e sincronização (como os testes existentes de `NoHardcodedText`) e verificar que o texto renderizado não contém as palavras de controle de T003 (exceto dados do usuário e nomes de locais ainda não traduzidos — US3 cobre os locais).

**Checkpoint**: US1 completa — navegador em inglês abre o app inteiro em inglês.

## Phase 4: User Story 2 — Trocar o idioma manualmente (P1)

**Goal**: o menu troca o idioma na hora, mantendo o estado, e a escolha prevalece nas próximas aberturas.

**Independent Test**: em inglês, digitar busca, trocar para português no menu → troca imediata com a busca mantida; reabrir → português.

- [X] T012 [US2] Em `tests/EvTrainer.Tests/Components/Shell/PokemonMenuTests.cs`: seção de idioma com `button[data-lang=pt-BR]` "Português (Brasil)" e `button[data-lang=en]` "English", `aria-pressed="true"` no ativo; clicar `en` → `Language.Current == "en"`, `Store.Preferences.Language == "en"`, `Browser.Language == "en"`.
- [X] T013 [US2] Em `tests/EvTrainer.Tests/Components/Shell/AppShellTests.cs` (ou `EvTabFlowTests.cs`): com o app em português, digitar uma busca na aba EVs, trocar para inglês pelo menu → textos da aba em inglês, o campo de busca mantém o texto, a aba e o Pokémon selecionado não mudam.
- [X] T014 [US2] Em `tests/EvTrainer.Tests/Domain/LanguageMatcherTests.cs`: `Resolve("en", ["pt-BR","en"], ["pt-BR"])` = "en" (escolha manual vence o navegador) — se já coberto por T004, só confirmar.
- [X] T015 [US2] Ajustar `src/EvTrainer.Web/Components/Shell/PokemonMenu.razor` / `src/EvTrainer.Web/State/TrainerStore.cs` só se T012–T013 falharem (o seletor e `SetLanguage` já existem); garantir que a troca não recria o estado da `EvTab` (busca mantida).

**Checkpoint**: US1 + US2 — o pedido central está pronto.

## Phase 5: User Story 3 — Locais e anotações no idioma ativo (P2)

**Goal**: cartões mostram locais em inglês; busca aceita nomes em inglês ("route 3", "pewter gym").

**Independent Test**: em inglês, buscar "route 3" e "pewter gym" → mesmos adversários que "rota 3"/"ginásio de pewter" em português, com locais em inglês nos cartões.

- [X] T016 [P] [US3] Criar `tests/EvTrainer.Tests/Domain/LocationNamesTests.cs`: todo nome de `ReferenceData.TrainersByLocation` e todo `WildLocation` não vazio tem tradução em `LocationNames` (lista os faltantes na falha); nenhuma tradução contém as palavras de controle de T003 nem "raro", "pesca", "parcial", "foge", " em "; `Display("Rota 22", "en")` = "Route 22"; `Display("Rota 22", "pt-BR")` = "Rota 22"; texto sem tradução volta igual.
- [X] T017 [P] [US3] Em `tests/EvTrainer.Tests/Domain/RouteExpanderTests.cs`: "route 3, 4, 22" → "route 3, route 4, route 22"; "routes 12–15" → "route 12, route 13, route 14, route 15"; casos "rota(s)" existentes continuam iguais.
- [X] T018 [P] [US3] Em `tests/EvTrainer.Tests/Domain/SearchTests.cs`: "route 22" devolve os mesmos adversários que "rota 22"; "pewter gym" devolve os de "Ginásio de Pewter (Brock)" com `TrainerLocations` = `["Ginásio de Pewter (Brock)"]` (texto pt, identidade); "routes 12-15" ≡ "rotas 12-15"; "route 1" não casa "route 12" (limite de dígito mantido).
- [X] T019 [US3] Criar `reference-data.en.json` na raiz do repositório: `{ "locations": { "<texto pt>": "<texto en>" } }` cobrindo as 34 chaves de `trainersByLocation` e os 46 `wildLocation` distintos de `reference-data.json`, com nomes oficiais do FireRed (ex.: "Rota 22"→"Route 22", "Laboratório do Prof. Oak"→"Prof. Oak's Lab", "Ginásio de Pewter (Brock)"→"Pewter Gym (Brock)", "Rota 19 (parcial)"→"Route 19 (partial)", "Mt. Moon (raro)"→"Mt. Moon (rare)", "Pesca"→"Fishing", "Rival: Laboratório, Rota 22, Cerulean"→"Rival: Lab, Route 22, Cerulean", "Rotas 12–15, Safari"→"Routes 12–15, Safari"); nomes já em inglês repetidos iguais. Embutir em `src/EvTrainer.Web/EvTrainer.Web.csproj`: `<EmbeddedResource Include="../../reference-data.en.json" LogicalName="EvTrainer.reference-data.en.json" />`.
- [X] T020 [US3] Criar `src/EvTrainer.Web/Domain/Reference/LocationNames.cs`: carrega `EvTrainer.reference-data.en.json` (`LoadEmbedded()`), `string Display(string ptText, string language)` e `IEnumerable<string> AllForms(string ptText)` (pt + en, sem repetir); registrar como singleton em `src/EvTrainer.Web/Program.cs` e em `tests/EvTrainer.Tests/Support/AppTestContext.cs`. T016 verde.
- [X] T021 [US3] Em `src/EvTrainer.Web/Domain/RouteExpander.cs`: regex aceita `(?<word>rotas?|routes?)` e expande para `"{rota|route} N"` conforme a palavra encontrada (singular). T017 verde.
- [X] T022 [US3] Em `src/EvTrainer.Web/Domain/OpponentSearch.cs`: construtor recebe `LocationNames`; `Entry.Wild` vira lista com as formas preparadas pt e en; cada local de treinador guarda o nome pt e as formas preparadas pt/en; casa se qualquer forma casar; `SearchHit.TrainerLocations` continua com o nome pt. Atualizar o registro DI e os testes que constroem `OpponentSearch`. T018 verde.
- [X] T023 [US3] Em `src/EvTrainer.Web/Components/Evs/OpponentCard.razor`: injetar `LocationNames` e `LanguageState`; exibir `L["Evs.Card.Trainers", string.Join(", ", TrainerLocations.Select(l => Names.Display(l, Language.Current)))]`. Teste bUnit em `tests/EvTrainer.Tests/Components/Evs/OpponentSearchTests.cs`: em `en`, buscar "pewter gym" → cartão mostra "Pewter Gym (Brock)"; em `pt-BR`, buscar "ginásio de pewter" → "Ginásio de Pewter (Brock)".
- [X] T024 [US3] Em `tests/EvTrainer.Tests/Localization/NoHardcodedTextTests.cs`, incluir as traduções de `LocationNames` no conjunto `_data` de dados permitidos; no teste de T011, remover a exceção de locais (agora traduzidos).

**Checkpoint**: todas as histórias completas.

## Phase 6: Polish

- [X] T025 `dotnet build PokemonEvTrainer.slnx --no-incremental` sem avisos, `dotnet test --solution PokemonEvTrainer.slnx`, `dotnet format PokemonEvTrainer.slnx --verify-no-changes`
- [X] T026 Validação no navegador conforme `quickstart.md` (Playwright com `locale` `en-US`, `pt-BR`, `fr-FR`; perfis limpos e com prefs salvas; 320 e 1366 px, claro/escuro): ler cada tela em inglês procurando texto em português, textos cortados ou layout quebrado; corrigir traduções longas em `src/EvTrainer.Web/Localization/strings.en.json`
- [X] T027 [P] `README.md`: mencionar que o app está em português e inglês (detecta pelo navegador, troca no menu)

## Dependencies

- Phase 2 (T001→T002→T003) bloqueia tudo.
- US1: T004→T005; T006→T007; T008 → T009; T010 independente; T011 depois de T002.
- US2 depende de US1 (T007 preferência nula, T009 inicialização); T012–T015 sequenciais.
- US3 depende só da Phase 2 (pode correr em paralelo a US1/US2): T016/T017/T018 [P] → T019 → T020 → T021 → T022 → T023 → T024.
- Polish depois de todas.

## Parallel Example

```text
# Depois da Phase 2:
T004 LanguageMatcherTests.cs  |  T006 ModelJsonTests.cs/TrainerStoreTests.cs  |  T016 LocationNamesTests.cs  |  T017 RouteExpanderTests.cs  |  T018 SearchTests.cs
```

## Implementation Strategy

MVP = Phase 2 + US1 + US2 (o pedido: inglês, detecção pelo navegador, troca manual). US3 (locais em
inglês e busca bilíngue) é o incremento seguinte; pode ser entregue no mesmo commit da feature se o
tempo permitir, como nas features anteriores.
