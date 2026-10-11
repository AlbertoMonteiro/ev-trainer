# Research: Pokémon identificado pela espécie, sem apelido

Não havia `NEEDS CLARIFICATION` no contexto técnico; as decisões abaixo resolvem as escolhas de desenho.

## R1 — Onde guardar a "forma atual"

- **Decision**: `TrainingPokemon.Name` é renomeado para `CurrentSpecies` e continua serializado na
  propriedade JSON `"name"`. Não há propriedade JSON nova.
- **Rationale**: a linha evolutiva (`species`) inclui as evoluções futuras
  (`Pokedex.LineFor` completa a linha enquanto houver uma única opção), então ela sozinha não diz em
  que forma o Pokémon está; hoje essa informação vive, implicitamente, no apelido. Gravar a forma atual
  em `"name"` mantém o formato do HANDOFF §3.1 e faz versões antigas do app (e o `app-atual.html`)
  mostrarem o nome da espécie, que é exatamente o que um apelido "não personalizado" já continha.
- **Alternatives considered**:
  - Campo novo `"current"` mantendo `"name"`: duplica informação e deixa o apelido antigo órfão no
    arquivo, sem uso.
  - Derivar sempre da última anotação de nível (`DisplaySpecies` atual): não reflete a evolução feita
    pelos Ajustes antes de anotar um nível (Story 1, cenário 3), e não permite corrigir um toque errado.
  - Usar a última espécie da linha: errado para linhas completadas pelo Pokédex (Ralts → … → Gardevoir).

## R2 — Normalização ao ler dados antigos

- **Decision**: uma função pura `CurrentForm.Resolve(name, line, ivLog)` no domínio, aplicada em
  `PokemonJson.FromDto` (armazenamento local e backup): (1) `name` igual a uma espécie da linha (ignorando
  maiúsculas/minúsculas) → essa espécie, com a grafia da linha; (2) senão, a espécie mais avançada na linha
  entre as anotações de nível; (3) senão, a primeira espécie da linha com nome; (4) senão, `""`.
- **Rationale**: Princípio IV pede normalizar na leitura sem reescrever o que foi gravado; aqui o dado é
  reescrito só na próxima gravação normal, já normalizado. Uma regra única cobre localStorage, backup e
  `user-data-export.json`.
- **Alternatives considered**: migração única com flag de versão — mais estado para manter, e backups
  antigos importados depois precisariam do mesmo código de qualquer forma.

## R3 — Avanço automático por anotação de nível

- **Decision**: `TrainingPokemon.InsertLevel` (usado por `AddLevel`, `ImportLevels` e `ReplaceLevel`)
  chama `AdvanceTo(entry.Species)`: se a espécie está na linha com índice maior que o da forma atual (ou se
  a forma atual está vazia), ela vira a forma atual; senão nada muda. Remover uma anotação nunca desfaz a
  evolução; o Desfazer de `ReplaceLevel` restaura também a forma atual anterior.
- **Rationale**: atende FR-005 com uma regra só, no domínio, sem duplicar lógica nos componentes
  (Princípio II). "Nunca desevoluir" evita que uma anotação atrasada mude a imagem.
- **Alternatives considered**: perguntar ao usuário a cada anotação — toque extra sem ganho.

## R4 — Escolha da forma atual nos Ajustes

- **Decision**: o `SpeciesPicker` recebe `Current` e passa a mostrar cada espécie da linha como botão
  alternável (`aria-pressed`); tocar marca a forma atual. Escolher uma espécie na busca ou tocar em
  "Evolui para…" já devolve `SpeciesPick.Picked`, que passa a ser a forma atual do formulário. O campo
  "Nome" sai do `SettingsForm` e a regra `IsValid` deixa de exigir nome (passa a exigir forma atual na
  linha).
- **Rationale**: reaproveita o componente e o `SpeciesPick.Picked` que já existem; 1 toque para evoluir
  + 1 para salvar (SC-001).
- **Alternatives considered**: `<select>` "Forma atual" separado — redundante com os chips que já mostram
  a linha com imagem.

## R5 — Imagem no resumo de EVs

- **Decision**: o `<h2>` de `EvSummary` passa a conter o `PokemonSprite` da forma atual em 64 px dentro de
  um `<span role="img" aria-label="…">` com o texto `Evs.Summary.Label` ("EVs de {0}"); o texto visível do
  título some. O selo de reserva do `PokemonSprite` cobre a falta de imagem.
- **Rationale**: os sprites da Gen 3 são 64×64 (`wwwroot/sprites/*.png`), então 64 px é o tamanho nítido
  sem escala; o `<h2>` continua existindo para a estrutura de títulos e leitores de tela (Princípio VI).
- **Alternatives considered**: 40 px como nos cards — pequeno demais para ser o identificador principal
  da tela; 96 px+ — escala com serrilhado e empurra os totais em 360 px.

## R6 — Nome exibido nos demais lugares

- **Decision**: menu, título da aba IVs, painel de exclusão e avisos usam um helper de UI
  `PokemonLabel` → `CurrentSpecies` ou, quando vazio, `L["Pokemon.NoSpecies"]` ("Pokémon sem espécie").
  O menu passa a mostrar também a nature (quando houver) para diferenciar Pokémon da mesma espécie.
- **Rationale**: texto vem de recursos (Princípio VII); o domínio continua sem produzir texto.
