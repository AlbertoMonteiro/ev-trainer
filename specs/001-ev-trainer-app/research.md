# Research: EVs Trainer — reconstrução no browser

**Feature**: [spec.md](./spec.md) · **Plan**: [plan.md](./plan.md) · **Date**: 2026-10-09
(revisado para .NET 11 e idiomas, constituição v1.1.0)

Cada item registra a decisão, o porquê e as alternativas descartadas. Não restam itens
"NEEDS CLARIFICATION" no Technical Context.

## R1. Plataforma, SDK e template

- **Decision**: .NET 11 / C# 15, SDK `11.0.100-rc.1.26425.128` (instalado), fixado em `global.json`
  com `"rollForward": "latestFeature"` (pela CLI, `allowPrerelease` vale `true` por padrão, então
  RC2 e a GA são aceitos sem editar o arquivo).
  Tudo criado pela CLI (constituição, "dotnet CLI primeiro"): `dotnet new globaljson`,
  `dotnet new buildprops` (`TreatWarningsAsErrors` comum), `dotnet new editorconfig`,
  `dotnet new blazorwasm --pwa --empty -f net11.0` e `dotnet new xunit --xunit-version v3
  --test-runner Microsoft.Testing.Platform -f net11.0`. **Sem** Central Package Management: as
  versões ficam nos `.csproj` e são trocadas com `dotnet package add <id> --version <v>`.
- **Rationale**: constituição v1.1.0. O RC1 tem licença go-live e a GA está prevista para
  10/11/2026. CPM foi testado e descartado: o template `blazorwasm` do RC1 grava `Version=` nos
  `PackageReference` mesmo com `Directory.Packages.props` presente, e o restore falha com
  `NU1008`; adotá-lo exigiria editar o `.csproj` à mão. A migração para a GA fica em
  `dotnet package list --outdated --include-prerelease` + `dotnet package add`. O template do
  .NET 11 já traz `net11.0`,
  `OverrideHtmlAssetPlaceholders` (fingerprint + import map), `StaticWebAssetSpaFallbackEnabled`,
  o novo servidor de desenvolvimento `Microsoft.AspNetCore.Components.Gateway` e, com `--pwa`, o
  manifest e o service worker com cache offline.
- **Alternatives considered**: .NET 10 LTS (descartado pelo usuário); CPM (incompatível com o
  template sem edição manual, ver acima); template completo sem
  `--empty` (traz Bootstrap e páginas de exemplo que seriam apagadas); Blazor Web App com render
  WebAssembly (exige servidor, viola Princípio I).

## R2. Base path e publicação no GitHub Pages

- **Decision**: workflow GitHub Actions com `actions/setup-dotnet` lendo `global-json-file:
  global.json` (instala o RC exato), `dotnet workload install wasm-tools`, `dotnet test`, e então:
  `sed` reescreve `<base href="/" />` → `<base href="/${{ github.event.repository.name }}/" />` no
  `wwwroot/index.html` do checkout **antes** do `dotnet publish`; depois do publish copia
  `index.html` → `404.html` e cria `.nojekyll`; publica com `actions/configure-pages`,
  `actions/upload-pages-artifact` e `actions/deploy-pages`. Repositório
  `AlbertoMonteiro/ev-trainer` ⇒ base href `/ev-trainer/`, URL
  `https://albertomonteiro.github.io/ev-trainer/`.
- **Rationale**: o service worker valida cada asset (inclusive `index.html`) pelo hash gravado em
  `service-worker-assets.js`; reescrever o base href **depois** do publish (como faz a action
  `ghaction-rewrite-base-href` da doc oficial) quebraria essa integridade e o modo offline.
  `404.html` igual ao `index.html` basta porque o app tem rota única e base href absoluto.
  `.nojekyll` é exigido pela constituição (inócuo no deploy por artefato). `wasm-tools` habilita o
  relinking do runtime, que reduz bastante o download com globalização invariante.
- **Alternatives considered**: action de terceiros pós-publish (quebra o PWA, dependência externa);
  redirect `spa-github-pages` (desnecessário com rota única); branch `gh-pages` (exige
  `.gitattributes *.js binary` e mais permissões).

## R3. Compressão

- **Decision**: manter o padrão do SDK, sem `decode.js`/Brotli manual; o Pages aplica gzip.
- **Rationale**: o Pages não serve `.br` com `Content-Encoding`; o ganho só existiria no primeiro
  acesso, pois depois tudo vem do cache do service worker.
- **Alternatives considered**: `decode.js` + `autostart=false` da doc oficial (mais complexidade).

## R4. Persistência local

- **Decision**: `localStorage`, chave `evtrainer:v1:data` com o documento inteiro no **mesmo
  formato do backup**, e `evtrainer:v1:prefs` para preferências (Pokémon ativo, tema, filtro,
  idioma). Acesso síncrono por `IJSInProcessRuntime` atrás de `IPokemonRepository`. Gravação
  **imediata** a cada alteração.
- **Rationale**: dados pequenos (export atual: 7 KB); síncrono e sem biblioteca; gravar na hora
  elimina a janela de perda do debounce de 450 ms do app antigo (SC-006); mesmo formato do backup
  ⇒ exportar = serializar o que já está salvo (SC-004).
- **Alternatives considered**: IndexedDB (assíncrono, wrapper JS maior); debounce (fazia sentido
  para escrita em nuvem); uma chave por Pokémon (mais interop, sem ganho).
- **Falhas**: exceção em `setItem` (cota/modo privado) → estado segue em memória, indicador vai a
  "erro" e um aviso sugere exportar backup.

## R5. Serialização JSON

- **Decision**: `System.Text.Json` com source generator (`JsonSerializerContext` em `Domain/Json/`),
  `[JsonPropertyName]` iguais ao HANDOFF §3.1 (`o`, `n`, `m`, `lv`, `sp`, `st`, `ev`, `ivLog`…).
  Leitura tolerante (desconhecidos ignorados; `n`/`m` ausentes = 1).
- **Rationale**: compatível com trimming e formato idêntico ao `user-data-export.json` (FR-026).
  As novidades do STJ no .NET 11 (polimorfismo de tipos fechados e uniões) **não** são usadas na
  persistência: o formato é plano e precisa continuar compatível com o app antigo.
- **Alternatives considered**: reflexão (alertas de trimming); Newtonsoft.Json (pacote extra).

## R6. Dados de referência

- **Decision**: `reference-data.json` permanece na raiz e entra como `EmbeddedResource` por link
  (`LogicalName="EvTrainer.reference-data.json"`), desserializado uma vez em `ReferenceData`.
  `user-data-export.json` é ligado ao projeto de testes como fixture.
- **Rationale**: "embutido no build" (Princípio IV), offline sem `HttpClient`, mesma API nos testes,
  pacote do HANDOFF intacto.
- **Alternatives considered**: servir em `wwwroot` via `HttpClient` (I/O no domínio, mais um asset).
- **Idiomas**: nomes de locais/rotas do arquivo estão em português e continuam sendo **dados** (não
  textos de UI). A tradução deles fica para a feature de inglês, como um arquivo complementar por
  idioma (`reference-data.en.json` com nomes de locais traduzidos), sem mudar o domínio (R14).

## R7. Normalização de texto e globalização

- **Decision**: `InvariantGlobalization=true`. Remoção de acentos por tabela própria (`TextFold`:
  á/à/â/ã→a, é/ê→e, í→i, ó/ô/õ→o, ú/ü→u, ç→c, ♀→" f", ♂→" m", `ToLowerInvariant`), sem
  `string.Normalize(FormD)`.
- **Rationale**: modo invariante reduz download e startup; `Normalize` só é garantido com ICU. O app
  só formata inteiros, então não precisa de dados de cultura. A localização de textos (R14) não
  depende de `CultureInfo`, por isso é compatível com o modo invariante.
- **Alternatives considered**: ICU completo (`BlazorWebAssemblyLoadAllGlobalizationData`) só para
  `Normalize` e satellite assemblies (download maior; o arquivo ICU `EFIGS` nem inclui `pt`).

## R8. Testes

- **Decision**: projeto `tests/EvTrainer.Tests` (`net11.0`) criado pelo template nativo
  `dotnet new xunit --xunit-version v3 --test-runner Microsoft.Testing.Platform`, rodando na
  **Microsoft Testing Platform** (`dotnet new globaljson --test-runner Microsoft.Testing.Platform`;
  `dotnet test` nativo do SDK), mais bUnit 2.x via `dotnet package add bunit` (estável mais
  recente; compatível com .NET 8+). Pastas `Domain/` (xUnit puro: HANDOFF §6, SC-003, SC-004), `Components/` (bUnit com
  `InMemoryRepository` e `BunitJSInterop` estrito) e `Localization/` (paridade de chaves e
  ausência de texto fixo).
- **Rationale**: constituição (Princípios II, III e VII). MTP é o caminho atual do `dotnet test`
  e roda o executável de teste diretamente, mais rápido no CI.
- **Alternatives considered**: VSTest (legado); MSTest/NUnit (sem vantagem); Playwright E2E
  (terceiro projeto ou dependência pesada; fica como verificação manual no quickstart).

## R9. Estado da UI e gravação

- **Decision**: `TrainerStore` (singleton) mantém os Pokémon em memória, aplica comandos do
  domínio, grava via repositório e emite `Changed`. `ToastService` separado (4 s, "Desfazer").
- **Rationale**: domínio isolado e componentes finos (Princípio II); testes bUnit com store real.
- **Alternatives considered**: Fluxor/Redux (pacote e cerimônia); estado por componente.
- **.NET 11**: `IHostedService` no WebAssembly existe agora, mas o app não tem trabalho em segundo
  plano; não é usado.

## R10. Download e upload de arquivos

- **Decision**: exportar via função JS em `wwwroot/js/interop.js` (`Blob` + `<a download>`);
  importar via `InputFile` nativo. Nome: `evs-trainer-backup-AAAA-MM-DD.json`.
- **Rationale**: interop mínimo (Restrições Técnicas).
- **Alternatives considered**: File System Access API (sem suporte no Safari iOS).

## R11. Tema, fontes e movimento

- **Decision**: CSS próprio com tokens do HANDOFF §5.7; `prefers-color-scheme` por padrão e
  override por `data-theme` no `<html>`, aplicado por script inline no `index.html` antes do Blazor
  iniciar (o mesmo script aplica `lang`, ver R14). Fontes OFL hospedadas em `wwwroot/fonts/*.woff2`
  (subset latin). `prefers-reduced-motion` desliga a animação "bump".
- **Rationale**: offline e sem requisições a terceiros (Princípio V).
- **Alternatives considered**: Google Fonts (externo, falha offline); framework CSS (vedado).

## R12. Ids, migração e merge

- **Decision**: ids importados preservados (`"squirtle"`); novos ids com `Guid.CreateVersion7()`
  em formato `N`. Importação de backup = merge por id com prévia ("X novos, Y substituídos").
  Aliases resolvidos só na leitura.
- **Rationale**: FR-025–FR-027; GUID v7 é ordenável por tempo, útil para depurar.
- **Alternatives considered**: substituir tudo (risco de perda); reescrever aliases (viola IV).

## R13. Recursos novos do .NET 11 / C# 15 — o que entra e o que não entra

Critério da constituição: usar o que simplifica código ou download; nada que exija flag de preview.

| Recurso | Decisão | Onde |
|---|---|---|
| `net11.0` + C# 15 como padrão | **Usa** | Os dois projetos |
| Closed hierarchies (`closed record`) | **Usa** | `IvCell` (Faixa / Inconsistente), `FinalCell` (Faixa / Único / Conferir / SemDados), `SaveStatus`, `ToastAction`: `switch` exaustivo sem `default` |
| Union types (`union`) | **Usa** | `BackupReadResult = union(BackupDocuments, BackupError)` e `ImportLine = union(...)` no parser de planilha |
| Collection expression arguments (`[with(...)]`) | **Usa** | `HashSet<string>` com comparer em `Recent` e no índice de busca |
| Labeled `break`/`continue` | **Usa** | Laços aninhados do `SpreadsheetImport` (linhas × células) |
| Extension members (C# 14) | **Usa** | Helpers de vetor de 6 stats (`Sum()`, `ClampTo()`) sem classe utilitária solta |
| Gateway como servidor de dev (template) | **Usa** | `dotnet watch`; não vai para produção (Pages) |
| Fingerprint + import map (`OverrideHtmlAssetPlaceholders`) | **Usa** | Padrão do template; nomes de arquivo com hash no publish |
| Publish menor (feature switches de métricas e hot reload) | **Usa** (automático) | Trimming no publish |
| Analyzer BL0010 (`InvokeVoidAsync`) | **Usa** (automático) | Com `TreatWarningsAsErrors` |
| Formato novo de opções do `Blazor.start` | Não se aplica | O app usa `autostart` padrão |
| `IHostedService` no WASM | Não usa | Sem tarefas em segundo plano |
| Blazor Web Worker (`blazorwebworker`) | Não usa | Cálculos são triviais; seria um terceiro projeto |
| CoreCLR no WebAssembly | Não usa | Experimental no .NET 11, sem chave documentada; WASM segue em Mono |
| Multithreading no WASM | Não usa | Sem ganho; exige cabeçalhos COOP/COEP que o Pages não envia |
| Variáveis de ambiente em `IConfiguration` | Não usa | Sem configuração por ambiente |
| `DisplayName` + `[Display(ResourceType)]` | Não usa | Depende de recursos `.resx`; a localização escolhida é por chaves JSON (R14) |
| `Virtualize` com alturas variáveis, `QuickGrid` | Não usa | Listas pequenas (157 itens) e sem tabelas de dados |
| STJ uniões/tipos fechados | Não usa | Formato persistido é plano (R5) |
| Memory safety (`unsafe` novo) | Não usa | Exige flag de preview; app não usa ponteiros |

## R14. Localização (pt-BR agora, inglês depois)

- **Decision**: interface `IStringLocalizer` (pacote `Microsoft.Extensions.Localization.Abstractions`)
  com implementação própria `JsonStringLocalizer`, lendo arquivos embutidos
  `src/EvTrainer.Web/Localization/strings.<idioma>.json` (chave → texto, com `{0}` para argumentos).
  Nesta versão existe só `strings.pt-BR.json`. Idioma ativo em `UiPreferences.Language` (padrão
  `pt-BR`; quando houver inglês, o padrão inicial segue `navigator.language`). Fallback: idioma ativo →
  pt-BR → a própria chave (e o teste de paridade falha). Componentes injetam
  `IStringLocalizer<T>`; o domínio devolve dados (ganho por stat, tipo de célula, contagens) e a UI
  formata. Nomes de stats (`HP ATK DEF…`, `HP Ataque Defesa…`) também viram chaves. O seletor de
  idioma no menu só aparece com 2+ idiomas. Trocar de idioma re-renderiza na hora, sem recarregar.
- **Rationale**: atende o Princípio VII e FR-035 sem ICU nem satellite assemblies (compatível com
  `InvariantGlobalization`, R7), sem recarregar a página para trocar idioma e sem `.resx`. Usar a
  interface padrão do .NET mantém a porta aberta para trocar por `.resx` no futuro sem tocar nos
  componentes. A abstração é um pacote pequeno e oficial (justificativa exigida pela constituição).
- **Testes**: (1) todo `strings.*.json` tem exatamente as chaves do pt-BR; (2) toda chave usada no
  código existe no pt-BR (busca por `L["..."]` nos `.razor`/`.cs`); (3) um idioma de teste
  `strings.xx.json` gerado no teste troca todos os textos de uma tela renderizada pelo bUnit
  (SC-010); (4) nenhum componente contém texto literal visível (verificação por render bUnit
  comparando com o idioma de teste).
- **Alternatives considered**: `.resx` + `IStringLocalizer` padrão (exige satellite assemblies,
  culturas reais e ICU no WASM, e recarregar a página para trocar idioma); textos em constantes C#
  (quebra o Princípio VII); biblioteca de i18n de terceiros (pacote extra sem ganho).

## R15. Pokédex embutida (feedback do usuário: não digitar linha evolutiva nem stats)

- **Decision**: gerar `pokedex-gen3.json` (raiz, embutido por link como o `reference-data.json`)
  com as espécies #1–386, stats base da Geração III e `evolvesFrom`, usando o file-based app
  `scripts/BuildPokedex.cs` (`dotnet run scripts/BuildPokedex.cs`, recurso do .NET 10+ que não cria
  projeto). Fonte: PokéAPI — GraphQL para espécies/nomes/evoluções e REST `/pokemon/{id}` para
  `past_stats`. O valor da Gen III de cada stat é o `past_stats` de menor geração ≥ III, ou o atual.
- **Rationale**: FireRed permite treinar Pokémon de Johto/Hoenn (Sevii Islands, trocas), então
  cobrir #1–386 evita outra lacuna. Stats mudaram na Gen VI/VII (ex.: Butterfree SpAtk 80 → 90,
  Pikachu Def 30 → 40, Pidgeot Speed 91 → 101); usar os atuais daria faixas de IV erradas. Conferido:
  Butterfree, Pikachu, Clefable, Pidgeot, Beedrill, Alakazam, Golem, Noctowl, Swellow, Pelipper.
- **Alternatives considered**: só os 151 de Kanto (deixa de fora Pokémon treináveis no jogo);
  digitar à mão (rejeitado pelo usuário); stats atuais da PokéAPI (errados para a Gen III); script
  Python (fora da regra "dotnet CLI primeiro").
