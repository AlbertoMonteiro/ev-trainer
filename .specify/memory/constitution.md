# EVs Trainer Constitution

## Core Principles

### I. Aplicação Estática no Browser

O EVs Trainer é uma aplicação **Blazor WebAssembly standalone** que roda inteiramente no browser
e é publicada no **GitHub Pages**.

- O app NÃO DEVE depender de código de servidor, API própria ou qualquer backend. Toda feature
  DEVE funcionar servida como arquivos estáticos.
- O artefato publicado DEVE ser a saída de `dotnet publish -c Release` (`wwwroot`), com:
  - `<base href>` ajustado para o subcaminho do repositório no GitHub Pages;
  - `404.html` como fallback de rotas da SPA (deep links e refresh funcionam);
  - arquivo `.nojekyll` para que o Pages sirva `_framework/` sem processamento.
- Após o primeiro carregamento, o app DEVE continuar funcional sem conexão para as funções que
  não dependem de rede (todas, dado o Princípio V).

**Racional:** GitHub Pages só hospeda conteúdo estático; qualquer dependência de servidor quebra
o deploy e o uso offline à beira do jogo.

### II. Dois Projetos, Nada Mais (Simplicidade)

A solução DEVE conter exatamente dois projetos:

- `EvTrainer.Web` — o app Blazor WebAssembly;
- `EvTrainer.Tests` — todos os testes (domínio e componentes).

Regras:

- O domínio DEVE viver em `EvTrainer.Web/Domain/` como C# puro: sem referência a Blazor,
  `IJSRuntime`, `HttpClient`, armazenamento ou qualquer I/O. Ele concentra `EvTotals`, `IvRange`,
  `FinalRanges`, `IsUseful`, `Search`, `Recent` e `ParseImport`.
- Componentes e serviços de UI DEVEM delegar regras ao domínio; componentes NÃO DEVEM
  reimplementar cálculo de EV, IV, busca ou importação.
- Um terceiro projeto (biblioteca, servidor, ferramenta) só pode ser criado por emenda a esta
  constituição.
- YAGNI: nenhuma abstração, camada ou pacote sem necessidade presente e documentada no plano.

**Racional:** o app é pequeno e de um único usuário; a separação entre domínio puro e UI garante
testabilidade sem o custo de múltiplos assemblies.

### III. Test-First (INEGOCIÁVEL)

- Todo código de produção DEVE seguir red → green → refactor: o teste é escrito, executado e
  falha antes da implementação existir.
- Os critérios de aceite do HANDOFF §6 (IVs, interseção, limites de EV, aliases, busca, filtro
  "Úteis") DEVEM existir como testes automatizados antes do código que os satisfaz.
- Testes de domínio usam **xUnit**; componentes com lógica ou estado usam **bUnit**, no mesmo
  projeto `EvTrainer.Tests`.
- Correção de bug DEVE começar por um teste que reproduz o bug.
- Nenhum merge na `main` e nenhum deploy com teste falhando ou ignorado sem justificativa escrita.

**Racional:** as regras do jogo têm muitos casos-limite (limites, ordem de eventos, natures);
só testes escritos antes garantem que a nova versão reproduz o comportamento da atual.

### IV. Fidelidade às Regras da Gen 3

O app DEVE reproduzir as regras de Pokémon FireRed (Gen 3) descritas no HANDOFF §2 e §4:

- Fórmula de stats com **aritmética inteira**; o modificador de nature é `×11/10` ou `×9/10`,
  NUNCA `×1.1`/`×0.9` em ponto flutuante. HP nunca é afetado pela nature.
- Limites de EV: **255 por stat e 510 no total**; ganhos excedentes são descartados.
- Vitaminas: +10, só enquanto o stat estiver abaixo de 100, resultado limitado a 100, sujeitas ao
  limite de 510 e NÃO afetadas por Pokérus/Macho Brace.
- O multiplicador (1, 2 ou 4) DEVE ser gravado por evento; alternar modificadores não reescreve
  o passado.
- Totais de EV NÃO DEVEM ser armazenados: são sempre derivados reprocessando `events` em ordem,
  do zero.
- Nomes antigos DEVEM ser normalizados via `aliases` na leitura, sem reescrever os dados gravados.
- Os dados de referência (`reference-data.json`) DEVEM ser versionados no repositório e embutidos
  no build; não há download em tempo de execução de fontes externas.
- Em caso de dúvida de comportamento, `app-atual.html` é a referência.

**Racional:** um erro de arredondamento ou de ordem invalida o planejamento de EVs, que no jogo
não pode ser desfeito.

### V. Dados Locais e do Usuário

- A persistência DEVE ser exclusivamente local ao aparelho (localStorage ou IndexedDB), acessada
  por uma interface `IPokemonRepository`; o domínio não conhece o mecanismo de armazenamento.
- A UI DEVE atualizar de imediato (escrita otimista) e a gravação NÃO DEVE bloquear a interação.
- O app DEVE oferecer **exportar** e **importar** os dados em JSON, no formato do HANDOFF §3.1,
  aceitando `user-data-export.json` sem conversão manual. Export/import é o único meio de levar
  dados entre dispositivos.
- O app NÃO DEVE ter login, conta, sincronização em nuvem, telemetria, analytics ou envio de
  dados do usuário a terceiros.
- O indicador de status DEVE refletir o estado real (salvo neste aparelho, salvando, erro).
- Exclusões destrutivas (batalha, nível, Pokémon) DEVEM oferecer desfazer.

**Racional:** sem backend no GitHub Pages, o aparelho é a fonte da verdade; export/import
preserva a portabilidade sem introduzir servidor ou credenciais.

### VI. Mobile-First e Acessível

- Layout de coluna única, largura máxima 560 px e margem lateral de 16 px; sem rolagem
  horizontal em 360 px de largura.
- A ação principal (registrar batalha) DEVE exigir um único toque.
- Tema claro e escuro DEVEM usar os tokens do HANDOFF §5.7, seguindo o sistema.
- Animações DEVEM respeitar `prefers-reduced-motion`.
- Campos numéricos DEVEM usar `inputmode="numeric"`; controles só com ícone DEVEM ter
  `aria-label`.
- O app DEVERIA ser um PWA instalável com service worker offline, em substituição à Fullscreen
  API no celular.

**Racional:** o uso real é no celular, com o jogo aberto ao lado, tocando rápido após cada batalha.

### VII. Pronto para Outros Idiomas

O app é público. A primeira versão é em **português (pt-BR)**, mas a estrutura DEVE permitir
adicionar inglês (e outros idiomas) sem reescrever componentes nem regras.

- Nenhum texto visível ao usuário DEVE estar fixo em componentes: todo texto vem de recursos
  localizáveis identificados por chave.
- O domínio NÃO DEVE produzir texto de interface; ele devolve dados estruturados (ganhos, faixas,
  estados) e a UI os formata no idioma ativo.
- Adicionar um idioma DEVE se limitar a incluir um novo arquivo de recursos (e, se for o caso,
  traduções dos dados de referência), sem alterar componentes ou domínio.
- Um teste automatizado DEVE garantir que todo idioma disponível tem todas as chaves do idioma
  padrão.
- O idioma escolhido DEVE ser lembrado no aparelho, e o atributo `lang` do documento DEVE
  refletir o idioma ativo.

**Racional:** traduzir depois um app com textos espalhados custa uma reescrita; separar textos e
regras desde o início custa pouco.

## Restrições Técnicas

- Plataforma: **.NET 11** com **C# 15**, Blazor WebAssembly standalone (`net11.0`). Enquanto o
  .NET 11 não estiver em GA, usa-se a versão RC/preview mais recente, fixada em `global.json` e
  usada também no CI; a migração para a GA (prevista para novembro de 2026) DEVE ocorrer assim
  que ela for lançada.
- Recursos novos do .NET 11 / C# 15 DEVEM ser preferidos quando simplificarem o código ou o
  download; recursos experimentais que exigem flags de preview NÃO DEVEM ser usados em produção.
- Testes: xUnit + bUnit, apenas no projeto `EvTrainer.Tests`.
- JavaScript somente via interop pontual e justificado (armazenamento local, download/upload de
  arquivo, recursos do browser sem API .NET). Frameworks JS/CSS de terceiros NÃO DEVEM ser usados.
- CSS próprio com os design tokens do HANDOFF §5.7; fontes Chakra Petch, IBM Plex Sans e
  IBM Plex Mono.
- Trimming do publish DEVE permanecer habilitado; serialização JSON DEVE ser compatível com
  trimming (source generators do `System.Text.Json`).
- Todo pacote NuGet novo DEVE ser justificado no plano da feature que o introduz.
- Idioma padrão da interface: português (pt-BR), servido por recursos localizáveis
  (Princípio VII).

## Fluxo de Desenvolvimento e Deploy

- Toda feature segue o fluxo Spec Kit: `/speckit-specify` → `/speckit-clarify` →
  `/speckit-plan` → `/speckit-tasks` → `/speckit-implement`.
- Todo plano DEVE passar pelo "Constitution Check" contra os Princípios I–VII antes da
  implementação; violações DEVEM ser justificadas em "Complexity Tracking" ou removidas.
- Tarefas de implementação DEVEM ser ordenadas com testes antes do código correspondente
  (Princípio III).
- O deploy DEVE ser feito por GitHub Actions a cada push na `main`, na ordem:
  1. `dotnet format --verify-no-changes` e `dotnet test` (falha bloqueia o deploy);
  2. ajuste de `<base href>` no `index.html` (antes do publish, para não invalidar os hashes do
     service worker);
  3. `dotnet publish -c Release`;
  4. criação de `404.html` e `.nojekyll`;
  5. publicação no GitHub Pages.
- Nenhum deploy manual fora do workflow.

### dotnet CLI primeiro

Tudo o que a CLI do .NET consegue fazer DEVE ser feito por ela, nunca editando arquivos à mão:

- Criar solução, projetos e arquivos de configuração: `dotnet new` (`blazorwasm`, `xunit3`,
  `globaljson`, `buildprops`, `packagesprops`, `editorconfig`, `gitignore`…).
- Montar a solução e referências: `dotnet sln add`, `dotnet add reference`.
- Pacotes NuGet: `dotnet package add` / `dotnet package remove` (ou as formas equivalentes
  `dotnet add package` / `dotnet remove package`), inclusive com Central Package Management;
  verificar atualizações com `dotnet package list --outdated` e atualizar com
  `dotnet package add <id> --version <versão>`.
- Formatação e estilo: `dotnet format` (a verificação `--verify-no-changes` roda no CI).
- Workloads e ferramentas: `dotnet workload install`, `dotnet tool install`.

Edição manual só é permitida para o que a CLI não gera (código-fonte, propriedades MSBuild
específicas sem comando equivalente, conteúdo de `wwwroot`), e o plano/tarefas DEVEM indicar o
comando exato de cada passo de scaffolding.

## Governance

- Esta constituição prevalece sobre qualquer outra prática, guia ou convenção do projeto.
- Emendas DEVEM ser feitas via `/speckit-constitution`, com Sync Impact Report descrevendo a
  mudança e seu impacto em specs, planos e tarefas existentes.
- Versionamento semântico:
  - **MAJOR**: remoção ou redefinição incompatível de princípio ou regra de governança;
  - **MINOR**: novo princípio/seção ou ampliação material de orientação;
  - **PATCH**: esclarecimentos, redação e correções sem efeito semântico.
- Cada plano e cada PR DEVEM verificar conformidade com esta constituição; complexidade adicional
  DEVE ser justificada por escrito.
- `HANDOFF.md` é a especificação de referência do produto; esta constituição governa como ele é
  construído.

**Version**: 1.1.0 | **Ratified**: 2026-10-09 | **Last Amended**: 2026-10-09
